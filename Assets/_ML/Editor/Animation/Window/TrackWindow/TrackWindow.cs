using Animancer;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Animancer.Editor;
using static Animancer.Editor.AnimancerGUI;

namespace ML.Editor.Animation
{
    public partial class TrackWindow : EditorWindow, IHasCustomMenu
    {
        public static TrackWindow Instance
        {
            get;
            private set;
        }

        private static GUIContent _FirstKeyButtonContent, _LastKeyButtonContent;
        public static GUIContent FirstKeyButtonContent => IconContent(ref _FirstKeyButtonContent, "Animation.FirstKey");
        public static GUIContent LastKeyButtonContent => IconContent(ref _LastKeyButtonContent, "Animation.LastKey");

        /// <summary>
        /// true => Frame
        /// false => Seconds
        /// </summary>
        public bool ShowTimeWay = true;

        /// <summary>
        /// 时间轴
        /// </summary>
        private Timeline _timeline = new Timeline();

        private void OnGUI()
        {
            AnimationWindow.GetCurrentEvent = Event.current;
            if (AnimationWindow.Instance != null && AnimationWindow.Instance.AssetEditor != null)
            {
                DrawTrackMenuGUI();
            }
            else
            {
                EditorGUILayout.LabelField("没有选中动画资产");
            }
        }

        private void OnEnable()
        {
            Instance = this;
        }

        private void OnDisable()
        {
            if (Instance == this)
                Instance = null;
        }

        private void DrawTrackMenuGUI()
        {
            var animancer = PreviewWindow.Instance.GetScene.Animancer;
            var anims = PreviewWindow.Instance.GetAnimations;

            // 动画播放控制
            if (animancer != null)
            {
                using (new EditorGUI.DisabledScope(!PreviewWindow.Transition.IsValid()))
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();

                    #region TrackMenu
                    // 到第一帧
                    if (CompactMiniButton(FirstKeyButtonContent))
                        anims.StepToFirstKey();

                    // 回退一帧
                    if (CompactMiniButton(StepBackwardButtonContent))
                        anims.StepBackward();

                    // 重新播放 | 暂停播放
                    if (animancer.IsGraphPlaying)
                    {
                        // 暂停播放
                        if (CompactMiniButton(PauseButtonContent))
                            animancer.PauseGraph();
                    }
                    else
                    {
                        if (CompactMiniButton(PlayButtonContent))
                            anims.PlaySequence(animancer);
                    }

                    // 前进一帧
                    if (CompactMiniButton(StepForwardButtonContent))
                        anims.StepForward();

                    // 到最后一帧
                    if (CompactMiniButton(LastKeyButtonContent))
                        anims.StepToLastKey();

                    // 当前播放帧数
                    // Frame
                    EditorGUI.BeginChangeCheck();
                    float num = anims.NormalizedTime * anims.Length * (ShowTimeWay ? anims.FrameRate : 1);
                    num = ShowTimeWay ? (int)num : (int)(num * 10000) * 0.0001f;
                    num = EditorGUILayout.FloatField(num, GUILayout.Width(60)) / anims.Length / (ShowTimeWay ? anims.FrameRate : 1);
                    if (EditorGUI.EndChangeCheck())
                    {
                        anims.NormalizedTime = num;
                    }
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();

                    #endregion

                    #region Track Timeline
                    // 时间轴 当前Cursor
                    _timeline.DrawTimelineGUI();
                    #endregion
                }

            }
        }


        void IHasCustomMenu.AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("时间显示方式/Frame"), ShowTimeWay, () =>
            {
                ShowTimeWay = true;
            });
            menu.AddItem(new GUIContent("时间显示方式/Seconds"), !ShowTimeWay, () =>
            {
                ShowTimeWay = false;
            });
        }
    }



}