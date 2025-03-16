using Animancer;
using Animancer.Editor;
using ML.Engine.Animation;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEditorInternal;
using UnityEngine;
using static UnityEditor.VersionControl.Asset;

namespace ML.Editor.Animation
{
    //[UnityEditor.CustomEditor(typeof(MixerManualTransitionAsset), true)]
    public abstract class MixerTransitionAssetEditor<T> : AnimationAssetBaseEditor
        where T : AnimationAssetBase, IAssetHasEvents
    {
        protected string _WeightsField => GetWeightsField();
        protected virtual string GetWeightsField() => "_Weights";
        protected string _AnimationsField => GetAnimationsField();
        protected virtual string GetAnimationsField() => "_Animations";
        protected string _SpeedsField => GetSpeedsField();
        protected virtual string GetSpeedsField() => "_Speeds";
        protected string _SynchronizeChildrenField => GetSynchronizeChildrenField();
        protected virtual string GetSynchronizeChildrenField() => "_SynchronizeChildren";

        protected EventTrack eventTrack { get; set; }

        protected T asset { get; set; }
        // 事件
        protected SerializedProperty _endEventProperty { get; set; }
        protected static SerializedProperty CurrentAnimations { get; set; }
        protected static SerializedProperty CurrentSpeeds { get; set; }
        protected static SerializedProperty CurrentWeights { get; set; }
        protected static SerializedProperty CurrentSynchronizeChildren { get; set; }

        protected static SerializedProperty CurrentProperty { get; set; }

        // [Animation Speed Weight Sync]
        protected ReorderableList _AnimationList { get; set; }

        protected ITransitionDetailed transition { get; set; }

        protected abstract float FadeDuration { get; set; }
        
        public override void Init()
{
            asset = target as T;
            eventTrack = new EventTrack(asset);
            var p = serializedObject.FindProperty("transition");
            _endEventProperty = serializedObject.FindProperty("_EndEvent");
            CurrentAnimations = p.FindPropertyRelative(_AnimationsField);
            CurrentSpeeds = p.FindPropertyRelative(_SpeedsField);
            CurrentWeights = p.FindPropertyRelative(_WeightsField);
            CurrentSynchronizeChildren = p.FindPropertyRelative(_SynchronizeChildrenField);

            if(CurrentWeights != null && CurrentWeights.arraySize != CurrentAnimations.arraySize)
            {
                serializedObject.Update();
                CurrentWeights.arraySize = CurrentAnimations.arraySize;
                serializedObject.ApplyModifiedProperties();
            }

            _AnimationList = new ReorderableList(CurrentAnimations.serializedObject, CurrentAnimations)
            {
                drawHeaderCallback = DoChildListHeaderGUI,
                elementHeightCallback = GetElementHeight,
                drawElementCallback = DoElementGUI,
                onAddCallback = OnAddElement,
                onRemoveCallback = OnRemoveElement,
                onReorderCallbackWithDetails = OnReorderList,
                drawFooterCallback = DoChildListFooterGUI,
            };
            _AnimationList.serializedProperty = CurrentAnimations;
            InitTransition();
        }

        /// <summary>
        /// 必须初始化transition
        /// </summary>
        public abstract void InitTransition();

        public override void DrawTrackGUI()
        {
            eventTrack.DrawTrackGUI();
        }

        protected bool bShowFadeDuration = true;
        protected bool bEndTime = true;
        public override void DrawInEditorWindow(EditorWindow window)
        {
            serializedObject.Update();

            float length = asset.Length;
            float frameRate = asset.FrameRate;
            float speed = transition.Speed;

            // 速度
            // Speed -> 播放速度
            DoSpeedGUI(transition);

            #region 时间轴 -> 使用秒数时间
            EditorGUILayout.Space();
            DoAnimTimelineGUI(transition, asset.EndEvent.NormalizedTime, length, frameRate);
            EditorGUILayout.Space();
            #endregion

            // 过渡时间
            // Fade Duration -> 过渡时间
            FadeDuration = DoFadeDurationGUI(transition, length, frameRate, ref bShowFadeDuration);

            // 结束时间
            DoEndTimeGUI(_endEventProperty, length, frameRate, (float.IsNaN(speed) || speed >= 0) ? 1 : 0, ref bEndTime);

            // Animations[动画、Speed、Weight、Sync]
            // Animation -> ObjectField
            // Speed -> Toggle + FloatField
            // Weight -> FloatField[0, 1]
            // Sync -> Toggle
            // 保证数组长度一致，以动画为准
            _AnimationList.DoLayoutList();

            serializedObject.ApplyModifiedProperties();
        }


        #region AnimationList
        #region Header
        protected virtual void DoChildListHeaderGUI(Rect area)
        {
            SplitListRect(area, true, out var animationArea, out var speedArea, out var weightArea, out var syncArea);

            DoAnimationHeaderGUI(animationArea);
            DoWeightHeaderGUI(weightArea);
            DoSpeedHeaderGUI(speedArea);
            DoSyncHeaderGUI(syncArea);
        }

        protected void DoAnimationHeaderGUI(Rect area)
        {
            using (ObjectPool.Disposable.AcquireContent(out var label, "动画",
                $"The animations that will be used for each child state" +
                $"\n\nCtrl + Click to allow picking Transition Assets (or anything that implements {nameof(ITransition)})"))
            {
                DoHeaderDropdownGUI(area, CurrentAnimations, label, menu =>
                {
                    menu.AddItem(new GUIContent(TwoLineMode.MenuItem), TwoLineMode.Value, () =>
                    {
                        TwoLineMode.Value = !TwoLineMode.Value;
                    });
                });
            }
        }

        protected void DoSpeedHeaderGUI(Rect area)
        {
            using (ObjectPool.Disposable.AcquireContent(out var label, "速度", Strings.Tooltips.Speed))
            {
                DoHeaderDropdownGUI(area, CurrentSpeeds, label, menu =>
                {
                    AddPropertyModifierFunction(menu, "重置为1",
                        CurrentSpeeds.arraySize == 0 ? MenuFunctionState.Selected : MenuFunctionState.Normal,
                        (_) => CurrentSpeeds.arraySize = 0);

                    AddPropertyModifierFunction(menu, "规约", MenuFunctionState.Normal, NormalizeDurations);
                });
            }
        }

        protected void DoWeightHeaderGUI(Rect area)
        {
            if(CurrentAnimations == null)
            {
                return;
            }
            using (ObjectPool.Disposable.AcquireContent(out var label, "权重", Strings.Tooltips.Speed))
            {
                DoHeaderDropdownGUI(area, CurrentWeights, label, menu =>
                {
                    AddPropertyModifierFunction(menu, "重置为平均",
                        CurrentWeights.arraySize == 0 ? MenuFunctionState.Selected : MenuFunctionState.Normal,
                        (_weight) => _weight.floatValue = 1f / CurrentWeights.arraySize);
                });
            }
        }

        protected void DoSyncHeaderGUI(Rect area)
        {
            using (ObjectPool.Disposable.AcquireContent(out var label, "同步",
                "Determines which child states have their normalized times constantly synchronized"))
            {
                DoHeaderDropdownGUI(area, CurrentSpeeds, label, menu =>
                {
                    var syncCount = CurrentSynchronizeChildren.arraySize;

                    var allState = syncCount == 0 ? MenuFunctionState.Selected : MenuFunctionState.Normal;
                    AddPropertyModifierFunction(menu, "All", allState,
                        (_) => CurrentSynchronizeChildren.arraySize = 0);

                    var syncNone = syncCount == CurrentAnimations.arraySize;
                    if (syncNone)
                    {
                        for (int i = 0; i < syncCount; i++)
                        {
                            if (CurrentSynchronizeChildren.GetArrayElementAtIndex(i).boolValue)
                            {
                                syncNone = false;
                                break;
                            }
                        }
                    }
                    var noneState = syncNone ? MenuFunctionState.Selected : MenuFunctionState.Normal;
                    AddPropertyModifierFunction(menu, "None", noneState, (_) =>
                    {
                        var count = CurrentSynchronizeChildren.arraySize = CurrentAnimations.arraySize;
                        for (int i = 0; i < count; i++)
                            CurrentSynchronizeChildren.GetArrayElementAtIndex(i).boolValue = false;
                    });

                    AddPropertyModifierFunction(menu, "Invert", MenuFunctionState.Normal, (_) =>
                    {
                        var count = CurrentSynchronizeChildren.arraySize;
                        for (int i = 0; i < count; i++)
                        {
                            var property = CurrentSynchronizeChildren.GetArrayElementAtIndex(i);
                            property.boolValue = !property.boolValue;
                        }

                        var newCount = CurrentSynchronizeChildren.arraySize = CurrentAnimations.arraySize;
                        for (int i = count; i < newCount; i++)
                            CurrentSynchronizeChildren.GetArrayElementAtIndex(i).boolValue = false;
                    });

                    AddPropertyModifierFunction(menu, "Non-Stationary", MenuFunctionState.Normal, (_) =>
                    {
                        var count = CurrentAnimations.arraySize;

                        for (int i = 0; i < count; i++)
                        {
                            var state = CurrentAnimations.GetArrayElementAtIndex(i).objectReferenceValue;
                            if (state == null)
                                continue;

                            if (i >= syncCount)
                            {
                                CurrentSynchronizeChildren.arraySize = i + 1;
                                for (int j = syncCount; j < i; j++)
                                    CurrentSynchronizeChildren.GetArrayElementAtIndex(j).boolValue = true;
                                syncCount = i + 1;
                            }

                            CurrentSynchronizeChildren.GetArrayElementAtIndex(i).boolValue =
                                AnimancerUtilities.TryGetAverageVelocity(state, out var velocity) &&
                                velocity != default;
                        }

                        TryCollapseSync();
                    });
                });
            }
        }

        #endregion

        #region Element
        protected virtual float GetElementHeight(int index) => TwoLineMode ? AnimancerGUI.LineHeight * 2 : AnimancerGUI.LineHeight;

        #region ElementGUI
        private void DoElementGUI(Rect area, int index, bool isActive, bool isFocused)
        {
            if (index < 0 || index > CurrentAnimations.arraySize)
                return;

            area.height = AnimancerGUI.LineHeight;

            var state = CurrentAnimations.GetArrayElementAtIndex(index);
            var speed = CurrentSpeeds.arraySize > 0 ? CurrentSpeeds.GetArrayElementAtIndex(index) : null;
            var weight = CurrentWeights == null ? null : CurrentWeights.GetArrayElementAtIndex(index);
            DoElementGUI(area, index, state, speed, weight);
        }

        protected virtual void DoElementGUI(Rect area, int index, SerializedProperty animation, SerializedProperty speed, SerializedProperty weight)
        {
            SplitListRect(area, false, out var animationArea, out var speedArea, out var weightArea,out var syncArea);

            DoAnimationField(animationArea, animation);
            DoSpeedFieldGUI(speedArea, speed, index);
            DoWeightFieldGUI(weightArea, weight, index);
            DoSyncToggleGUI(syncArea, index);
        }

        public static void DoAnimationField(Rect area, SerializedProperty property)
        {
            EditorGUI.BeginProperty(area, GUIContent.none, property);

            var targetObject = property.serializedObject.targetObject;
            var oldReference = property.objectReferenceValue;

            var currentEvent = Event.current;
            var isDrag =
                currentEvent.type == EventType.DragUpdated ||
                currentEvent.type == EventType.DragPerform;
            var type = isDrag || currentEvent.control || currentEvent.commandName == "ObjectSelectorUpdated" ? typeof(System.Object) : typeof(AnimationClip);

            var allowSceneObjects = targetObject != null && !EditorUtility.IsPersistent(targetObject);

            EditorGUI.BeginChangeCheck();
            var newReference = EditorGUI.ObjectField(area, GUIContent.none, oldReference, type, allowSceneObjects);
            if (EditorGUI.EndChangeCheck())
            {
                if (newReference == null || (IsClipOrTransition(newReference) && newReference != targetObject))
                    property.objectReferenceValue = newReference;
            }

            if (isDrag && area.Contains(currentEvent.mousePosition))
            {
                var objects = DragAndDrop.objectReferences;
                if (objects.Length != 1 ||
                    !IsClipOrTransition(objects[0]) ||
                    objects[0] == targetObject)
                    DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
            }

            EditorGUI.EndProperty();
        }
        
        protected void DoSpeedFieldGUI(Rect area, SerializedProperty speed, int index)
        {
            if (speed != null)
            {
                EditorGUI.PropertyField(area, speed, GUIContent.none);
            }
            else// If this element doesn't have its own speed property, just show 1.
            {
                EditorGUI.BeginProperty(area, GUIContent.none, CurrentSpeeds);

                var value = Animancer.Units.UnitsAttribute.DoSpecialFloatField(
                    area, null, 1, Animancer.Units.AnimationSpeedAttribute.DisplayConverters[0]);

                // Middle Click toggles from 1 to -1.
                if (AnimancerGUI.TryUseClickEvent(area, 2))
                    value = -1;

                if (value != 1)
                {
                    CurrentSpeeds.InsertArrayElementAtIndex(0);
                    CurrentSpeeds.GetArrayElementAtIndex(0).floatValue = 1;
                    CurrentSpeeds.arraySize = CurrentAnimations.arraySize;
                    CurrentSpeeds.GetArrayElementAtIndex(index).floatValue = value;
                }

                EditorGUI.EndProperty();
            }
        }

        protected void DoWeightFieldGUI(Rect area, SerializedProperty weight, int index)
        {
            if (weight != null)
            {
                EditorGUI.PropertyField(area, weight, GUIContent.none);
            }
        }

        protected void DoSyncToggleGUI(Rect area, int index)
        {
            var syncProperty = CurrentSynchronizeChildren;
            var syncFlagCount = syncProperty.arraySize;

            var enabled = true;

            if (index < syncFlagCount)
            {
                syncProperty = syncProperty.GetArrayElementAtIndex(index);
                enabled = syncProperty.boolValue;
            }

            EditorGUI.BeginChangeCheck();
            EditorGUI.BeginProperty(area, GUIContent.none, syncProperty);

            enabled = GUI.Toggle(area, enabled, GUIContent.none);

            EditorGUI.EndProperty();
            if (EditorGUI.EndChangeCheck())
            {
                if (index < syncFlagCount)
                {
                    syncProperty.boolValue = enabled;
                }
                else
                {
                    syncProperty.arraySize = index + 1;

                    for (int i = syncFlagCount; i < index; i++)
                    {
                        syncProperty.GetArrayElementAtIndex(i).boolValue = true;
                    }

                    syncProperty.GetArrayElementAtIndex(index).boolValue = enabled;
                }
            }
        }
        #endregion

        #region Add
        private void OnAddElement(ReorderableList list)
        {
            var index = list.index;
            if (index < 0 || Event.current.button == 1)// Right Click to add at the end.
            {
                index = CurrentAnimations.arraySize - 1;
                if (index < 0)
                    index = 0;
            }

            OnAddElement(index);
        }

        protected virtual void OnAddElement(int index)
        {
            CurrentAnimations.InsertArrayElementAtIndex(index);
            CurrentWeights?.InsertArrayElementAtIndex(index);

            if (CurrentSpeeds.arraySize > 0)
            {
                CurrentSpeeds.InsertArrayElementAtIndex(index);
            }
            if (CurrentSynchronizeChildren.arraySize > index)
            {
                CurrentSynchronizeChildren.InsertArrayElementAtIndex(index);
            }
        }

        #endregion

        #region Remove
        protected virtual void OnRemoveElement(ReorderableList list)
        {
            var index = list.index;

            Serialization.RemoveArrayElement(CurrentAnimations, index);
            if(CurrentWeights !=null)
                Serialization.RemoveArrayElement(CurrentWeights, index);

            if (CurrentSpeeds.arraySize > index)
            {
                Serialization.RemoveArrayElement(CurrentSpeeds, index);
            }

            if (CurrentSynchronizeChildren.arraySize > index)
            {
                Serialization.RemoveArrayElement(CurrentSynchronizeChildren, index);
            }
        }

        #endregion

        #region Reorder
        protected virtual void OnReorderList(ReorderableList list, int oldIndex, int newIndex)
        {
            CurrentWeights?.MoveArrayElement(oldIndex, newIndex);

            CurrentSpeeds.MoveArrayElement(oldIndex, newIndex);

            var syncCount = CurrentSynchronizeChildren.arraySize;
            if (Math.Max(oldIndex, newIndex) >= syncCount)
            {
                CurrentSynchronizeChildren.arraySize++;
                CurrentSynchronizeChildren.GetArrayElementAtIndex(syncCount).boolValue = true;
                CurrentSynchronizeChildren.arraySize = newIndex + 1;
            }

            CurrentSynchronizeChildren.MoveArrayElement(oldIndex, newIndex);
        }

        #endregion

        #endregion

        #region Footer
        protected virtual void DoChildListFooterGUI(Rect area)
        {
            ReorderableList.defaultBehaviours.DrawFooter(area, _AnimationList);

            EditorGUI.BeginChangeCheck();

            area.xMax = EditorGUIUtility.labelWidth + AnimancerGUI.IndentSize;

            area.y++;
            area.height = AnimancerGUI.LineHeight;

            using (ObjectPool.Disposable.AcquireContent(out var label, "Count"))
            {
                var indentLevel = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0;

                var labelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = AnimancerGUI.CalculateLabelWidth(label.text);

                var count = EditorGUI.DelayedIntField(area, label, _AnimationList.count);

                if (EditorGUI.EndChangeCheck())
                    ResizeList(count);

                EditorGUIUtility.labelWidth = labelWidth;

                EditorGUI.indentLevel = indentLevel;
            }
        }

        #endregion

        #region STATIC
        public static readonly BoolPref TwoLineMode = new BoolPref(nameof(TwoLineMode), "按两行显示", true);
        private static float _SpeedLabelWidth;
        private static float _WeightLabelWidth;
        private static float _SyncLabelWidth;
        protected static void SplitListRect(Rect area, bool isHeader, out Rect animation, out Rect speed, out Rect weight,out Rect sync)
        {
            if (_SpeedLabelWidth == 0)
                _SpeedLabelWidth = AnimancerGUI.CalculateWidth(EditorStyles.popup, "速度");

            if (_WeightLabelWidth == 0)
                _WeightLabelWidth = AnimancerGUI.CalculateWidth(EditorStyles.popup, "权重");

            if (_SyncLabelWidth == 0)
                _SyncLabelWidth = AnimancerGUI.CalculateWidth(EditorStyles.popup, "同步");

            var spacing = AnimancerGUI.StandardSpacing;

            var syncWidth = isHeader ? _SyncLabelWidth : AnimancerGUI.ToggleWidth - spacing;
            //var syncWidth = _SyncLabelWidth;

            //var weightWidth = _WeightLabelWidth + _SyncLabelWidth - syncWidth;
            var weightWidth = _WeightLabelWidth + (_SyncLabelWidth - syncWidth) * 0.5f;

            var speedWidth = _SpeedLabelWidth + (_SyncLabelWidth - syncWidth) * 0.5f;
            //var speedWidth = _SpeedLabelWidth;

            if (!isHeader)
            {
                // Don't use Clamp because the max might be smaller than the min.
                var max = Mathf.Max(area.height, area.width * 0.25f - 30);
                speedWidth = Mathf.Min(speedWidth, max);
            }

            area.width += spacing;
            if (TwoLineMode && !isHeader)
            {
                animation = area;
                area.y += area.height;
                sync = AnimancerGUI.StealFromRight(ref area, syncWidth, spacing);
                speed = AnimancerGUI.StealFromRight(ref area, area.width * 0.5f, spacing);
                weight = area;
            }
            else
            {
                sync = AnimancerGUI.StealFromRight(ref area, syncWidth, spacing);
                speed = AnimancerGUI.StealFromRight(ref area, speedWidth, spacing);
                weight = AnimancerGUI.StealFromRight(ref area, weightWidth, spacing);
                animation = area;
            }
        }

        public static void DoHeaderDropdownGUI(Rect area, SerializedProperty property, GUIContent content, Action<GenericMenu> populateMenu)
        {
            if (property != null)
                EditorGUI.BeginProperty(area, GUIContent.none, property);

            if (populateMenu != null)
            {
                if (EditorGUI.DropdownButton(area, content, FocusType.Passive))
                {
                    var menu = new GenericMenu();
                    populateMenu(menu);
                    menu.ShowAsContext();
                }
            }
            else
            {
                GUI.Label(area, content);
            }

            if (property != null)
                EditorGUI.EndProperty();
        }

        private static void NormalizeDurations(SerializedProperty property)
        {
            var speedCount = CurrentSpeeds.arraySize;

            var lengths = new float[CurrentAnimations.arraySize];
            if (lengths.Length <= 1)
                return;

            int nonZeroLengths = 0;
            float totalLength = 0;
            float totalSpeed = 0;
            for (int i = 0; i < lengths.Length; i++)
            {
                var state = CurrentAnimations.GetArrayElementAtIndex(i).objectReferenceValue;
                if (AnimancerUtilities.TryGetLength(state, out var length) &&
                    length > 0)
                {
                    nonZeroLengths++;
                    totalLength += length;
                    lengths[i] = length;

                    if (speedCount > 0)
                        totalSpeed += CurrentSpeeds.GetArrayElementAtIndex(i).floatValue;
                }
            }

            if (nonZeroLengths == 0)
                return;

            var averageLength = totalLength / nonZeroLengths;
            var averageSpeed = speedCount > 0 ? totalSpeed / nonZeroLengths : 1;

            CurrentSpeeds.arraySize = lengths.Length;
            InitializeSpeeds(speedCount);

            for (int i = 0; i < lengths.Length; i++)
            {
                if (lengths[i] == 0)
                    continue;

                CurrentSpeeds.GetArrayElementAtIndex(i).floatValue = averageSpeed * lengths[i] / averageLength;
            }

            TryCollapseArrays();
        }

        public static void InitializeSpeeds(int start)
        {
            var count = CurrentSpeeds.arraySize;
            while (start < count)
                CurrentSpeeds.GetArrayElementAtIndex(start++).floatValue = 1;
        }

        public static void TryCollapseArrays()
        {
            if (CurrentProperty == null ||
                CurrentProperty.hasMultipleDifferentValues)
                return;

            TryCollapseSpeeds();
            TryCollapseSync();
        }

        public static void TryCollapseSpeeds()
        {
            var property = CurrentSpeeds;
            if (property == null)
                return;

            var speedCount = property.arraySize;
            if (speedCount <= 0)
                return;

            for (int i = 0; i < speedCount; i++)
            {
                if (property.GetArrayElementAtIndex(i).floatValue != 1)
                    return;
            }

            property.arraySize = 0;
        }

        public static void TryCollapseSync()
        {
            var property = CurrentSynchronizeChildren;
            if (property == null)
                return;

            var count = property.arraySize;
            var changed = false;

            for (int i = count - 1; i >= 0; i--)
            {
                if (property.GetArrayElementAtIndex(i).boolValue)
                {
                    count = i;
                    changed = true;
                }
                else
                {
                    break;
                }
            }

            if (changed)
                property.arraySize = count;
        }

        public static bool IsClipOrTransition(object clipOrTransition) => clipOrTransition is AnimationClip || clipOrTransition is ITransition;


        #endregion

        #region Method
        protected void AddPropertyModifierFunction(GenericMenu menu, string label, MenuFunctionState state, Action<SerializedProperty> function)
        {
            Serialization.AddPropertyModifierFunction(menu, CurrentProperty, label, state, (property) =>
            {
                GatherSubProperties(property);
                function(property);
            });
        }

        protected virtual void GatherSubProperties(SerializedProperty property)
        {
            CurrentProperty = property;
            CurrentAnimations = property.FindPropertyRelative(_AnimationsField);
            CurrentSpeeds = property.FindPropertyRelative(_SpeedsField);
            CurrentWeights = property.FindPropertyRelative(_WeightsField);
            CurrentSynchronizeChildren = property.FindPropertyRelative(_SynchronizeChildrenField);

            if (!property.hasMultipleDifferentValues && CurrentAnimations != null && CurrentSpeeds != null && CurrentSpeeds.arraySize != 0)
            {
                CurrentSpeeds.arraySize = CurrentAnimations.arraySize;
            }
            if (!property.hasMultipleDifferentValues && CurrentAnimations != null && CurrentWeights != null && CurrentWeights.arraySize != 0)
            {
                CurrentWeights.arraySize = CurrentAnimations.arraySize;
            }
        }

        protected virtual void ResizeList(int size)
        {
            CurrentAnimations.arraySize = size;
            if(CurrentWeights != null)
                CurrentWeights.arraySize = size;

            if (CurrentSpeeds.arraySize > size)
                CurrentSpeeds.arraySize = size;

            if (CurrentSynchronizeChildren.arraySize > size)
                CurrentSynchronizeChildren.arraySize = size;
        }


        #endregion

        #endregion


    }

    [UnityEditor.CustomEditor(typeof(MixerManualTransitionAsset), true)]
    public class MixerManualTransitionAssetEditor : MixerTransitionAssetEditor<MixerManualTransitionAsset>
    {
        protected override float FadeDuration { get => asset.transition.FadeDuration; set => asset.transition.FadeDuration = value; }

        public override void InitTransition()
        {
            transition = asset.transition;
        }
    }
}
