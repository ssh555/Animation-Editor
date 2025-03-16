using Animancer;
using Animancer.Editor;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ML.Editor.Animation
{
    public class AnimationSettings : ScriptableObject
    {
        private static AnimationSettings _Instance;
        /// <summary>
        /// 载入已存在的
        /// 若没有，则新建一个保存
        /// </summary>
        public static AnimationSettings Instance
        {
            get
            {
                if (_Instance != null)
                    return _Instance;

                // 找已存在的
                _Instance = AnimancerEditorUtilities.FindAssetOfType<AnimationSettings>();

                if (_Instance != null)
                    return _Instance;

                // 新建
                _Instance = CreateInstance<AnimationSettings>();
                _Instance.name = "Animation Settings";
                _Instance.hideFlags = HideFlags.DontSaveInBuild;

                var script = MonoScript.FromScriptableObject(_Instance);
                var path = AssetDatabase.GetAssetPath(script);
                path = Path.Combine(Path.GetDirectoryName(path), $"{_Instance.name}.asset");
                AssetDatabase.CreateAsset(_Instance, path);

                return _Instance;
            }
        }


        private SerializedObject _SerializedObject;
        public static SerializedObject SerializedObject
            => Instance._SerializedObject ?? (Instance._SerializedObject = new SerializedObject(Instance));

        private readonly Dictionary<string, SerializedProperty> SerializedProperties = new Dictionary<string, SerializedProperty>();

        private static SerializedProperty GetSerializedProperty(string propertyPath)
        {
            var properties = Instance.SerializedProperties;
            if (!properties.TryGetValue(propertyPath, out var property))
            {
                property = SerializedObject.FindProperty(propertyPath);
                properties.Add(propertyPath, property);
            }

            return property;
        }

        [SerializeField]
        private PreviewWindow.Settings _Settings;
        internal static PreviewWindow.Settings PreviewWindow => Instance._Settings;


        protected virtual void OnEnable()
        {
            if (_Settings == null)
                _Settings = new PreviewWindow.Settings();
            _Settings.SetBasePropertyPath(nameof(_Settings));
        }

        public static new void SetDirty() => EditorUtility.SetDirty(_Instance);

        [CustomEditor(typeof(AnimationSettings), true), CanEditMultipleObjects]
        public class Editor : UnityEditor.Editor
        {
            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

                EditorGUILayout.BeginHorizontal();

                using (ObjectPool.Disposable.AcquireContent(out var label, "Disabled Warnings"))
                {
                    EditorGUI.BeginChangeCheck();
                    var value = EditorGUILayout.EnumFlagsField(label, Validate.PermanentlyDisabledWarnings);
                    if (EditorGUI.EndChangeCheck())
                        Validate.PermanentlyDisabledWarnings = (OptionalWarning)value;
                }

                if (GUILayout.Button("Help", EditorStyles.miniButton, AnimancerGUI.DontExpandWidth))
                    Application.OpenURL(Strings.DocsURLs.OptionalWarning);

                EditorGUILayout.EndHorizontal();
            }
        }

        public abstract class Group
        {
            private string _BasePropertyPath;

            internal void SetBasePropertyPath(string propertyPath)
            {
                _BasePropertyPath = propertyPath + ".";
            }

            protected SerializedProperty GetSerializedProperty(string propertyPath)
                => AnimationSettings.GetSerializedProperty(_BasePropertyPath + propertyPath);

            protected SerializedProperty DoPropertyField(string propertyPath)
            {
                var property = GetSerializedProperty(propertyPath);
                EditorGUILayout.PropertyField(property, true);
                return property;
            }
        }

    }

}
