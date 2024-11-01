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
                manager.Reload();
            }

            GUILayout.Space(EditorGUIUtility.singleLineHeight);
            LanguageHeadersGui(manager);
        }
        
        
        private void LanguageHeadersGui(LocalizationManager manager)
        {
            // string loadedLangName = manager.LoadedLanguageName;

            var headers = manager.Headers.Value;
            
            foreach (var pair in headers)
            {
                string langName = pair.Key;
                LanguageHeader header = pair.Value;
                bool isOn = false;//langName == loadedLangName;
                bool toggle = GUILayout.Toggle(isOn, new GUIContent(header.DisplayName, header.Texture), m_buttonStyle);

                if(toggle && !isOn)
                {
                    // clicked
                    EditorLanguageLoader.LoadParams = new LanguageLoader.Params
                    {
                        Manager = manager,
                        LanguageName = langName
                    };
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
