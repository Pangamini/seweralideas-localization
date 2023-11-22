using System;
using System.Collections.Generic;
using System.Globalization;
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
        private static GUIStyle s_searchFieldStyle;
        
        [SerializeField] private List<float> m_columnWidths = new();
        [SerializeField] private Vector2 m_tableScrollPos;
        [SerializeField] private string m_searchText;
        private readonly List<(string, LanguageData)> m_languages = new();

        protected void OnGUI()
        {
            if(m_deleteKeyStyle == null)
            {
                m_deleteKeyStyle = "OL Minus";
                s_searchFieldStyle = "ToolbarSearchTextField";
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

            // search bar
            m_searchText = GUILayout.TextField(m_searchText, s_searchFieldStyle);

            UpdateLanguates(manager);
            UpdateAllKeys(manager);
            UpdateDisplayedKeys();

            void ValueDrawer(Rect position, int rowId, int columnId)
            {
                (string languageName, LanguageData languageData) = m_languages[columnId];
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
                GUI.Button(position, m_languages[column].Item2.Header.DisplayName);
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
            EditorGUITable.TableGUI(tableRect, ref m_tableScrollPos, m_columnWidths, m_displayKeys.Count, m_languages.Count, CornerDrawer, ValueDrawer, ColumnDrawer, RowDrawer);
        }

        private void UpdateLanguates(LocalizationManager manager)
        {
            m_languages.Clear();
            foreach (var pair in manager.Headers)
            {
                if (LocalizationEditor.TryGetLanguage(pair.Key, out var language))
                    m_languages.Add((pair.Key, language));
            }
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
            static bool Filter(string key, string search)
            {
                int i = CultureInfo.InvariantCulture.CompareInfo.IndexOf(key, search, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace);
                return i > (-1);
            }

            // skip if search text is empty
            if (string.IsNullOrEmpty(m_searchText))
                return true;

            // check key
            if(Filter(key, m_searchText))
                return true;
            
            // check values
            foreach ((string, LanguageData) pair in m_languages)
            {
                if(!pair.Item2.Texts.TryGetValue(key, out string value))
                    continue;
                
                if(Filter(value, m_searchText))
                    return true;
            }
            return false;
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
