using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using SeweralIdeas.Collections;
using SeweralIdeas.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;

namespace SeweralIdeas.Localization.Editor
{
    public class LocalizationEditorWindow : EditorWindow
    {
        private readonly        HashSet<string>   m_allKeys         = new();
        private readonly        List<string>      m_displayKeys     = new();
        private static readonly GUILayoutOption[] ExpandWidth       = { GUILayout.ExpandWidth(true) };
        private static readonly GUILayoutOption[] ExpandWidthHeight = { GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true) };

        private static GUIStyle s_deleteKeyStyle;
        private static GUIStyle s_searchFieldStyle;
        private static GUIStyle s_whiteBgStyle;
        private static GUIStyle s_rowButtonStyle;

        
        private static readonly Color SelectedColor = new Color(1,1,1,1);
        private static readonly Color HoveredColor  = new Color(1,1,1,0.5f);
        
        private LanguageData[] m_loadedLanguages = Array.Empty<LanguageData>();

        [SerializeField] private LocalizationManager   m_manager;
        [SerializeField] private string                m_searchText;
        [SerializeField] private Vector2               m_tableScrollPos;
        [SerializeField] private List<float>           m_columnWidths = new();
        [SerializeField] private string                m_selectedKey;
        private                  string                m_editedKey;
        [NonSerialized]  private LocalizationManager   m_registeredManager;
        private readonly         HashSet<LanguageData> m_dirtyLanguages = new();
        private                  ValueTask             m_savingTask;

        private LocalizationManager RegisteredManager
        {
            get => m_registeredManager;
            set
            {
                if(m_registeredManager == value)
                    return;

                if(m_registeredManager != null)
                    m_registeredManager.Headers.Changed -= OnManagerHeadersChanged;

                m_registeredManager = value;

                if(m_registeredManager != null)
                    m_registeredManager.Headers.Changed += OnManagerHeadersChanged;
                else
                    OnManagerHeadersChanged(default);
            }
        }

        private async void OnManagerHeadersChanged(ReadonlyDictView<string, LanguageHeader> dict)
        {
            m_dirtyLanguages.Clear();
            m_loadedLanguages = await LoadAllLanguages(dict);
            UpdateAllKeys();
        }

        private async Task<LanguageData[]> LoadAllLanguages(ReadonlyDictView<string, LanguageHeader> dict)
        {
            var tasks = new Task<LanguageData>[dict.Count];
            var languages = new LanguageData[dict.Count];

            int i = 0;
            foreach (KeyValuePair<string, LanguageHeader> pair in dict)
            {
                tasks[i++] = LanguageData.LoadAsync(pair.Value);
            }

            for( int index = 0; index < tasks.Length; index++ )
            {
                Task<LanguageData> task = tasks[index];
                languages[index] = await task;
            }

            return languages;
        }

        protected void OnGUI()
        {
            wantsMouseMove = true;
            s_whiteBgStyle ??= "WhiteBackground";
            s_deleteKeyStyle ??= "OL Minus";
            s_searchFieldStyle ??= "ToolbarSearchTextField";

            if(s_rowButtonStyle == null)
            {
                s_rowButtonStyle = new GUIStyle("Button");
                s_rowButtonStyle.alignment = TextAnchor.MiddleLeft;
            }

            //using var vertScope = new EditorGUILayout.VerticalScope();
            m_manager = (LocalizationManager)EditorGUILayout.ObjectField("Manager", m_manager, typeof( LocalizationManager ), false);
            RegisteredManager = m_manager;

            m_searchText = GUILayout.TextField(m_searchText, s_searchFieldStyle);
            UpdateDisplayedKeys();

            void ValueDrawer(Rect position, int rowId, int columnId)
            {
                LanguageData languageData = m_loadedLanguages[columnId];
                string key = m_displayKeys[rowId];

                languageData.Texts.TryGetValue(key, out string oldValue);

                string newValue = EditorGUI.DelayedTextField(position, oldValue);
                if(newValue != oldValue)
                {
                    languageData.SetText(key,newValue);
                    m_dirtyLanguages.Add(languageData);
                }
            }

            void ColumnDrawer(Rect position, int column)
            {
                GUI.Button(position, m_loadedLanguages[column].Header.DisplayName);
            }

            void RowDrawer(Rect position, int row)
            {
                Rect deletePos = new(position.x, position.y, 24, position.height);
                Rect labelPos = new(deletePos.xMax, position.y, position.width - deletePos.width, position.height);
                string key = m_displayKeys[row];
                
                if(GUI.Button(deletePos, GUIContent.none, s_deleteKeyStyle))
                    DeleteKey(key);

                bool isSelected = m_selectedKey == key;
                GUI.backgroundColor = isSelected ? SelectedColor : HoveredColor;

                if(key == m_editedKey)
                {
                    var newKey = EditorGUI.DelayedTextField(labelPos, m_editedKey);
                    if(newKey != m_editedKey)
                    {
                        RenameKey(m_editedKey, newKey);
                    }
                }
                else if (GUI.Button(labelPos, key, s_rowButtonStyle))
                {
                    if(m_selectedKey == key)
                        m_editedKey = key;
                    else
                        m_editedKey = null;
                    m_selectedKey = key;
                }
                
                GUI.backgroundColor = Color.white;
                
                // GUI.Label(labelPos, key);
            }

            static void CornerDrawer(Rect pos) => GUI.Button(pos, "");

            var tableRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight, ExpandWidthHeight);
            EditorGUITable.TableGUI(tableRect, ref m_tableScrollPos, m_columnWidths, m_displayKeys.Count, m_loadedLanguages.Length, CornerDrawer, ValueDrawer, ColumnDrawer, RowDrawer);

            if(m_savingTask.IsCompleted)
                m_savingTask = SaveAllDirtyLanguagesAsync();
        }
        
        private async ValueTask SaveAllDirtyLanguagesAsync()
        {
            if(m_dirtyLanguages.Count == 0)
                return;
            
            using (ListPool<Task>.Get(out var tasks))
            using (ListPool<LanguageData>.Get(out var languages))
            {
                foreach(var dirtyLang in m_dirtyLanguages)
                    languages.Add(dirtyLang);
                m_dirtyLanguages.Clear();
                
                foreach (var dirtyLang in languages)
                    tasks.Add(dirtyLang.Save());

                foreach (var task in tasks)
                    await task;
            }
        }

        private void UpdateAllKeys()
        {
            m_allKeys.Clear();
            foreach (var language in m_loadedLanguages)
            {
                foreach (var keyTextPair in language.Texts)
                    m_allKeys.Add(keyTextPair.Key);
            }
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

        private bool FilterKey(string key)
        {
            static bool Filter(string key, string search)
            {
                int i = CultureInfo.InvariantCulture.CompareInfo.IndexOf(key, search, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace);
                return i > (-1);
            }

            // skip if search text is empty
            if(string.IsNullOrEmpty(m_searchText))
                return true;

            // check key
            if(Filter(key, m_searchText))
                return true;

            // check values
            foreach (var language in m_loadedLanguages)
            {
                if(!language.Texts.TryGetValue(key, out string value))
                    continue;

                if(Filter(value, m_searchText))
                    return true;
            }
            return false;
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

            foreach (var languageData in m_loadedLanguages)
            {
                if (languageData.RemoveText(keyToDelete, out _))
                    m_dirtyLanguages.Add(languageData);
                
                m_allKeys.Remove(keyToDelete);
                // throw new NotImplementedException();
                // LocalizationEditor.DeleteLanguageText(pair.Key, keyToDelete);
            }
        }
        
        private void RenameKey(string editedKey, string newKey)
        {
            foreach (var language in m_loadedLanguages)
            {
                if(language.Texts.ContainsKey(newKey))
                {
                    Debug.LogError("Failed to rename: New key already present.");
                    return;
                }
            }

            foreach (var language in m_loadedLanguages)
            {
                if(language.RemoveText(editedKey, out var removedValue))
                {
                    language.SetText(newKey, removedValue);
                    m_dirtyLanguages.Add(language);
                }
            }
            m_allKeys.Remove(editedKey);
            m_allKeys.Add(newKey);
        }


        [MenuItem("Window/Localization Manager")]
        public static void Init()
        {
            var window = CreateWindow<LocalizationEditorWindow>("Localization Manager");
            window.Show();
        }
    }
}