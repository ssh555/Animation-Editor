using Animancer;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Animancer.Editor;
using ML.Engine.Animation;

namespace ML.Editor.Animation
{
#if UNITY_2020_1_OR_NEWER
    [EditorWindowTitle]// Prevent the base SceneView from trying to use this type name to find the icon.
#endif
    public partial class PreviewWindow : SceneView
    {
        #region Instance
        public static PreviewWindow Instance
        {
            get;
            private set;
        }
        #endregion

        #region Public API
        /// <summary>
        /// 预览窗口使用的Icon
        /// </summary>
        private static Texture _Icon;

        /// <summary>
        /// 预览窗口使用的Icon
        /// </summary>
        public static Texture Icon
        {
            get
            {
                if (_Icon == null)
                {
                    // Possible icons: "UnityEditor.LookDevView", "SoftlockInline", "ViewToolOrbit", "ClothInspector.ViewValue".
                    var name = EditorGUIUtility.isProSkin ? "ViewToolOrbit On" : "ViewToolOrbit";

                    _Icon = AnimancerGUI.LoadIcon(name);
                    if (_Icon == null)
                        _Icon = EditorGUIUtility.whiteTexture;
                }

                return _Icon;
            }
        }

        public static float PreviewNormalizedTime
        {
            get => Instance._Animations.NormalizedTime;
            set
            {
                if (value.IsFinite())
                    Instance._Animations.NormalizedTime = value;
            }
        }

        /// <summary>
        /// 获取当前正在预览的动画的状态
        /// </summary>
        /// <returns></returns>
        public static AnimancerState GetCurrentState()
        {
            if (Instance._Scene.Animancer == null)
                return null;

            Instance._Scene.Animancer.States.TryGet(Transition, out var state);
            return state;
        }

        #endregion

        #region Messages
        /// <summary>
        /// 预览的动画
        /// </summary>
        [SerializeField] private Animations _Animations;
        public Animations GetAnimations => _Animations;
        /// <summary>
        ///  预览的场景
        /// </summary>
        [SerializeField] private Scene _Scene;
        public Scene GetScene => _Scene;

        public override void OnEnable()
        {
#if ! UNITY_2020_1_OR_NEWER
            // Unity 2019 logs an error message when opening this window.
            // This is because the base SceneView has a [EditorWindowTitle] attribute which looks for the icon by
            // name, but since it's internal before Unity 2020 we can't replace it to prevent it from doing so.
            // Error: Unable to load the icon: 'Animancer.Editor.TransitionPreviewWindow'.
            using (BlockAllLogs.Activate())
#endif
            {
                base.OnEnable();
            }
            Instance = this;
            AnimationWindow.Instance.OnSelectedChanged += OnSelectedAssetChange;

            name = "Transition Preview Window";
            titleContent = new GUIContent("Transition Preview", Icon);
            autoRepaintOnSceneChange = true;
            sceneViewState.showSkybox = Settings.ShowSkybox;
            sceneLighting = Settings.SceneLighting;
            drawGizmos = Settings.DrawGizmos;

            if (_Scene == null)
                _Scene = new Scene();
            if (_Animations == null)
                _Animations = new Animations();

            // 可能会有问题
            DestroySelectedAsset();

            // 初始化场景
            _Scene.OnEnable();

            AssemblyReloadEvents.beforeAssemblyReload += DeselectPreviewSceneObjects;
        }

        public override void OnDisable()
        {
            base.OnDisable();

            if (AnimationWindow.Instance)
                AnimationWindow.Instance.OnSelectedChanged -= OnSelectedAssetChange;
            _Scene.OnDisable();
            AssemblyReloadEvents.beforeAssemblyReload -= DeselectPreviewSceneObjects;

            if (Instance == this)
                Instance = null;
        }

        private new void OnDestroy()
        {
            base.OnDestroy();

            _Scene.OnDestroy();
            DestroySelectedAsset();

            _selectedAsset = null;

            AnimancerGUI.RepaintEverything();


        }

#if UNITY_2021_2_OR_NEWER
        protected override void OnSceneGUI()
#else
        protected override void OnGUI()
#endif
        {
#if UNITY_2021_2_OR_NEWER
            base.OnSceneGUI();
#else
            base.OnGUI();
#endif
            AnimationWindow.GetCurrentEvent = Event.current;

            _Scene.OnGUI();

            //Settings.ShowSkybox = sceneViewState.showSkybox;
            //Settings.SceneLighting = sceneLighting;
        }

        /// <summary>Returns false.</summary>
        /// <remarks>Returning true makes it draw the main scene instead of the custom scene in Unity 2020.</remarks>
        protected override bool SupportsStageHandling() => false;

        private void OnSelectedAssetChange(AnimationAssetBase selection)
        {
            SetSelectedAsset(selection);
        }

        /// <summary>
        /// 取消选中除 PreviewSceneRoot 之外的 Object
        /// </summary>
        private void DeselectPreviewSceneObjects()
        {
            using (ObjectPool.Disposable.AcquireList<UnityEngine.Object>(out var objects))
            {
                var selection = Selection.objects;
                for (int i = 0; i < selection.Length; i++)
                {
                    var obj = selection[i];
                    if (!_Scene.IsSceneObject(obj))
                        objects.Add(obj);
                }
                Selection.objects = objects.ToArray();
            }
        }
        #endregion

        #region SelectedTransition
        private AnimationAssetBase _selectedAsset;
        public AnimationAssetBase _SelectedAsset => _selectedAsset;
        public static AnimationAssetBase SelectedAsset => Instance._selectedAsset;
        public static ITransition Transition
        {
            get
            {
                if (SelectedAsset == null)
                    return null;
                return SelectedAsset.GetPreviewTransition();
            }
        }

        private void SetSelectedAsset(AnimationAssetBase asset)
        {
            DestroySelectedAsset();
            _selectedAsset = asset;
            //_selectedAsset = ML.Engine.Utility.DeepCopyUtility.DeepCopy<AnimationAssetBase>(asset);
            _Scene.OnSelectedAssetChanged();
        }

        private void DestroySelectedAsset()
        {
            if (_selectedAsset == null)
                return;

            _Scene.DestroyModelInstance();

            _selectedAsset = null;
        }
        #endregion

        #region Error Intercepts
#if !UNITY_2020_1_OR_NEWER
        /************************************************************************************************************************/

        /// <summary>Prevents log messages between <see cref="Activate"/> and <see cref="IDisposable.Dispose"/>.</summary>
        private class BlockAllLogs : IDisposable, ILogHandler
        {
            private static readonly BlockAllLogs Instance = new BlockAllLogs();

            private ILogHandler _PreviousHandler;

            public static IDisposable Activate()
            {
                AnimancerUtilities.Assert(Instance._PreviousHandler == null,
                    $"{nameof(BlockAllLogs)} can't be used recursively.");

                Instance._PreviousHandler = Debug.unityLogger.logHandler;
                Debug.unityLogger.logHandler = Instance;
                return Instance;
            }

            void IDisposable.Dispose()
            {
                Debug.unityLogger.logHandler = _PreviousHandler;
                _PreviousHandler = null;
            }

            void ILogHandler.LogFormat(LogType logType, Object context, string format, params object[] args) { }

            void ILogHandler.LogException(Exception exception, Object context) { }
        }

        /************************************************************************************************************************/
#endif
        #endregion
    }
}
