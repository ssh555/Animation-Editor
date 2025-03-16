using Animancer;
using Animancer.Editor;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static Animancer.Editor.AnimancerGUI;

namespace ML.Editor.Animation
{
    partial class PreviewWindow
    {
        [Serializable]
        public class Animations
        {
            public const string PreviousAnimationKey = "Previous Animation";
            public const string NextAnimationKey = "Next Animation";

            /// <summary>
            /// 预览模型上的其他AnimationClip
            /// </summary>
            [NonSerialized] private AnimationClip[] _OtherAnimations;

            [SerializeField]
            private AnimationClip _PreviousAnimation;
            public AnimationClip PreviousAnimation => _PreviousAnimation;

            [SerializeField]
            private AnimationClip _NextAnimation;
            public AnimationClip NextAnimation => _NextAnimation;


            /// <summary>
            /// 动画播放的规约化时间
            /// </summary>
            [SerializeField]
            private float _NormalizedTime;
            /// <summary>
            /// 动画播放的规约化时间
            /// </summary>
            public float NormalizedTime
            {
                get => _NormalizedTime;
                set
                {
                    if (!value.IsFinite())
                        return;

                    _NormalizedTime = value;
                    
                    // 尝试播放当前动画
                    if (!TryShowTransitionPaused(out var animancer, out var transition, out var state))
                        return;

                    // 动画长度
                    var length = state.Length;
                    // 播放速度
                    var speed = state.Speed;
                    // 当前播放时间点
                    var time = value * length;
                    // 平滑过渡时长
                    var fadeDuration = transition.FadeDuration * Math.Abs(speed);

                    // 动画开始时间
                    var startTime = TimelineGUI.GetStartTime(transition.NormalizedStartTime, speed, length);
                    // 规约化的结束时间
                    var normalizedEndTime = state.NormalizedEndTime;
                    // 动画结束时间
                    var endTime = normalizedEndTime * length;
                    // 平滑过渡结束时间
                    var fadeOutEnd = TimelineGUI.GetFadeOutEnd(speed, endTime, length);

                    // 反向播放
                    if (speed < 0)
                    {
                        time = length - time;
                        startTime = length - startTime;
                        value = 1 - value;
                        normalizedEndTime = 1 - normalizedEndTime;
                        endTime = length - endTime;
                        fadeOutEnd = length - fadeOutEnd;
                    }

                    // 播放 Previous Animation
                    if (time < startTime)
                    {
                        if (_PreviousAnimation != null)
                        {
                            PlayOther(PreviousAnimationKey, _PreviousAnimation, value);
                            value = 0;
                        }
                    }
                    // 平滑过渡 Fade from previous animation to the target
                    else if (time < startTime + fadeDuration)
                    {
                        if (_PreviousAnimation != null)
                        {
                            // 播放前一个动画
                            var fromState = PlayOther(PreviousAnimationKey, _PreviousAnimation, value);
                            // 启用播放
                            state.IsPlaying = true;
                            // 设置权重 -> 平滑过渡
                            state.Weight = (time - startTime) / fadeDuration;
                            fromState.Weight = 1 - state.Weight;
                        }
                    }
                    // 播放 Next Animation
                    else if (_NextAnimation != null)
                    {
                        if (value < normalizedEndTime)
                        {
                            // Just the main state.
                        }
                        // 平滑过渡
                        else
                        {
                            var toState = PlayOther(NextAnimationKey, _NextAnimation, value - normalizedEndTime);
                            // Fade from the target transition to the next animation.
                            if (time < fadeOutEnd)
                            {
                                state.IsPlaying = true;
                                toState.Weight = (time - endTime) / (fadeOutEnd - endTime);
                                state.Weight = 1 - toState.Weight;
                            }
                            // Else just the next animation.
                        }
                    }

                    // 反向播放
                    if (speed < 0)
                    {
                        value = 1 - value;
                    }

                    state.NormalizedTime = state.Weight > 0 ? value : 0;
                    animancer.Evaluate();

                    RepaintEverything();
                }
            }

            public float Length
            {
                get
                {
                    var state = Instance._Scene.Animancer.States.Current;
                    float length = state.Clip == null ? 1 : state.Clip.length;
                    for (int i = 0; i < state.ChildCount; ++i)
                    {
                        float tmp = state.GetChild(i).Clip == null ? 1 : state.GetChild(i).Clip.length;
                        if (tmp > length)
                        {
                            length = tmp;
                        }
                    }
                    return length;
                }
            }

            public float FrameRate
            {
                get
                {
                    var state = Instance._Scene.Animancer.States.Current;
                    float frameRate = state.Clip == null ? 1 : state.Clip.frameRate;
                    for(int i = 0; i < state.ChildCount; ++i)
                    {
                        float tmp = state.GetChild(i).Clip == null ? 1 : state.GetChild(i).Clip.frameRate;
                        if(tmp > frameRate)
                        {
                            frameRate = tmp;
                        }
                    }
                    return frameRate;
                }
            }


            /// <summary>
            /// 动画预览细节GUI
            /// </summary>
            public void DoGUI()
            {
                GUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.LabelField("Preview Details", "(Not Serialized)");

                // 模型选择GUI
                DoModelGUI();
                // Animator选择GUI -> 只有一个Animator就不显示
                DoAnimatorSelectorGUI();

                // 动画播放GUI
                // 前一个动画 Previous Animation
                using (ObjectPool.Disposable.AcquireContent(out var label, "Previous Animation",
                    "The animation for the preview to play before the target transition"))
                {
                    // 显示 Previous Animation Clip -> 可编辑
                    DoAnimationFieldGUI(label, ref _PreviousAnimation, (clip) => _PreviousAnimation = clip);
                }

                // 当前预览的动画
                var animancer = Instance._Scene.Animancer;
                DoCurrentAnimationGUI(animancer);

                // 下一个动画 Next Animation
                using (ObjectPool.Disposable.AcquireContent(out var label, "Next Animation",
                    "The animation for the preview to play after the target transition"))
                {
                    // 显示 Next Animation Clip -> 可编辑
                    DoAnimationFieldGUI(label, ref _NextAnimation, (clip) => _NextAnimation = clip);
                }

                // 动画播放控制
                if (animancer != null)
                {
                    using (new EditorGUI.DisabledScope(!Transition.IsValid()))
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();

                        // 正在播放
                        if (animancer.IsGraphPlaying)
                        {
                            // 暂停播放
                            if (CompactMiniButton(PauseButtonContent))
                                animancer.PauseGraph();
                        }
                        // 尚未播放
                        else
                        {
                            // 回退一帧
                            if (CompactMiniButton(StepBackwardButtonContent))
                                StepBackward();

                            // 重新播放
                            if (CompactMiniButton(PlayButtonContent))
                                PlaySequence(animancer);

                            // 前进一帧
                            if (CompactMiniButton(StepForwardButtonContent))
                                StepForward();
                        }

                        GUILayout.FlexibleSpace();
                        GUILayout.EndHorizontal();
                    }
                }

                GUILayout.EndVertical();
            }

            /// <summary>
            /// 选择模型
            /// </summary>
            private void DoModelGUI()
            {
                // 选中模型的根物体(不是Instance)
                var root = Instance._Scene.OriginalRoot;
                var model = root != null ? root.gameObject : null;

                EditorGUI.BeginChangeCheck();

                // model warning 信息字符串
                var warning = GetModelWarning(model);
                // 警告信息颜色
                var color = GUI.color;
                if (warning != null)
                    GUI.color = WarningFieldColor;

                using (ObjectPool.Disposable.AcquireContent(out var label, "Model"))
                {
                    // 模型选择下拉列表
                    if (DoDropdownObjectField(label, true, ref model, SpacingMode.After))
                    {
                        // 下拉菜单
                        var menu = new GenericMenu();

                        // 默认人形模型
                        menu.AddItem(new GUIContent("Default Humanoid"), Settings.IsDefaultHumanoid(model), () => Instance._Scene.OriginalRoot = Settings.DefaultHumanoid.transform);
                        // 模型2D Sprite
                        menu.AddItem(new GUIContent("Default Sprite"), Settings.IsDefaultSprite(model), () => Instance._Scene.OriginalRoot = Settings.DefaultSprite.transform);

                        // 自添加模型
                        var persistentModels = Settings.Models;
                        var temporaryModels = TemporarySettings.PreviewModels;
                        if (persistentModels.Count == 0 && temporaryModels.Count == 0)
                        {
                            menu.AddDisabledItem(new GUIContent("No model prefabs have been used yet"));
                        }
                        else
                        {
                            AddModelSelectionFunctions(menu, persistentModels, model);
                            AddModelSelectionFunctions(menu, temporaryModels, model);
                        }

                        // 显示菜单下拉列表
                        menu.ShowAsContext();
                    }
                }


                // 有模型选择修改
                if (EditorGUI.EndChangeCheck())
                    Instance._Scene.OriginalRoot = model != null ? model.transform : null;

                // 显示警告
                if (warning != null)
                    EditorGUILayout.HelpBox(warning, MessageType.Warning, true);

                GUI.color = color;
            }

            private void DoAnimatorSelectorGUI()
            {
                // 预览模型的实例上的所有的Amimator
                var instanceAnimators = Instance._Scene.InstanceAnimators;
                // 只有一个Animator就不显示
                if (instanceAnimators == null ||
                    instanceAnimators.Length <= 1)
                    return;

                var area = LayoutSingleLineRect(SpacingMode.After);
                var labelArea = StealFromLeft(ref area, EditorGUIUtility.labelWidth, StandardSpacing);
                GUI.Label(labelArea, nameof(Animator));

                // 当前选中的Animator
                var selectedAnimator = Instance._Scene.SelectedInstanceAnimator;

                // 选择Animator下拉列表
                using (ObjectPool.Disposable.AcquireContent(out var label, selectedAnimator != null ? selectedAnimator.name : "None"))
                {
                    var clicked = EditorGUI.DropdownButton(area, label, FocusType.Passive);

                    if (!clicked)
                        return;

                    // 下拉菜单
                    var menu = new GenericMenu();

                    for (int i = 0; i < instanceAnimators.Length; i++)
                    {
                        var animator = instanceAnimators[i];
                        var index = i;
                        menu.AddItem(new GUIContent(animator.name), animator == selectedAnimator, () =>
                        {
                            // 选中选择的Animator
                            Instance._Scene.SetSelectedAnimator(index);
                            // 将播放时间重置为0
                            NormalizedTime = 0;
                        });
                    }
                    // 显示菜单
                    menu.ShowAsContext();
                }
            }

            /// <summary>
            /// 获取model的warning信息字符串
            /// </summary>
            /// <param name="model"></param>
            /// <returns></returns>
            private string GetModelWarning(GameObject model)
            {
                if (model == null)
                    return "No Model is selected so nothing can be previewed.";

                if (Instance._Scene.Animancer == null)
                    return "The selected Model has no Animator component.";

                return null;
            }

            /// <summary>
            /// 下拉列表
            /// </summary>
            /// <typeparam name="T">DropDown -> TObject</typeparam>
            /// <param name="label">DropDownText</param>
            /// <param name="showDropdown">是否为下拉列表</param>
            /// <param name="obj">DropDownSelectObject</param>
            /// <param name="spacingMode"></param>
            /// <returns></returns>
            private static bool DoDropdownObjectField<T>(GUIContent label, bool showDropdown, ref T obj, SpacingMode spacingMode = SpacingMode.None) where T : UnityEngine.Object
            {
                var area = LayoutSingleLineRect(spacingMode);

                var labelWidth = EditorGUIUtility.labelWidth;

                labelWidth += 2;
                area.xMin -= 1;

                var spacing = StandardSpacing;
                var labelArea = StealFromLeft(ref area, labelWidth - spacing, spacing);

                // 当前选中的model
                obj = (T)EditorGUI.ObjectField(area, obj, typeof(T), true);

                // 下拉列表
                if (showDropdown)
                {
                    return EditorGUI.DropdownButton(labelArea, label, FocusType.Passive);
                }
                // Label
                else
                {
                    GUI.Label(labelArea, label);
                    return false;
                }
            }

            /// <summary>
            /// 向menu中添加下拉选项
            /// </summary>
            /// <param name="menu"></param>
            /// <param name="models"></param>
            /// <param name="selected"></param>
            private static void AddModelSelectionFunctions(GenericMenu menu, List<GameObject> models, GameObject selected)
            {
                for (int i = models.Count - 1; i >= 0; i--)
                {
                    var model = models[i];
                    var path = AssetDatabase.GetAssetPath(model);
                    // 资产
                    if (!string.IsNullOrEmpty(path))
                        path = path.Replace('/', '\\');
                    // 场景中物体
                    else
                        path = model.name;

                    // 添加下拉选项
                    menu.AddItem(new GUIContent(path), model == selected, () => Instance._Scene.OriginalRoot = model.transform);
                }
            }

            /// <summary>
            /// 尝试播放当前动画并暂停
            /// </summary>
            /// <param name="animancer">用于播放动画的AnimancerPlayable</param>
            /// <param name="transition">播放的预览动画</param>
            /// <param name="state">播放动画的AnimancerState</param>
            /// <returns></returns>
            private bool TryShowTransitionPaused(out AnimancerPlayable animancer, out ITransitionDetailed transition, out AnimancerState state)
            {
                // 用于播放预览动画的Animancer
                animancer = Instance._Scene.Animancer;
                // 预览的动画
                transition = Transition as ITransitionDetailed;

                // 当前不能预览动画
                if (animancer == null || !transition.IsValid())
                {
                    state = null;
                    return false;
                }

                // 播放当前动画
                state = animancer.Play(transition, 0);
                OnPlayAnimation();
                animancer.PauseGraph();
                return true;
            }

            /// <summary>
            /// 新建动画播放Playable,清空Event(预览时不执行)
            /// </summary>
            public void OnPlayAnimation()
            {
                var animancer = Instance._Scene.Animancer;
                if (animancer == null || animancer.States.Current == null)
                    return;

                var state = animancer.States.Current;

                state.RecreatePlayableRecursive();

                if (state.HasEvents)
                {
                    // 警告
                    var warnings = OptionalWarning.UnsupportedEvents.DisableTemporarily();
                    //var normalizedEndTime = state.Events.NormalizedEndTime;
                    var end = state.Events.EndEvent;
                    // 清空事件 -> 预览动画时不执行Event
                    state.Events = null;
                    // 恢复当前播放时间
                    //state.Events.NormalizedEndTime = normalizedEndTime;
                    state.Events.EndEvent = end;
                    warnings.Enable();
                }
            }

            /// <summary>
            /// 播放动画 -> 用于播放前一个动画和后一个动画
            /// </summary>
            /// <param name="key"></param>
            /// <param name="animation"></param>
            /// <param name="normalizedTime"></param>
            /// <param name="fadeDuration"></param>
            /// <returns></returns>
            private AnimancerState PlayOther(object key, AnimationClip animation, float normalizedTime, float fadeDuration = 0)
            {
                var animancer = Instance._Scene.Animancer;
                // 播放动画的AnimancerState
                var state = animancer.States.GetOrCreate(key, animation, true);
                // 播放动画
                state = animancer.Play(state, fadeDuration);
                // 预览的动画播放处理
                OnPlayAnimation();

                // state 开始播放的实际时间
                normalizedTime *= state.Length;
                state.Time = normalizedTime.IsFinite() ? normalizedTime : 0;

                return state;
            }

            /// <summary>
            /// 显示 Animation Clip Field GUI
            /// 若OtherAnimations不为null，则可进行下拉框选择
            /// </summary>
            /// <param name="label"></param>
            /// <param name="clip"></param>
            /// <param name="setClip"></param>
            private void DoAnimationFieldGUI(GUIContent label, ref AnimationClip clip, Action<AnimationClip> setClip)
            {
                // 是否显示下拉列表-> 其他动画为null则不显示
                var showDropdown = !_OtherAnimations.IsNullOrEmpty();

                if (DoDropdownObjectField(label, showDropdown, ref clip))
                {
                    var menu = new GenericMenu();

                    menu.AddItem(new GUIContent("None"), clip == null, () => setClip(null));

                    for (int i = 0; i < _OtherAnimations.Length; i++)
                    {
                        var animation = _OtherAnimations[i];
                        menu.AddItem(new GUIContent(animation.name), animation == clip, () => setClip(animation));
                    }

                    menu.ShowAsContext();
                }
            }

            /// <summary>
            /// 显示当前动画的GUI
            /// </summary>
            /// <param name="animancer"></param>
            private void DoCurrentAnimationGUI(AnimancerPlayable animancer)
            {
                string text;

                if (animancer != null)
                {
                    // 当前动画
                    var transition = Transition;
                    if (transition.IsValid() && transition.Key != null)
                        text = animancer.States.GetOrCreate(transition).ToString();
                    else
                        text = transition.ToString();
                }
                else
                {
                    text = Instance._selectedAsset.name;
                }

                if (text != null)
                    EditorGUILayout.LabelField("Current Animation", text);
            }

            /// <summary>
            /// 回退动画一帧
            /// </summary>
            public void StepBackward()
                => StepTime(-(1 / FrameRate));

            /// <summary>
            /// 前进动画一帧
            /// </summary>
            public void StepForward()
                => StepTime((1 / FrameRate));

            /// <summary>
            /// 基于当前动画更改时间步长
            /// </summary>
            /// <param name="timeOffset"></param>
            private void StepTime(float timeOffset)
            {
                // 尝试播放
                if (!TryShowTransitionPaused(out _, out _, out var state))
                    return;

                var length = state.Length;
                // 修正为规约化时间
                if (length != 0)
                    timeOffset /= length;

                // 应用当前TimeOffset
                NormalizedTime += timeOffset;
            }

            /// <summary>
            /// 回到第一帧
            /// </summary>
            public void StepToFirstKey()
            {
                // 尝试播放
                if (!TryShowTransitionPaused(out _, out _, out var state))
                    return;

                // 应用当前TimeOffset
                NormalizedTime = 0;
            }

            /// <summary>
            /// 到最后一帧
            /// </summary>
            public void StepToLastKey()
            {
                // 尝试播放
                if (!TryShowTransitionPaused(out _, out _, out var state))
                    return;

                // 应用当前TimeOffset
                NormalizedTime = 1;
            }

            /// <summary>
            /// 基于 Previous Animation 播放当前动画
            /// </summary>
            /// <param name="animancer"></param>
            public void PlaySequence(AnimancerPlayable animancer)
            {
                if (_PreviousAnimation != null && _PreviousAnimation.length > 0)
                {
                    // 暂停当前播放
                    Instance._Scene.Animancer.Stop();
                    // Previous Animancer State
                    var fromState = animancer.States.GetOrCreate(PreviousAnimationKey, _PreviousAnimation, true);
                    // 播放 Previous
                    animancer.Play(fromState);
                    OnPlayAnimation();
                    fromState.TimeD = 0;

                    var warnings = OptionalWarning.UnsupportedEvents.DisableTemporarily();
                    // Previous 播放结束时播放当前动画
                    fromState.Events.EndEvent = new AnimancerEvent(1 / fromState.Length, PlayTransition);
                    warnings.Enable();
                }
                // 直接播放当前动画
                else
                {
                    PlayTransition();
                }

                // 播放
                Instance._Scene.Animancer.UnpauseGraph();
            }

            /// <summary>
            /// 播放当前动画
            /// </summary>
            private void PlayTransition()
            {
                var transition = Transition;
                var animancer = Instance._Scene.Animancer;
                // 获取旧的播放状态
                animancer.States.TryGet(transition, out var oldState);

                // 销毁旧状态
                if (oldState != null)
                    oldState.Destroy();

                // 播放新的动画的状态
                var targetState = animancer.Play(transition);
                if (_PreviousAnimation == null || _PreviousAnimation.length == 0)
                {
                    targetState.Weight = 1;
                }

                OnPlayAnimation();


                // 警告
                var warnings = OptionalWarning.UnsupportedEvents.DisableTemporarily();
                //targetState.NormalizedTime = 0;
                // 播放结束事件
                //targetState.NormalizedEndTime = 1;
                targetState.Events.OnEnd = () =>
                {
                    // 播放下一个动画 Next Animation
                    if (_NextAnimation != null)
                    {
                        var fadeDuration = AnimancerEvent.GetFadeOutDuration(targetState, AnimancerPlayable.DefaultFadeDuration);
                        PlayOther(NextAnimationKey, _NextAnimation, 0, fadeDuration);
                        OnPlayAnimation();
                    }
                    else
                    {
                        //// TODO: 不可访问
                        //animancer.Layers[0].IncrementCommandCount();
                        animancer.PauseGraph();
                    }
                };
                warnings.Enable();
            }
            /// <summary>
            /// 从 OriginalRoot 物体上收集 AnimationClip
            /// 并设置默认的 Previous || Next Animation
            /// </summary>
            public void GatherAnimations()
            {
                AnimationGatherer.GatherFromGameObject(Instance._Scene.OriginalRoot.gameObject, ref _OtherAnimations, true);

                if (_OtherAnimations.Length > 0 && (_PreviousAnimation == null || _NextAnimation == null))
                {
                    var defaultClip = _OtherAnimations[0];
                    var defaultClipIsIdle = false;

                    for (int i = 0; i < _OtherAnimations.Length; i++)
                    {
                        var clip = _OtherAnimations[i];

                        if (defaultClipIsIdle && clip.name.Length > defaultClip.name.Length)
                            continue;

                        if (clip.name.IndexOf("idle", StringComparison.CurrentCultureIgnoreCase) >= 0)
                        {
                            defaultClip = clip;
                            break;
                        }
                    }

                    if (_PreviousAnimation == null)
                        _PreviousAnimation = defaultClip;
                    if (_NextAnimation == null)
                        _NextAnimation = defaultClip;
                }
            }

            /// <summary>
            /// 让当前状态的播放时间与Animations中的规约化时间匹配
            /// </summary>
            internal class WindowMatchStateTime : Key, IUpdatable
            {
                public static readonly WindowMatchStateTime Instance = new WindowMatchStateTime();

                /// <summary>
                /// 每帧调用
                /// </summary>
                void IUpdatable.Update()
                {
                    //if (PreviewWindow.Instance == null ||
                    //    !AnimancerPlayable.Current.IsGraphPlaying)
                    //    return;
                    if (PreviewWindow.Instance == null)
                        return;

                    var transition = Transition;
                    if (transition == null)
                        return;

                    if (AnimancerPlayable.Current.States.TryGet(transition, out var state))
                    {
                        PreviewWindow.Instance._Animations._NormalizedTime = state.NormalizedTime;
                        if(state.IsPlaying)
                        {
                            TrackWindow.Instance.Repaint();
                        }
                    }

                }
            }
        }
    }
}

