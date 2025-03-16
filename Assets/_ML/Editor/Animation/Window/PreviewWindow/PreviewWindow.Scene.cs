using Animancer;
using Animancer.Editor;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ML.Editor.Animation
{
    partial class PreviewWindow
    {
        public static Scene InstanceScene => Instance != null ? Instance._Scene : null;

        [Serializable]
        public class Scene
        {
            #region Fields and Properties
            private const HideFlags HideAndDontSave = HideFlags.HideInHierarchy | HideFlags.DontSave;

            /// <summary>
            /// Preview Scene
            /// </summary>
            [SerializeField]
            private UnityEngine.SceneManagement.Scene _Scene;

            /// <summary>
            /// 预览场景中的根物体
            /// </summary>
            public Transform PreviewSceneRoot { get; private set; }

            /// <summary>
            /// 预览场景中模型的根物体
            /// <see cref="PreviewSceneRoot">的子物体
            /// </summary>
            public Transform InstanceRoot { get; private set; }

            /// <summary>
            /// <see cref="Settings.SceneEnvironment"/>的实例物体
            /// <see cref="PreviewSceneRoot"/>的子物体
            /// </summary>
            public GameObject EnvironmentInstance { get; private set; }

            [SerializeField]
            private Transform _OriginalRoot;

            /// <summary>
            /// 用于实例化<see cref="InstanceRoot"/> 的原模型
            /// </summary>
            public Transform OriginalRoot
            {
                get => _OriginalRoot;
                set
                {
                    _OriginalRoot = value;
                    InstantiateModel();

                    if (value != null)
                    {
                        Settings.AddModel(value.gameObject);
                    }
                }
            }

            /// <summary>
            /// <see cref="InstanceRoot"/> 及其子物体所有的Animator
            /// </summary>
            public Animator[] InstanceAnimators { get; private set; }

            /// <summary>
            /// 当前选中的Animator的Index
            /// </summary>
            [SerializeField] private int _SelectedInstanceAnimator;
            /// <summary>
            /// 选中的模型动画的类型
            /// </summary>
            [NonSerialized] private AnimationType _SelectedInstanceType;

            /// <summary>
            /// 当前选中的Animator
            /// </summary>
            public Animator SelectedInstanceAnimator
            {
                get
                {
                    if (InstanceAnimators == null ||
                        InstanceAnimators.Length == 0)
                        return null;

                    if (_SelectedInstanceAnimator > InstanceAnimators.Length)
                        _SelectedInstanceAnimator = InstanceAnimators.Length;

                    return InstanceAnimators[_SelectedInstanceAnimator];
                }
            }

            [NonSerialized]
            private AnimancerPlayable _Animancer;
            /// <summary>
            /// 用于预览动画
            /// </summary>
            public AnimancerPlayable Animancer
            {
                get
                {
                    if ((_Animancer == null || !_Animancer.IsValid) &&
                        InstanceRoot != null)
                    {
                        // 当前选中的Animator
                        var animator = SelectedInstanceAnimator;
                        if (animator != null)
                        {
                            AnimancerPlayable.SetNextGraphName($"{animator.name} (Animancer Preview)");
                            _Animancer = AnimancerPlayable.Create();
                            _Animancer.CreateOutput(
                                new AnimancerEditorUtilities.DummyAnimancerComponent(animator, _Animancer));
                            _Animancer.RequirePostUpdate(Animations.WindowMatchStateTime.Instance);
                            Instance._Animations.NormalizedTime = Instance._Animations.NormalizedTime;
                        }
                    }

                    return _Animancer;
                }
            }

            #endregion

            #region Initialization
            /// <summary>
            /// 初始化场景 -> 手动调用
            /// </summary>
            public void OnEnable()
            {
                // 新场景打开时调用
                EditorSceneManager.sceneOpening += OnSceneOpening;
                // 播放模式修改时调用
                EditorApplication.playModeStateChanged += OnPlayModeChanged;

                // 绘制场景GUI时调用
                //duringSceneGui += DoCustomGUI;

                // 创建场景
                CreateScene();

                // 选择最佳的预览模型
                if (OriginalRoot == null)
                    OriginalRoot = Settings.TrySelectBestModel();
            }

            /// <summary>
            /// 创建预览场景
            /// </summary>
            private void CreateScene()
            {
                _Scene = EditorSceneManager.NewPreviewScene();
                _Scene.name = "Transition Preview";
                Instance.customScene = _Scene;

                // 场景预览根物体
                PreviewSceneRoot = EditorUtility.CreateGameObjectWithHideFlags(
                    $"Preview Scene Root", HideAndDontSave).transform;
                // 将 PreviewSceneRoot 移动至预览场景中
                SceneManager.MoveGameObjectToScene(PreviewSceneRoot.gameObject, _Scene);
                Instance.customParentForDraggedObjects = PreviewSceneRoot;

                OnEnvironmentPrefabChanged();
            }

            /// <summary>
            /// 实例化新的模型
            /// </summary>
            private void InstantiateModel()
            {
                // 销毁旧模型
                DestroyModelInstance();

                if (_OriginalRoot == null)
                {
                    return;
                }

                // 实例化新的模型为InstanceRoot
                PreviewSceneRoot.gameObject.SetActive(false);
                InstanceRoot = Instantiate(_OriginalRoot, PreviewSceneRoot);
                InstanceRoot.localPosition = default;
                InstanceRoot.name = _OriginalRoot.name;

                // 禁用预览实例模型上的不必要组件
                DisableUnnecessaryComponents(InstanceRoot.gameObject);

                // 实例化模型上的所有Animators
                InstanceAnimators = InstanceRoot.GetComponentsInChildren<Animator>();
                for (int i = 0; i < InstanceAnimators.Length; i++)
                {
                    var animator = InstanceAnimators[i];
                    // 禁用
                    animator.enabled = false;
                    // 总是播放
                    animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    // 不触发时间
                    animator.fireEvents = false;
                    // 正常播放模式
                    animator.updateMode = AnimatorUpdateMode.Normal;
                }

                // Active 预览动画的根物体
                PreviewSceneRoot.gameObject.SetActive(true);

                // 设置选择的Animator
                SetSelectedAnimator(_SelectedInstanceAnimator);
                // 聚焦摄像机于当前模型实例
                FocusCamera();
                // 收集当前预览模型上的动画
                Instance._Animations.GatherAnimations();
            }

            private static void DisableUnnecessaryComponents(GameObject root)
            {
                var behaviours = root.GetComponentsInChildren<Behaviour>();
                for (int i = 0; i < behaviours.Length; i++)
                {
                    var behaviour = behaviours[i];

                    // Other undesirable components aren't Behaviors anyway: Transform, MeshFilter, Renderer
                    if (behaviour is Animator)
                        continue;

                    var type = behaviour.GetType();
                    if (type.IsDefined(typeof(ExecuteAlways), true) ||
                        type.IsDefined(typeof(ExecuteInEditMode), true))
                        continue;

                    behaviour.enabled = false;
                    behaviour.hideFlags |= HideFlags.NotEditable;
                }
            }

            /// <summary>
            /// 销毁之前的环境实例，生成新的环境实例
            /// </summary>
            internal void OnEnvironmentPrefabChanged()
            {
                DestroyImmediate(EnvironmentInstance);

                var prefab = Settings.SceneEnvironment;
                if (prefab != null)
                    EnvironmentInstance = Instantiate(prefab, PreviewSceneRoot);
            }

            /// <summary>
            /// 在选择的资产改变时调用
            /// </summary>
            public void OnSelectedAssetChanged()
            {
                _SelectedInstanceAnimator = 0;
                if (_ExpandedHierarchy != null)
                    _ExpandedHierarchy.Clear();

                // 直接寻找最佳预览模型
                //OriginalRoot = AnimancerEditorUtilities.FindRoot(Instance._TransitionProperty.TargetObject);
                if (OriginalRoot == null)
                    OriginalRoot = Settings.TrySelectBestModel();

                // 重置规约化时间
                Instance._Animations.NormalizedTime = 0;

                // 根据动画类型设置PreviewScene窗口显示mode
                Instance.in2DMode = _SelectedInstanceType == AnimationType.Sprite;
            }

            /// <summary>
            /// 设置选中的Animator
            /// </summary>
            /// <param name="index"></param>
            public void SetSelectedAnimator(int index)
            {
                // 销毁之前的Animancer实例
                DestroyAnimancerInstance();

                // 取消之前选中的Animator
                var animator = SelectedInstanceAnimator;
                if (animator != null && animator.enabled)
                {
                    animator.Rebind();
                    animator.enabled = false;
                    // 为什么要return
                    //return;
                }

                // 选中当前选择的Animator
                _SelectedInstanceAnimator = index;

                animator = SelectedInstanceAnimator;
                if (animator != null)
                {
                    animator.enabled = true;
                    _SelectedInstanceType = AnimationBindings.GetAnimationType(animator);
                    Instance.in2DMode = _SelectedInstanceType == AnimationType.Sprite;
                }
            }

            /// <summary>
            /// 更改摄像机 -> 聚焦于当前的模型实例
            /// </summary>
            private void FocusCamera()
            {
                // 实例模型的RendererBound
                var bounds = CalculateBounds(InstanceRoot);

                var rotation = Instance.in2DMode ? Quaternion.identity : Quaternion.Euler(35, 135, 0);

                var size = bounds.extents.magnitude * 1.5f;
                if (size == float.PositiveInfinity)
                    return;
                else if (size == 0)
                    size = 10;

                Instance.LookAt(bounds.center, rotation, size, Instance.in2DMode, true);
            }

            /// <summary>
            /// 计算 Transform 的 RendererBound -> 所有的RendererBound.Grow
            /// </summary>
            /// <param name="transform"></param>
            /// <returns></returns>
            private static Bounds CalculateBounds(Transform transform)
            {
                var renderers = transform.GetComponentsInChildren<Renderer>();
                if (renderers.Length == 0)
                    return default;

                var bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }
                return bounds;
            }

            #endregion

            #region Cleanup
            /// <summary>
            /// PreviewWindow 手动调用
            /// </summary>
            public void OnDisable()
            {
                EditorSceneManager.sceneOpening -= OnSceneOpening;
                EditorApplication.playModeStateChanged -= OnPlayModeChanged;

                //duringSceneGui -= DoCustomGUI;

                DestroyAnimancerInstance();

                EditorSceneManager.ClosePreviewScene(_Scene);
            }

            /// <summary>
            /// PreviewWindow 手动调用
            /// </summary>
            public void OnDestroy()
            {
                if (PreviewSceneRoot != null)
                {
                    DestroyImmediate(PreviewSceneRoot.gameObject);
                    PreviewSceneRoot = null;
                }
            }


            public void DestroyModelInstance()
            {
                DestroyAnimancerInstance();

                if (InstanceRoot == null)
                    return;

                DestroyImmediate(InstanceRoot.gameObject);
                InstanceRoot = null;
                InstanceAnimators = null;
            }

            private void DestroyAnimancerInstance()
            {
                if (_Animancer == null)
                    return;

                _Animancer.CancelPostUpdate(Animations.WindowMatchStateTime.Instance);
                _Animancer.DestroyGraph();
                _Animancer = null;
            }
            #endregion

            #region Execution
            [SerializeField]
            private List<Transform> _ExpandedHierarchy;

            /// <summary>
            /// A list of all objects with their child hierarchy expanded.
            /// </summary>
            public List<Transform> ExpandedHierarchy
            {
                get
                {
                    if (_ExpandedHierarchy == null)
                        _ExpandedHierarchy = new List<Transform>();
                    return _ExpandedHierarchy;
                }
            }

            /// <summary>
            /// GUI绘制 -> 手动调用
            /// </summary>
            public void OnGUI()
            {
                // 实例化预览模型
                if (!AnimancerEditorUtilities.IsChangingPlayMode && InstanceRoot == null)
                    InstantiateModel();

                if (_Animancer != null && _Animancer.IsGraphPlaying)
                    AnimancerGUI.RepaintEverything();

                // 按下 ALT + F 重新聚焦于预览模型
                if (Event.current.alt && Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.F)
                    FocusCamera();
            }

            /// <summary>
            /// 新场景打开时调用
            /// </summary>
            /// <param name="path">新场景的路径</param>
            /// <param name="mode">打开方式</param>
            private void OnSceneOpening(string path, OpenSceneMode mode)
            {
                if (mode == OpenSceneMode.Single)
                    DestroyModelInstance();
            }

            /// <summary>
            /// 在运行模式改变时调用
            /// </summary>
            /// <param name="change"></param>
            private void OnPlayModeChanged(PlayModeStateChange change)
            {
                switch (change)
                {
                    // 退出编辑模式 | 退出运行模式
                    case PlayModeStateChange.ExitingEditMode:
                    case PlayModeStateChange.ExitingPlayMode:
                        DestroyModelInstance();
                        break;
                }
            }

            ///// <summary>
            ///// 在SceneGUI上绘制 Transition 的自定义GUI
            ///// </summary>
            ///// <param name="sceneView"></param>
            //private void DoCustomGUI(SceneView sceneView)
            //{
            //    var animancer = Animancer;
            //    if (animancer != null &&
            //        sceneView is TransitionPreviewWindow instance &&
            //        AnimancerUtilities.TryGetWrappedObject(Transition, out ITransitionGUI gui) &&
            //        instance._TransitionProperty != null)
            //    {
            //        EditorGUI.BeginChangeCheck();

            //        using (TransitionDrawer.DrawerContext.Get(instance._TransitionProperty))
            //        {
            //            try
            //            {
            //                gui.OnPreviewSceneGUI(new TransitionPreviewDetails(animancer));
            //            }
            //            catch (Exception exception)
            //            {
            //                Debug.LogException(exception);
            //            }
            //        }

            //        if (EditorGUI.EndChangeCheck())
            //            AnimancerGUI.RepaintEverything();
            //    }
            //}

            /// <summary>
            /// 是否是预览场景中的PreviewSceneRoot下的物体
            /// </summary>
            /// <param name="obj"></param>
            /// <returns></returns>
            public bool IsSceneObject(UnityEngine.Object obj)
            {
                return obj is GameObject gameObject && gameObject.transform.IsChildOf(PreviewSceneRoot);
            }
            #endregion
        }
    }
}
