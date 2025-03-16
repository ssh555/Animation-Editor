using Animancer;
using Animancer.Editor;
using ML.Engine.Animation;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace ML.Editor.Animation
{
    public abstract class MixerParameterTransitionAssetEditor<T> : MixerTransitionAssetEditor<T>
        where T : AnimationAssetBase, IAssetHasEvents
    {
        protected override string GetWeightsField() => "_Thresholds";
    }

    [UnityEditor.CustomEditor(typeof(Mixer1DTransitionAsset), true)]
    public class Mixer1DTransitionAssetEditor : MixerParameterTransitionAssetEditor<Mixer1DTransitionAsset>
    {
        private Timeline timeline { get; set; }
        protected override float FadeDuration { get => asset.transition.FadeDuration; set => asset.transition.FadeDuration = value; }

        protected SerializedProperty DefaultValue { get; set; }

        public override void Init()
        {
            base.Init();
            DefaultValue = serializedObject.FindProperty("transition").FindPropertyRelative("_DefaultParameter");
            timeline = new Timeline(DefaultValue, CurrentWeights);
        }

        public override void InitTransition()
        {
            transition = asset.transition;
        }

        private Vector2 scrollPosition;
        public override void DrawInEditorWindow(EditorWindow window)
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            serializedObject.Update();
            base.DrawInEditorWindow(window);
            DoThresholdGraphGUI(window);

            serializedObject.ApplyModifiedProperties();
            EditorGUILayout.EndScrollView();
        }

        protected void DoThresholdGraphGUI(EditorWindow window)
        {
            EditorGUILayout.PropertyField(DefaultValue, new GUIContent("默认参数"));
            timeline.DrawTimelineGUI(window);
        }


        internal class Timeline
        {
            public Timeline(SerializedProperty defaultValue, SerializedProperty weights)
            {
                Weights = weights;
                DefaultValue = defaultValue;
            }

            protected SerializedProperty Weights { get; set; }
            protected SerializedProperty DefaultValue { get; set; }


            /// <summary>
            /// 权重阈值1对应绘制的比例
            /// </summary>
            public const float weightRatio = 50;

            /// <summary>
            /// 时间轴缩放
            /// </summary>
            public float timelineScale = 1f;

            /// <summary>
            /// 最小刻度高度
            /// </summary>
            private const float minTickHeight = 20f;
            /// <summary>
            /// 最大刻度高度
            /// </summary>
            private const float maxTickHeight = 40f;
            /// <summary>
            /// 刻度间距
            /// </summary>
            public const float tickSpacing = 20f;
            /// <summary>
            /// 缩放后的刻度间距小于此值不绘制
            /// </summary>
            private const float tickInternal = 5f;
            /// <summary>
            /// 缩放后的刻度间距每隔此值就显示一次帧数
            /// </summary>
            private const float tickTimeInternal = 50f;

            private Color backgroundColor = new Color(0.2f, 0.8f, 0.6f, 0.4f);
            private Color timelineColor = new Color(0, 0.5f, 0.8f, 0.5f);

            private bool isInitScroll = false;
            private Vector2 scrollPositon = Vector2.zero;

            /// <summary>
            /// -2 -> None
            /// -1 -> 默认值
            /// >0 -> 权重值
            /// </summary>
            private int selected = -2;

            public void DrawTimelineGUI(EditorWindow window)
            {
                scrollPositon = EditorGUILayout.BeginScrollView(scrollPositon, alwaysShowHorizontal: false, alwaysShowVertical : false, GUILayout.Height(65),  GUILayout.ExpandWidth(true));

                #region 绘制区域数据
                // 计算轴宽度
                int size = Weights.arraySize;
                // 权重值
                float[] weights = new float[size];
                float minW = float.MaxValue, maxW = float.MinValue;
                for(int i = 0; i < size; ++i)
                {
                    weights[i] = Weights.GetArrayElementAtIndex(i).floatValue;
                    minW = Mathf.Min(minW, weights[i]);
                    maxW = Mathf.Max(maxW, weights[i]);
                }
                // 轴宽度
                float timelineWidth = (maxW - minW + 2) * weightRatio * timelineScale * 5;
                if(size == 0)
                {
                    timelineWidth = 2 * weightRatio * timelineScale * 5;
                    maxW = 1;
                    minW = -1;
                }
                // 区域
                Rect area = GUILayoutUtility.GetRect(timelineWidth, 50);
                EditorGUI.DrawRect(area, backgroundColor);
                float showWidth = window.position.width;
                if (area.width <= timelineWidth)
                {
                    area = new Rect(area.center.x - timelineWidth * 0.5f, area.y, timelineWidth, 50);
                }
                // 上中心零点
                Vector2 topcenter = area.center;
                topcenter.y = area.y;


                // 绘制时间轴刻度 -> 包括小数
                int tickCount = Mathf.FloorToInt(area.width / (tickSpacing * timelineScale));
                float lastx = -tickInternal;
                // 上一次绘制的值
                float lastTX = -tickTimeInternal / timelineScale - 2;
                // [-tickCount/2, +tickCount/2]
                int halftickCount = tickCount / 2;
                for (int i = -halftickCount; i <= halftickCount; ++i) 
                {
                    float x = topcenter.x + i * tickSpacing * timelineScale;
                    if (x - lastx < tickInternal)
                    {
                        continue;
                    }
                    float height = Mathf.Lerp(minTickHeight, maxTickHeight, timelineScale);

                    if (x - lastTX >= tickTimeInternal)
                    {
                        GUI.Label(new Rect(x, topcenter.y + area.height - height - 10, 50, 20), (i * 0.1f).ToString("F1"));
                        timelineColor = Color.white;
                        lastTX = x;
                    }
                    else
                    {
                        height *= 0.5f;
                        timelineColor = new Color(1, 1, 1, 0.7f);
                    }
                    EditorGUI.DrawRect(new Rect(x, topcenter.y + area.height - height, 1, height), timelineColor);
                    lastx = x;
                }
                #endregion

                #region 绘制默认值
                float cursorX = topcenter.x + DefaultValue.floatValue * tickSpacing * timelineScale * 10;
                // 赋值ScrollPosition
                if (isInitScroll == false)
                {
                    isInitScroll = true;
                    scrollPositon.x = cursorX * tickSpacing * timelineScale * 10 + (timelineWidth - showWidth) * 0.5f;

                    timelineScale = 1 / (maxW - minW + 1);
                    timelineWidth = (maxW - minW + 2) * weightRatio * timelineScale * 5;
                    scrollPositon.x = cursorX;// + (timelineSize - showSize) * 0.5f;
                    scrollPositon.x += (timelineWidth - showWidth) * 0.5f;
                }
                Rect cursorRect = new Rect(cursorX - 4, topcenter.y, 10, 30);
                Texture2D cursorTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_ML/Editor/Animation/Window/TrackWindow/Cursor.png");
                timelineColor = GUI.color;
                GUI.color = selected == -1 ? Color.red : Color.white;
                GUI.DrawTexture(cursorRect, cursorTexture);
                // 被选中，则显示当前值
                if (selected == -1)
                {
                    GUI.color = Color.white;
                    Rect rect = new Rect(cursorX, topcenter.y, 30, 10);
                    EditorGUI.LabelField(rect, (DefaultValue.floatValue).ToString("F2"));
                }
                GUI.color = timelineColor;

                #endregion

                #region 绘制权重值
                Texture2D weightTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_ML/Editor/Animation/Window/TrackWindow/TriangleCursor.png");
                Rect[] weightRects = new Rect[size];
                for(int i = 0; i < size; ++i)
                {
                    float weightX = topcenter.x + Weights.GetArrayElementAtIndex(i).floatValue * tickSpacing * timelineScale * 10;
                    Rect rect = new Rect(weightX - 4, topcenter.y, 10, 20);
                    weightRects[i] = rect;
                    timelineColor = GUI.color;
                    GUI.color = selected == i ? Color.red : Color.white;
                    GUI.DrawTexture(rect, weightTexture);
                    // 被选中，则显示当前值
                    if (selected == i)
                    {
                        GUI.color = Color.white;
                        rect = new Rect(weightX, topcenter.y, 30, 10);
                        EditorGUI.LabelField(rect, (Weights.GetArrayElementAtIndex(i).floatValue).ToString("F2"));
                    }
                    GUI.color = timelineColor;
                }
                #endregion

                #region 输入响应事件
                // 处理鼠标滚轮缩放
                if (Event.current.type == EventType.ScrollWheel)
                {
                    timelineScale -= Event.current.delta.y * 0.01f;
                    timelineScale = Mathf.Max(0.1f, timelineScale);
                    window.Repaint();
                }

                Event currentEvent = Event.current;
                var mpos = Event.current.mousePosition;

                // 检查鼠标拖动事件
                if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0)
                {
                    if(cursorRect.Contains(mpos))
                    {
                        selected = -1;
                        currentEvent.Use();
                        window.Repaint();
                    }
                    else
                    {
                        for(int i = 0; i < size; ++i)
                        {
                            if(weightRects[i].Contains(mpos))
                            {
                                selected = i;
                                currentEvent.Use();
                                window.Repaint();
                            }
                        }
                    }
                }
                else if (currentEvent.type == EventType.MouseUp && currentEvent.button == 0)
                {
                    selected = -2;
                    currentEvent.Use();
                }
                else if (currentEvent.type == EventType.MouseDrag)
                {
                    // 拖动默认值
                    if(selected == -1)
                    {
                        DefaultValue.floatValue = (mpos.x - topcenter.x) / timelineScale / tickSpacing / 10;
                        window.Repaint();
                        currentEvent.Use();
                    }
                    // 拖动权重值
                    else if(selected >= 0)
                    {
                        Weights.GetArrayElementAtIndex(selected).floatValue = (mpos.x - topcenter.x) / timelineScale / tickSpacing / 10;
                        window.Repaint();
                        currentEvent.Use();
                    }
                }

                // 双击重置视图
                if (currentEvent.type == EventType.MouseDown && area.Contains(mpos) && currentEvent.clickCount >= 2)
                {
                    isInitScroll = false;
                }
                #endregion

                EditorGUILayout.EndScrollView();
            }

        }
    }

}
