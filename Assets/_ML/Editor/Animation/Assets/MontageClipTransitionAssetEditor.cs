using Animancer.Editor;
using ML.Engine.Animation;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;
using static ML.Engine.Animation.IAssetHasEvents;
using static ML.Engine.Animation.MontageClipTransitionAsset;

namespace ML.Editor.Animation
{
    [UnityEditor.CustomEditor(typeof(MontageClipTransitionAsset), true)]
    public class MontageClipTransitionAssetEditor : AnimationAssetBaseEditor
    {
        protected EventTrack eventTrack;

        protected MontageTrack montageTrack;

        protected MontageClipTransitionAsset asset;
        // 动画Property
        protected SerializedProperty _clipProperty;
        // 事件
        protected SerializedProperty _endEventProperty;

        public override void Init()
        {
            asset = (MontageClipTransitionAsset)target;
            if (asset.montageNames == null)
            {
                asset.montageNames = new List<MontageName>();
            }


            eventTrack = new EventTrack(asset);
            montageTrack = new MontageTrack(asset, asset.montageNames, asset.FrameLength);
            var p = serializedObject.FindProperty("transition");
            _clipProperty = p.FindPropertyRelative("_Clip");
            _endEventProperty = serializedObject.FindProperty("_EndEvent");


        }

        private void OnEnable()
        {
            asset = (MontageClipTransitionAsset)target;
        }


        public override void DrawTrackGUI()
        {
            eventTrack.DrawTrackGUI();
            // MontageTrack
            montageTrack.DrawTrackGUI();
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

                DoMontageNameSelectionGUI();

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


        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            DoMontageNameSelectionGUI();
        }

        private void DoMontageNameSelectionGUI()
        {
            if(asset.montageNames != null)
            {
                // StringField(DropDown)，Play MontageName
                // 所有的标签 + All
                string[] names = new string[asset.montageNames.Count + 1];
                names[0] = "All";
                asset.SortMontage();
                int selectedIndex = 0;
                for (int i = 1; i < asset.montageNames.Count + 1; ++i)
                {
                    names[i] = asset.montageNames[i - 1].Name;
                    if (asset.DefaultName == names[i])
                    {
                        selectedIndex = i;
                    }
                }
                EditorGUI.BeginChangeCheck();
                selectedIndex = EditorGUILayout.Popup(new GUIContent("默认播放动画"), selectedIndex, names);
                if (EditorGUI.EndChangeCheck())
                {
                    asset.DefaultName = selectedIndex > 0 ? asset.montageNames[selectedIndex - 1].Name : names[0];
                }
            }
        }
    }

    [System.Serializable]
    public class MontageTrack : TrackWindow.Track
    {
        protected List<MontageName> Montages;
        protected AnimationAssetBase _asset;

        public MontageTrack(AnimationAssetBase target, List<MontageName> montages, float endtime) : base()
        {
            this.MainColor = new Color(0.3f, 0.3f, 0.3f, 1);

            this.BotColor = new Color(0, 0.8f, 0.8f, 0.5f);

            this.TextColor = new Color(1, 1, 1, 1);

            _asset = target;

            this.Name = "Montage Track";
            this.Start = 0;
            this.End = endtime;
            Montages = montages;
 

            if (montages != null)
            {
                int length = montages.Count;

                for (int i = 0; i < length; ++i)
                {
                    var signal = CreateSignal<MontageTrackSignal>();
                    signal.Montage = montages[i];
}
            }

        }

        protected override void AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("新建Montage"), false, () =>
            {
                var signal = this.CreateSignalOnMouse<MontageTrackSignal>();
                Montages.Add(signal.Montage);
            });
        }

        public override void DrawTrackGUI()
        {
            this.End = _asset.FrameLength;
            base.DrawTrackGUI();
        }


        [System.Serializable]
        internal class MontageTrackSignal : TrackWindow.TrackSignal
        {
            public List<MontageName> _target => ((MontageTrack)AttachedTrack).Montages;
            public AnimationAssetBase _asset => ((MontageTrack)AttachedTrack)._asset;

            public override string Name { get => Montage.Name; set => Montage.Name = value; }
            public override float NormalizedTime { get => Montage.NormalizedTime; set => Montage.NormalizedTime = value; }
            public MontageName Montage;

            public MontageTrackSignal()
            {
                Montage = new MontageName();
            }

            public override void OnDelete()
            {
                _target.Remove(Montage);
            }
        }
    }
}
