using System.Collections.Generic;
using SeweralIdeas.Editor;
using SeweralIdeas.Pooling;
using UnityEditor;
using UnityEngine;

namespace SeweralIdeas.Localization.Editor
{
    public class LocalizationEditorWindow : EditorWindow
    {
        private readonly HashSet<string> m_allKeys = new();
        private readonly List<string> m_displayKeys = new();
        private static readonly GUILayoutOption[] s_expandWidth = { GUILayout.ExpandWidth(true) };
        private static readonly GUILayoutOption[] s_expandWidthHeight = { GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true) };
        private static GUIStyle m_deleteKeyStyle;
        
        [SerializeField] private List<float> m_columnWidths = new();
        [SerializeField] private Vector2 m_tableScrollPos;

        protected void OnGUI()
        {
            if(m_deleteKeyStyle == null)
            {
                m_deleteKeyStyle = "OL Minus";
            }

            //using var vertScope = new EditorGUILayout.VerticalScope();
            var manager = LocalizationManager.GetInstance();
            EditorGUILayout.ObjectField("Manager", manager, typeof(LocalizationManager), false);

            if(manager == null)
                return;

            using (new GUILayout.HorizontalScope())
            {
                if(GUILayout.Button("Detect Languages"))
                {
                    manager.DetectLanguages();
                }
                
                if(GUILayout.Button("Reload"))
                {
                    LocalizationEditor.DiscardAllChanges();
                    manager.DetectLanguages();
                }
            }


            UpdateAllKeys(manager);
            UpdateDisplayedKeys();

            using var _ = ListPool<(string, LanguageData)>.Get(out var languages);

            foreach (var pair in manager.Headers)
            {
                if (LocalizationEditor.TryGetLanguage(pair.Key, out var language))
                    languages.Add((pair.Key, language));
            }

            void ValueDrawer(Rect position, int rowId, int columnId)
            {
                (string languageName, LanguageData languageData) = languages[columnId];
                string key = m_displayKeys[rowId];

                if(!languageData.Texts.TryGetValue(key, out string oldValue))
                    oldValue = null;

                string newValue = EditorGUI.DelayedTextField(position, oldValue);
                if(newValue != oldValue)
                {
                    LocalizationEditor.SetLanguageText(languageName, key, newValue);
                }
            }

            void ColumnDrawer(Rect position, int column)
            {
                GUI.Button(position, languages[column].Item2.Header.DisplayName);
            }

            void RowDrawer(Rect position, int row)
            {
                Rect deletePos = new(position.x, position.y, 24, position.height);
                Rect labelPos = new(deletePos.xMax, position.y, position.width - deletePos.width, position.height);
                if(GUI.Button(deletePos, GUIContent.none, m_deleteKeyStyle))
                {
                    DeleteKey(m_displayKeys[row]);
                }
                GUI.Label(labelPos, m_displayKeys[row]);
            }

            void CornerDrawer(Rect position)
            {
                GUI.Button(position, "");
            }

            var tableRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight, s_expandWidthHeight);
            EditorGUITable.TableGUI(tableRect, ref m_tableScrollPos, m_columnWidths, m_displayKeys.Count, languages.Count, CornerDrawer, ValueDrawer, ColumnDrawer, RowDrawer);
        }
        
        private void DeleteKey(string keyToDelete)
        {
            bool prompt = EditorUtility.DisplayDialog(
                "Confirm localized key deletion", 
                $"This will permanently delete key \"{keyToDelete}\" from all languages.\nDo you want to proceed?",
                "Delete",
                "Cancel"
                );
            if(!prompt)
                return;
            
            var manager = LocalizationManager.GetInstance();
            foreach (var pair in manager.Headers)
            {
                LocalizationEditor.DeleteLanguageText(pair.Key, keyToDelete);
            }
        }

        private bool FilterKey(string key)
        {
            return true;
        }
        
        
        private void UpdateDisplayedKeys()
        {
            m_displayKeys.Clear();
            foreach (var key in m_allKeys)
            {
                if(!FilterKey(key))
                    continue;
                
                m_displayKeys.Add(key);
            }
            
            m_displayKeys.Sort();
        }
        
        private void UpdateAllKeys(LocalizationManager manager)
        {
            m_allKeys.Clear();
            foreach (KeyValuePair<string, LanguageHeader> languagePair in manager.Headers)
            {
                if(!LocalizationEditor.TryGetLanguage(languagePair.Key, out LanguageData languageData))
                    continue;

                foreach (var keyTextPair in languageData.Texts)
                {
                    m_allKeys.Add(keyTextPair.Key);
                }
            }
        }

        [MenuItem("Window/Localization Manager")]
        public static void Init()
        {
            var window = CreateWindow<LocalizationEditorWindow>("Localization Manager");
            window.Show();
        }
    }
}
