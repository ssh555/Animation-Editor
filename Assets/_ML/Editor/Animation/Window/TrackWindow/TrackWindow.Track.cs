using Codice.Client.Common;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static ML.Editor.Animation.DetailWindow;

namespace ML.Editor.Animation
{
    partial class TrackWindow
    {
        public abstract class Track
        {
            public const float TrackHeight = 40;

            /// <summary>
            /// 开始位置
            /// </summary>
            public float Start = 0;
            /// <summary>
            /// 结束位置
            /// </summary>
            public float End = 60;

            /// <summary>
            /// 轨道显示名字
            /// </summary>
            public string Name = "Track";

            /// <summary>
            /// 轨道主颜色
            /// </summary>
            public Color MainColor = new Color(0.3f, 0.3f, 0.3f, 1);
            /// <summary>
            /// 轨道底部颜色
            /// </summary>
            public Color BotColor = new Color(0, 0.2f, 0.8f, 0.5f);
            /// <summary>
            /// 字体颜色
            /// </summary>
            public Color TextColor = new Color(1, 1, 1, 1);
            /// <summary>
            /// 轨道标记
            /// </summary>
            private List<TrackSignal> Siganls = new List<TrackSignal>();

            public Rect PaintRect { get; private set; }

            private Vector2 mousepos;
            public virtual void DrawTrackGUI()
            {
                // 绘制Track
                Rect rect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.ExpandWidth(true), GUILayout.Height(TrackHeight));
                rect.x += 10;
                rect.y += 2;
                PaintRect = rect;
                // 主体颜色
                Rect mainRect = new Rect(rect.x + Start, rect.y, (Timeline.tickSpacing * Instance._timeline.timelineScale) * (End - Start), rect.height);
                EditorGUI.DrawRect(mainRect, MainColor);
                // 底部颜色
                Rect botRect = new Rect(rect.x + Start, rect.y + rect.height * 0.85f, (Timeline.tickSpacing * Instance._timeline.timelineScale) * (End - Start), rect.height * 0.15f);
                EditorGUI.DrawRect(botRect, BotColor);

                // 在矩形上绘制字符串
                GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
                labelStyle.alignment = TextAnchor.MiddleLeft;
                labelStyle.normal.textColor = TextColor;

                // 确保字符串在矩形的中心绘制
                Rect textRect = mainRect;
                EditorGUI.LabelField(textRect, Name, labelStyle);

                // 绘制Signal
                foreach (var signal in Siganls.ToArray())
                {
                    signal.DrawGUI(rect);
                }

                // 处理鼠标右键点击
                if (Event.current.type == EventType.MouseDown && Event.current.button == 1 && rect.Contains(Event.current.mousePosition))
                {
                    bool IsContain = false;
                    foreach(var signal in Siganls)
                    {
                        if(signal.PaintRect.Contains(Event.current.mousePosition))
                        {
                            signal.ShowContextMenu();
                            IsContain = true;
                            break;
                        }
                    }
                    if(!IsContain)
                    {
                        mousepos = Event.current.mousePosition;
                        ShowContextMenu();
                    }
                }

                //if(Event.current.control && Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.S)
                //{
                //    SaveData();
                //}
            }

            /// <summary>
            /// 右键菜单项
            /// </summary>
            private void ShowContextMenu()
            {
                // 创建一个新的菜单
                GenericMenu menu = new GenericMenu();

                AddItemsToMenu(menu);

                // 显示菜单
                menu.ShowAsContext();
            }

            protected virtual void AddItemsToMenu(GenericMenu menu)
            {

            }

            public T CreateSignal<T>() where T : TrackSignal
            {
                T signal = ScriptableObject.CreateInstance<T>();
                this.Siganls.Add(signal);
                signal.AttachedTrack = this;
                return signal;

            }

            public T CreateSignalOnMouse<T>() where T : TrackSignal, new()
            {
                return CreateSignal<T>(Mathf.Max((mousepos.x - 10), 0) / Instance._timeline.timelineScale / Timeline.tickSpacing / Instance._timeline.Framelength);
            }

            public T CreateSignalOnMouse<T>(out float time) where T : TrackSignal, new()
            {
                time = Mathf.Max((mousepos.x - 10), 0) / Instance._timeline.timelineScale / Timeline.tickSpacing / Instance._timeline.Framelength;
                return CreateSignal<T>();
            }

            public T CreateSignal<T>(float time) where T : TrackSignal, new()
            {
                var signal = CreateSignal<T>();
                signal.NormalizedTime = time;
                return signal;
            }

            public void DeleteSignal(TrackSignal signal)
            {
                if (this.Siganls.Remove(signal))
                {
                    signal.OnDelete();
                }
            }
        }

        /// <summary>
        /// 轨道标记
        /// </summary>
        [System.Serializable]
        public class TrackSignal : ScriptableObject, ISelection
        {
            /// <summary>
            /// 标记名称
            /// </summary>
            public virtual string Name { get; set; } = "New Signal";

            /// <summary>
            /// 位于轨道的哪个位置
            /// </summary>
            public virtual float NormalizedTime { get; set; } = 0;

            /// <summary>
            /// 绘制颜色
            /// </summary>
            public Color MainColor = Color.white;
            /// <summary>
            /// 被选中时的颜色
            /// </summary>
            public Color SelectedColor = Color.red;
            private bool IsSelected = false;

            /// <summary>
            /// 所属轨道
            /// </summary>
            public Track AttachedTrack;

            public Rect PaintRect;

            private bool isdragging = false;

            /// <summary>
            /// 绘制Signal
            /// </summary>
            public void DrawGUI(Rect rect)
            {
                float cursorX = rect.x + NormalizedTime * Timeline.tickSpacing * Instance._timeline.timelineScale * Instance._timeline.Framelength;
                Texture2D cursorTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_ML/Editor/Animation/Window/TrackWindow/Cursor.png");
                var color = GUI.color;
                GUI.color = IsSelected ? SelectedColor : MainColor;
                PaintRect = new Rect(cursorX - 4, rect.y, 10, 30);
                GUI.DrawTexture(PaintRect, cursorTexture);
                GUI.color = color;

                if (Event.current.button == 0 && Event.current.type == EventType.MouseDown && PaintRect.Contains(Event.current.mousePosition))
                {
                    DetailWindow.Instance.CurSelection = this;
                }
                else if(Event.current.type == EventType.MouseDown && !PaintRect.Contains(Event.current.mousePosition))
                {
                    Instance.Repaint();
                    DetailWindow.Instance.CurSelection = null;
                }

                // 被选中
                if (this.IsSelected)
                {
                    Event currentEvent = Event.current;

                    // 检查鼠标拖动事件
                    if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0 && rect.Contains(Event.current.mousePosition))
                    {
                        isdragging = true;
                        currentEvent.Use();
                        NormalizedTime = Mathf.Max((Event.current.mousePosition.x - AttachedTrack.PaintRect.x), 0) / Instance._timeline.timelineScale / Timeline.tickSpacing / Instance._timeline.Framelength;
                        Instance.Repaint();
                        DetailWindow.Instance.Repaint();
                    }
                    else if (isdragging && currentEvent.type == EventType.MouseUp && currentEvent.button == 0)
                    {
                        currentEvent.Use();
                    }
                    else if (isdragging && currentEvent.type == EventType.MouseDrag)
                    {
                        NormalizedTime = Mathf.Max((Event.current.mousePosition.x - AttachedTrack.PaintRect.x), 0) / Instance._timeline.timelineScale / Timeline.tickSpacing / Instance._timeline.Framelength;
                        Instance.Repaint();
                        DetailWindow.Instance.Repaint();
                        currentEvent.Use();
                    }

                    // 按下Delete键删除
                    if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Delete)
                    {
                        AttachedTrack.DeleteSignal(this);
                        Instance.Repaint();
                        DetailWindow.Instance.Repaint();
                    }
                }
            }

            public virtual void DoSelectedGUI()
            {
                GUILayout.BeginVertical();

                // 编辑并显示 Name 属性，限制宽度
                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("名字", GUILayout.Width(60));
                Name = EditorGUILayout.TextField(Name);
                GUILayout.EndHorizontal();

                // 编辑并显示 Time 属性，限制宽度
                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("时间点", GUILayout.Width(60));
                NormalizedTime = EditorGUILayout.FloatField(NormalizedTime);
                GUILayout.EndHorizontal();

                // 编辑并显示 MainColor 属性，限制宽度
                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("常规颜色", GUILayout.Width(60));
                MainColor = EditorGUILayout.ColorField(MainColor);
                GUILayout.EndHorizontal();

                // 编辑并显示 SelectedColor 属性，限制宽度
                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("选中颜色", GUILayout.Width(60));
                SelectedColor = EditorGUILayout.ColorField(SelectedColor);
                GUILayout.EndHorizontal();

                GUILayout.EndVertical();
            }



            /// <summary>
            /// 右键菜单项
            /// </summary>
            public void ShowContextMenu()
            {
                // 创建一个新的菜单
                GenericMenu menu = new GenericMenu();

                AddItemsToMenu(menu);

                // 显示菜单
                menu.ShowAsContext();
            }

            protected virtual void AddItemsToMenu(GenericMenu menu)
            {
                menu.AddItem(new GUIContent("取消选中"), false, () =>
                {
                    DetailWindow.Instance.CurSelection = null;
                });
                menu.AddItem(new GUIContent("删除"), false, () =>
                {
                    AttachedTrack.DeleteSignal(this);
                });
            }

            /// <summary>
            /// 被删除时的回调
            /// </summary>
            public virtual void OnDelete()
            {

            }

            public virtual void OnSelected()
            {
                IsSelected = true;
                Instance.Repaint();
            }

            public virtual void OnDeselected()
            {
                IsSelected = false;
            }
        }
    }
}

