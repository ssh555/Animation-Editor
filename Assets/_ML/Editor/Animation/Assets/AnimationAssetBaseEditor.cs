using UnityEditor.Callbacks;
using UnityEditor;
using UnityEngine;
using ML.Engine.Animation;
using static ML.Engine.Animation.IAssetHasEvents;


namespace ML.Editor.Animation
{
    [UnityEditor.CustomEditor(typeof(AnimationAssetBase), true)]
    public partial class AnimationAssetBaseEditor : UnityEditor.Editor
    {
        [OnOpenAsset(0)]
        public static bool ClickAction(int instanceID, int line)
        {
            AnimationAssetBase asset = EditorUtility.InstanceIDToObject(instanceID) as AnimationAssetBase;
            if (asset == null)
            {
                return false;
            }

            AnimationWindow.OpenWithAsset(asset);

            return true;
        }

        /// <summary>
        /// 用于在AnimationWindow窗口中绘制常规配置项(不包括Track)
        /// </summary>
        public virtual void DrawInEditorWindow(EditorWindow window)
        {
            //Debug.LogWarning($"{this.target.GetType()} 没有实现Editor");
            DrawDefaultInspector();
        }

        /// <summary>
        /// 绘制自定义的Track
        /// 需在此调用Track.DrawTrackGUI()
        /// </summary>
        public virtual void DrawTrackGUI()
        {
        }

        /// <summary>
        /// 等效于OnEnable -> 用于窗口打包这个资产时的初始化编辑器
        /// 用于初始化所使用的轨道
        /// </summary>
        public virtual void Init()
        {

        }

        public override void OnInspectorGUI()
        {
            //DrawInEditorWindow();
            EditorGUILayout.LabelField("请双击打开 Animation Window 进行编辑与查看");
            //base.OnInspectorGUI();
        }
    }

    [System.Serializable]
    public class EventTrack : TrackWindow.Track
    {
        public EventTrack(IAssetHasEvents transition) : base()
        {
            this.Name = "Event Track";
            
            this.Start = 0;
            this.End = transition.FrameLength;

            TargetTransition = transition;
            // 不能使用foreach: Events和EventName是分开存的的
            if(transition.Events != null)
            {
                int length = transition.Events.Count;
                //transition.Events[1].normalizedTime

                for (int i = 0; i < length; ++i)
                {
                    var signal = CreateSignal<EventTrackSignal>();
                    signal.Event = transition.Events[i];
                    //Debug.Log(i + " " + transition.Events[i]);
                }
                // OnEndEvent需要单独处理 -> 在AnimationWindow面板，EventTrack只处理其他的Event
            }
        }

        public override void DrawTrackGUI()
        {
            this.End = TargetTransition.FrameLength;
            base.DrawTrackGUI();
        }

        /// <summary>
        /// 编辑的目标 Transition
        /// </summary>
        protected IAssetHasEvents TargetTransition;

        protected override void AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("新建Event"), false, () =>
            {
                var signal = this.CreateSignalOnMouse<EventTrackSignal>();
                TargetTransition.Events.Add(signal.Event);
                var asset = (TargetTransition as AnimationAssetBase);
            });
        }


        [System.Serializable]
        internal class EventTrackSignal : TrackWindow.TrackSignal
        {
            public IAssetHasEvents _asset => ((EventTrack)AttachedTrack).TargetTransition;
            public override string Name { get => Event.Name; set => Event.Name = value; }
            public override float NormalizedTime { get => Event.NormalizedTime; set => Event.NormalizedTime = value; }
            public AssetEvent Event;

            public override void OnDelete()
            {
                serializedObject.Update();
                _asset.Events.Remove(Event);
                serializedObject.ApplyModifiedProperties();
            }

            SerializedObject serializedObject;
            SerializedProperty eventProperty;
            void OnEnable()
            {
                serializedObject = new SerializedObject(this);
                eventProperty = serializedObject.FindProperty("Event.UnityEvents");
            }

            public override void DoSelectedGUI()
            {
                EditorGUI.BeginChangeCheck();
                base.DoSelectedGUI();

                // 编辑并显示 Event.Event 属性

                GUILayout.BeginHorizontal();
                //EditorGUILayout.LabelField("时间点", GUILayout.Width(60));
                serializedObject.Update();
                EditorGUILayout.PropertyField(eventProperty, new GUIContent("事件"), true);
                GUILayout.EndHorizontal();

                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();
                }
            }
        }
    }

}
