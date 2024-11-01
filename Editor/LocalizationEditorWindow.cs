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
        
        private readonly List<EditorLanguageManager.Request> m_requests = new();
        private          bool                                m_allKeysDirty;

        [SerializeField] private string                m_searchText;
        [SerializeField] private Vector2               m_tableScrollPos;
        [SerializeField] private List<float>           m_columnWidths = new();
        [SerializeField] private string                m_selectedKey;
        
        private                  string                m_editedKey;
        private readonly         HashSet<LanguageData> m_dirtyLanguages = new();
        private                  ValueTask             m_savingTask;

        protected void OnEnable() => EditorLanguageManager.ActiveManager.Changed += OnActiveManagerChanged;
        
        protected void OnDisable()
        {
            ClearAllRequests();
            EditorLanguageManager.ActiveManager.Changed -= OnActiveManagerChanged;
        }

        private void OnActiveManagerChanged(LanguageManager languageManager, LanguageManager oldLanguageManager)
        {
            if(oldLanguageManager != null)
                oldLanguageManager.Headers.Changed -= OnLanguageSetChanged;
            
            if(languageManager != null)
                languageManager.Headers.Changed += OnLanguageSetChanged;
        }

        private void OnLanguageSetChanged(ReadonlyDictView<string, LanguageHeader> languageHeaders, ReadonlyDictView<string, LanguageHeader> oldHeaders)
        {
            ClearAllRequests();

            foreach (KeyValuePair<string, LanguageHeader> keyValuePair in languageHeaders)
            {
                var request = EditorLanguageManager.CreateRequest(keyValuePair.Key);
                m_requests.Add(request);
                request.Language.Changed += OnRequestLanguageChanged;
            }
        }
        
        private void ClearAllRequests()
        {
            foreach (var oldReq in m_requests)
            {
                oldReq.Language.Changed -= OnRequestLanguageChanged;
                oldReq.Dispose();
            }

            m_requests.Clear();
        }
        
        private void OnRequestLanguageChanged(LanguageData newLang, LanguageData oldLang)
        {
            if(oldLang != null)
                oldLang.Modified -= OnLangModified;
            if(newLang != null)
                newLang.Modified += OnLangModified;
            m_allKeysDirty = true;
            Repaint();
        }

        private void OnLangModified()
        {
            Repaint();
            m_allKeysDirty = true;
        }

        protected void OnGUI()
        {
            if(m_allKeysDirty)
            {
                m_allKeysDirty = false;
                UpdateAllKeys();
            }
            
            wantsMouseMove = true;
            s_whiteBgStyle ??= "WhiteBackground";
            s_deleteKeyStyle ??= "OL Minus";
            s_searchFieldStyle ??= "ToolbarSearchTextField";

            if(s_rowButtonStyle == null)
            {
                s_rowButtonStyle = new GUIStyle("Button");
                s_rowButtonStyle.alignment = TextAnchor.MiddleLeft;
            }
            
            m_searchText = GUILayout.TextField(m_searchText, s_searchFieldStyle);
            UpdateDisplayedKeys();

            void ValueDrawer(Rect position, int rowId, int columnId)
            {
                LanguageData languageData = m_requests[columnId].Language.Value;

                if(languageData == null)
                    return;
                
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
                var language = m_requests[column].Language.Value;
                if(language == null)
                    return;
                GUI.Button(position, language.Header.DisplayName);
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
            EditorGUITable.TableGUI(tableRect, ref m_tableScrollPos, m_columnWidths, m_displayKeys.Count, m_requests.Count, CornerDrawer, ValueDrawer, ColumnDrawer, RowDrawer);

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
            foreach (var request in m_requests)
            {
                if(request.Language.Value == null)
                    continue;
                
                foreach (var keyTextPair in request.Language.Value.Texts)
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
            foreach (var request in m_requests)
            {
                if(!request.Language.Value.Texts.TryGetValue(key, out string value))
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

            foreach (var request in m_requests)
            {
                var languageData = request.Language.Value;
                
                if (languageData.RemoveText(keyToDelete, out _))
                    m_dirtyLanguages.Add(languageData);
            }
        }
        
        private void RenameKey(string editedKey, string newKey)
        {
            foreach (var request in m_requests)
            {
                if(request.Language.Value.Texts.ContainsKey(newKey))
                {
                    Debug.LogError("Failed to rename: New key already present.");
                    return;
                }
            }

            foreach (var request in m_requests)
            {
                var languageData = request.Language.Value;
                if(languageData.RemoveText(editedKey, out var removedValue))
                {
                    languageData.SetText(newKey, removedValue);
                    m_dirtyLanguages.Add(languageData);
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