using SeweralIdeas.Localization.Editor;
using UnityEditor;
using UnityEngine;
namespace SeweralIdeas.Localization.Editor
{
    [CustomEditor(typeof(LocalizationManager))]
    public class LocalizationManagerEditor : UnityEditor.Editor
    {
        private GUIStyle m_buttonStyle;
        
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            
            if(m_buttonStyle == null)
            {
                m_buttonStyle = new GUIStyle(EditorStyles.miniButton);
                m_buttonStyle.alignment = TextAnchor.MiddleLeft;
                m_buttonStyle.fixedHeight = 36;
                m_buttonStyle.padding = new RectOffset(2, 2, 2, 2);
            }
            
            
            var manager = (LocalizationManager)target;
            if(GUILayout.Button("Detect Languages"))
            {
                manager.DetectLanguages();
            }

            GUILayout.Space(EditorGUIUtility.singleLineHeight);
            
            string loadedLangName = manager.LoadedLanguageName;
            foreach (var pair in manager.Headers)
            {
                string langName = pair.Key;
                LanguageHeader header = pair.Value;
                bool isOn = langName == loadedLangName;
                bool toggle = GUILayout.Toggle(isOn, new GUIContent(header.Name, header.Texture), m_buttonStyle);

                if(toggle && !isOn)
                {
                    // clicked
                    manager.SetLanguage(langName);
                    RepaintInspector();
                }
            }
        }
        
        public static void RepaintInspector()
        {
            var ed = Resources.FindObjectsOfTypeAll<UnityEditor.Editor>();
            for (int i = 0; i < ed.Length; i++)
            {
                ed[i].Repaint();
            }
        }
    }
}
