using Animancer.Editor;
using ML.Engine.Animation;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;

namespace ML.Editor.Animation
{
    [UnityEditor.CustomEditor(typeof(IntegerMixerTransitionAsset), true)]
    public class IntegerMixerTransitionAssetEditor : AnimationAssetBaseEditor
    {
        protected SerializedProperty _transitionsProperty { get; set; }

        protected IntegerMixerTransitionAsset asset { get; set; }

        public override void Init()
        {
            asset = (IntegerMixerTransitionAsset)target;

            _transitionsProperty = serializedObject.FindProperty("transitions");

        }

        public override void DrawInEditorWindow(EditorWindow window)
        {
            serializedObject.Update();

            // 默认值
            asset.DefaultValue = EditorGUILayout.IntField("默认值", asset.DefaultValue);
            asset.ClampDefaultValue();

            // 随机值
            asset.IsRandom = EditorGUILayout.Toggle("是否随机", asset.IsRandom);

            // Animations
            EditorGUILayout.PropertyField(_transitionsProperty, new GUIContent("动画"));

            serializedObject.ApplyModifiedProperties();
        }
     }

}
