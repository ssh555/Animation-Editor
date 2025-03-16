using System;
using UnityEngine;
using UnityEditor;
using System.Reflection;

namespace ML.Editor
{
    public static class EditorEditorWindow
    {
        private static Type _editorWindowType;

        public static Type EdtiorWindowType
        {
            get
            {
                if (_editorWindowType == null) _editorWindowType = typeof(EditorWindow);
                return _editorWindowType;
            }
        }

        /// <summary>
        /// ∆•≈‰∏∏ŒÔÃÂ≥ﬂ¥Á
        /// </summary>
        /// <param name="instance"></param>
        public static void MakeParentsSettingsMatchMe(object instance)
        {
            MethodInfo mInfo = EdtiorWindowType.GetMethod("MakeParentsSettingsMatchMe", BindingFlags.Instance | BindingFlags.NonPublic);
            if (mInfo == null) return;
            mInfo.Invoke(instance, null);
        }
    }

}
