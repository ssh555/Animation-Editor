using Animancer;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using ML.Editor;
using ML.Engine.Animation;
using UnityEditor.SceneManagement;
using UnityEngine.Analytics;
using UnityEngine.SceneManagement;
using Animancer.Editor;
using System;

namespace ML.Editor.Animation
{
    public class AnimationWindow : EditorWindow, IHasCustomMenu
    {
        private static AnimationWindow _instance;
        public static AnimationWindow Instance => _instance;

        private static object _container;

        public static Event GetCurrentEvent;

        public PreviewWindow previewWindow => PreviewWindow.Instance;
        public TrackWindow trackWindow => TrackWindow.Instance;
        public DetailWindow detailWindow => DetailWindow.Instance;


        private AnimationAssetBase selectedAsset;
        public event Action<AnimationAssetBase> OnSelectedChanged;
        public AnimationAssetBase SelectedAsset
        {
            get => selectedAsset;
            private set
            {
                if (!locked && selectedAsset != value)
                {
                    selectedAsset = value;
                    OnSelectedChanged?.Invoke(selectedAsset);
                }
            }
        }
        private AnimationAssetBaseEditor assetEditor;
        public AnimationAssetBaseEditor AssetEditor => assetEditor;

        [MenuItem("Window/ML/AnimationWindow", priority = 0)]
        public static void ShowWindow()
        {
            if (_instance != null)
            {
                return;
            }


            // 创建最外层容器
            _container = ML.Editor.EditorContainerWindow.CreateInstance();
            // 创建分屏容器
            object splitViewInstance = EditorSplitView.CreateInstance();
            // 设置根容器
            EditorContainerWindow.SetRootView(_container, splitViewInstance);

            // 创建window容器
            object windowDockAreaInstance = EditorDockArea.CreateInstance();
            EditorDockArea.SetPosition(windowDockAreaInstance, new Rect(0, 0, 200, 800));
            _instance = (AnimationWindow)ScriptableObject.CreateInstance(typeof(AnimationWindow));
            EditorWindow window = _instance;
            window.titleContent = new GUIContent("动画资产");
            EditorDockArea.AddTab(windowDockAreaInstance, window);
            // 加入Window容器
            EditorSplitView.AddChild(splitViewInstance, windowDockAreaInstance);

            object previewtrackSplitViewInstance = EditorSplitView.CreateInstance();
            EditorSplitView.SetPosition(previewtrackSplitViewInstance, new Rect(200, 0, 800, 800));
            EditorSplitView.SetVertical(previewtrackSplitViewInstance, true);
            EditorSplitView.AddChild(splitViewInstance, previewtrackSplitViewInstance);
            // 创建Preview
            object previewDockAreaInstance = EditorDockArea.CreateInstance();
            EditorDockArea.SetPosition(previewDockAreaInstance, new Rect(200, 0, 800, 600));
            ScriptableObject.CreateInstance(typeof(PreviewWindow));
            _instance.previewWindow.titleContent = new GUIContent("预览");
            EditorDockArea.AddTab(previewDockAreaInstance, _instance.previewWindow);
            // 加入Window容器
            EditorSplitView.AddChild(previewtrackSplitViewInstance, previewDockAreaInstance);

            // 创建Track
            object trackDockAreaInstance = EditorDockArea.CreateInstance();
            EditorDockArea.SetPosition(trackDockAreaInstance, new Rect(200, 600, 800, 200));
            ScriptableObject.CreateInstance(typeof(TrackWindow));
            _instance.trackWindow.titleContent = new GUIContent("轨道");
            EditorDockArea.AddTab(trackDockAreaInstance, _instance.trackWindow);
            // 加入Window容器
            EditorSplitView.AddChild(previewtrackSplitViewInstance, trackDockAreaInstance);
            //EditorSplitView.SetVertical(splitViewInstance, false);


            // 创建Detail
            object detailDockAreaInstance = EditorDockArea.CreateInstance();
            EditorDockArea.SetPosition(detailDockAreaInstance, new Rect(1000, 600, 200, 800));
            ScriptableObject.CreateInstance(typeof(DetailWindow));
            _instance.detailWindow.titleContent = new GUIContent("细节");
            EditorDockArea.AddTab(detailDockAreaInstance, _instance.detailWindow);
            // 加入Window容器
            EditorSplitView.AddChild(splitViewInstance, detailDockAreaInstance);


            EditorEditorWindow.MakeParentsSettingsMatchMe(window);

            EditorContainerWindow.SetPosition(_container, new Rect(100, 100, 1200, 800));
            EditorSplitView.SetPosition(splitViewInstance, new Rect(0, 0, 1200, 800));


            EditorContainerWindow.Show(_container, 0, true, false, true);
            EditorContainerWindow.OnResize(_container);

            return;
        }

        public static AnimationWindow OpenWithAsset(AnimationAssetBase asset)
        {
            ShowWindow();
            if (locked == false)
            {
                _instance.SelectedAsset = asset;
            }
            return _instance;
        }

        private void OnGUI()
        {
            GetCurrentEvent = Event.current;
            // 选中的资产
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("选中的资产", GUILayout.Width(100));
            SelectedAsset = (AnimationAssetBase)EditorGUILayout.ObjectField(SelectedAsset, typeof(AnimationAssetBase), false);
            EditorGUILayout.EndHorizontal();

            // 显示选中的资产的数据
            if (SelectedAsset != null && (assetEditor == null || assetEditor.target != SelectedAsset))
            {
                assetEditor = UnityEditor.Editor.CreateEditor(SelectedAsset) as AnimationAssetBaseEditor;
                if (assetEditor != null)
                {
                    assetEditor.Init();
                }
            }
            if (assetEditor != null)
            {
                assetEditor.DrawInEditorWindow(this);
            }
            else if (SelectedAsset)
            {
                Debug.LogWarning($"{SelectedAsset.GetType()} 没有对应的Editor");
            }


        }

        public void OnSelectionChange()
        {
            var tmp = Selection.activeObject as AnimationAssetBase;
            if (tmp != null)
            {
                SelectedAsset = tmp;
                Repaint();
            }
        }

        private void OnEnable()
        {
            if(_instance == null)
            {
                _instance = this;
            }
            AssemblyReloadEvents.afterAssemblyReload += AssemblyReloadEvents_afterAssemblyReload;
            EditorApplication.update += SaveData;
        }

        private void AssemblyReloadEvents_afterAssemblyReload()
        {
            this.Close();
            this.previewWindow.Close();
            this.detailWindow.Close();
            this.trackWindow.Close();
            _instance = null;
            OpenWithAsset(SelectedAsset);
        }

        private void OnDisable()
        {
            AssemblyReloadEvents.afterAssemblyReload -= AssemblyReloadEvents_afterAssemblyReload;
            if (_instance == this)
            {
                //_instance.Close();

                EditorContainerWindow.Close(_container);
                _instance = null;
            }
            EditorApplication.update -= SaveData;
        }

        public void SaveData()
        {
            Event e = GetCurrentEvent;
            if (SelectedAsset != null && e != null && e.keyCode == KeyCode.S && e.control)
            {
                Debug.Log("Animation Window : Save Selected Animation Asset");
                e.Use();
                EditorUtility.SetDirty(SelectedAsset);
                AssetDatabase.SaveAssetIfDirty(SelectedAsset);
            }
        }

        #region ContextMenu
        /// <summary>
        /// Keep local copy of lock button style for efficiency.
        /// </summary>
        [System.NonSerialized]
        GUIStyle lockButtonStyle;
        /// <summary>
        /// Indicates whether lock is toggled on/off.
        /// </summary>
        [System.NonSerialized]
        private static bool locked = false;

        /// <summary>
        /// Magic method which Unity detects automatically.
        /// </summary>
        /// <param name="position">Position of button.</param>
        void ShowButton(Rect position)
        {
            if (lockButtonStyle == null)
                lockButtonStyle = "IN LockButton";
            // 如果没有选中资产，则禁用按钮
            EditorGUI.BeginDisabledGroup(SelectedAsset == null);
            if (SelectedAsset == null)
            {
                locked = false;
            }
            locked = GUI.Toggle(position, locked, GUIContent.none, lockButtonStyle);
            EditorGUI.EndDisabledGroup();
        }

        /// <summary>
        /// Adds custom items to editor window context menu.
        /// </summary>
        /// <remarks>
        /// <para>This will only work for Unity 4.x+</para>
        /// </remarks>
        /// <param name="menu">Context menu.</param>
        void IHasCustomMenu.AddItemsToMenu(GenericMenu menu)
        {
            // 如果没有选中资产，则禁用菜单项
            bool disabled = SelectedAsset == null;
            if (disabled)
            {
                menu.AddDisabledItem(new GUIContent("Lock"));
                locked = false;
            }
            else
            {
                menu.AddItem(new GUIContent("Lock"), locked, () =>
                {
                    locked = !locked;
                });
            }
        }
        #endregion
    }
}


public class DragSourceWindow : EditorWindow
{
    private string objectName = "Drag Me!";
    private Rect draggableRect = new Rect(10, 10, 100, 20);

    [MenuItem("Window/Drag Source")]
    public static void ShowWindow()
    {
        GetWindow<DragSourceWindow>("Drag Source").name = "Drag Source";
    }

    private void OnGUI()
    {
        EditorGUI.DrawRect(draggableRect, Color.gray);
        GUI.Label(draggableRect, objectName);

        if (Event.current.type == EventType.MouseDown && draggableRect.Contains(Event.current.mousePosition))
        {
            DragAndDrop.PrepareStartDrag();
            DragAndDrop.objectReferences = new UnityEngine.Object[] { this };
            DragAndDrop.StartDrag(objectName);
            Event.current.Use();
        }
    }
}


public class DragTargetWindow : EditorWindow
{
    private Rect dropArea = new Rect(10, 10, 200, 100);

    [MenuItem("Window/Drag Target")]
    public static void ShowWindow()
    {
        GetWindow<DragTargetWindow>("Drag Target").name = "Drag Target";
    }

    private void OnGUI()
    {
        EditorGUI.DrawRect(dropArea, Color.cyan);
        GUI.Label(dropArea, "Drop Here!");

        Event currentEvent = Event.current;
        if (currentEvent.type == EventType.DragUpdated || currentEvent.type == EventType.DragPerform)
        {
            if (dropArea.Contains(currentEvent.mousePosition))
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                if (currentEvent.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();

                    foreach (UnityEngine.Object draggedObject in DragAndDrop.objectReferences)
                    {
                        Debug.Log("Dropped: " + draggedObject.name);
                    }
                }

                Event.current.Use();
            }
        }
    }
}



