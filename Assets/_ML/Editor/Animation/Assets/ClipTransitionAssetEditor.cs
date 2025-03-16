using ML.Engine.Animation;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using Animancer.Editor;
using UnityEditor.VersionControl;
using System.Linq;

namespace ML.Editor.Animation
{
    [UnityEditor.CustomEditor(typeof(ClipTransitionAsset), true)]
    public class ClipTransitionAssetEditor : AnimationAssetBaseEditor
    {
        protected EventTrack eventTrack;

        protected ClipTransitionAsset asset;
        // 动画Property
        protected SerializedProperty _clipProperty;
        // 事件
        protected SerializedProperty _endEventProperty;

        public override void Init()
        {
            asset = (ClipTransitionAsset)target;
            eventTrack = new EventTrack(asset);
            var p = serializedObject.FindProperty("transition");
            _clipProperty = p.FindPropertyRelative("_Clip");
            _endEventProperty = serializedObject.FindProperty("_EndEvent");
        }


        public override void DrawTrackGUI()
        {
            eventTrack.DrawTrackGUI();
        }

        private bool bShowFadeDuration = true;
        private bool bStartTime = true;
        private bool bEndTime = true;
        public override void DrawInEditorWindow(EditorWindow window)
        {
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();

            // 动画选择
            EditorGUILayout.PropertyField(_clipProperty, new GUIContent("动画片段"), true);
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }

            if (_clipProperty.objectReferenceValue != null)
            {
                float length = asset.transition.Clip.length;
                float frameRate = asset.transition.Clip.frameRate;
                float speed = asset.transition.Speed;
                // 帧率
                DoClipFrameGUI(asset.transition.Clip);

                // Speed -> 播放速度
                DoSpeedGUI(asset.transition);

                #region 时间轴 -> 使用秒数时间
                EditorGUILayout.Space();

                DoAnimTimelineGUI(asset.transition, _endEventProperty.GetValue<IAssetHasEvents.AssetEvent>().NormalizedTime, length, frameRate);
                EditorGUILayout.Space();
                #endregion

                // Fade Duration -> 过渡时间
                asset.transition.FadeDuration = DoFadeDurationGUI(asset.transition, length, frameRate, ref bShowFadeDuration);

                // StartTime -> 开始时间
                DoStartTimeGUI(asset.transition, length, frameRate, (float.IsNaN(speed) || speed >= 0) ? 0 : 1, ref bStartTime);

                // End Time -> 结束时间
                DoEndTimeGUI(_endEventProperty, length, frameRate, (float.IsNaN(speed) || speed >= 0) ? 1 : 0, ref bEndTime);
            }
            
            serializedObject.ApplyModifiedProperties();
        }



    }

}
