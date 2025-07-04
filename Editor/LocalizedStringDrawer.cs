using System;
using System.Collections.Generic;
using SeweralIdeas.UnityUtils.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;

namespace SeweralIdeas.Localization.Editor
{
    [CustomPropertyDrawer(typeof(LocalizedString))]
    public class LocalizedStringDrawer : PropertyDrawer
    {
        private static GUIStyle s_addKeyStyle;
        private const int TextLines = 1;
        private const float KeyPopupWidth = 20;
        private const float KeyAddWidth = 20;

        public override bool CanCacheInspectorGUI(SerializedProperty property) => true;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * (TextLines + 1);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if(s_addKeyStyle == null)
            {
                s_addKeyStyle = "OL Plus";
            }

            var language = EditorLanguageManager.ActiveLanguage.Value;
            
            var keyProp = property.FindPropertyRelative("m_key");
            Rect keyRect = new Rect(position.x, position.y, position.width - KeyPopupWidth - KeyAddWidth, EditorGUIUtility.singleLineHeight);
            Rect popupRect = new Rect(keyRect.xMax, keyRect.y, KeyPopupWidth, keyRect.height);
            Rect addRect = new Rect(popupRect.xMax, popupRect.y, KeyAddWidth, keyRect.height);
            Rect valueRowRect = new Rect(position.x, keyRect.yMax, position.width, position.height - keyRect.height);

            EditorGUI.PropertyField(keyRect, keyProp, label);

            GUI.enabled = language != null;
            if(GUI.Button(popupRect, GUIContent.none, EditorStyles.popup) && language != null)
            {
                GUI.enabled = true;
                var texts = language.Texts;
                var controlId = GUIUtility.GetControlID(FocusType.Passive, popupRect);
                
                List<GUIContent> options = new();
                foreach (var pair in texts)
                {
                    options.Add(new GUIContent(pair.Key));
                }
                
                var scrRect = new Rect(GUIUtility.GUIToScreenPoint(popupRect.position), new Vector2(256, popupRect.height));

                Action<int> onKeySelected = (index) =>
                {
                    keyProp.stringValue = options[index].text;
                    keyProp.serializedObject.ApplyModifiedProperties();
                };
                
                AdvancedDropdownWindow.ShowWindow(controlId, scrRect, options, true, null, onKeySelected);
            }

            GUI.enabled = language != null && !language.Texts.ContainsKey(keyProp.stringValue);
            if (GUI.Button(addRect, GUIContent.none, s_addKeyStyle))
            {
                LanguageData loadedLanguage = EditorLanguageManager.ActiveLanguage.Value;
                
                loadedLanguage.SetText(keyProp.stringValue, string.Empty);
                loadedLanguage.Save();
            }
            GUI.enabled = true;
            

            // Show localized value field
            LocalizedValueGUI(valueRowRect, property, popupRect, keyProp);
        }
        
        private static void LocalizedValueGUI(Rect position, SerializedProperty property, Rect popupRect, SerializedProperty keyProp)
        {
            LanguageData loadedLanguage = EditorLanguageManager.ActiveLanguage.Value;

            const float imageWidth = 32;

            if(loadedLanguage == null)
            {
                GUI.Label(position, "no language loaded");
                return;
            }

            var iconRect = new Rect(position.x, position.y, imageWidth, position.height);
            var valueRect = new Rect(position.x + EditorGUIUtility.labelWidth, position.y, position.width - EditorGUIUtility.labelWidth, position.height);

            if(GUI.Button(iconRect, loadedLanguage.Header.Texture, EditorStyles.label))
            {
                var controlId = GUIUtility.GetControlID(FocusType.Keyboard, iconRect);
                var scrRect = new Rect(GUIUtility.GUIToScreenPoint(iconRect.position), new Vector2(128, popupRect.height));

                List<GUIContent> options = new();
                List<string> langNames = new();
                
                foreach (var pair in EditorLanguageManager.ActiveManager.Value.Headers.Value)
                {
                    langNames.Add(pair.Key);
                    options.Add(new GUIContent(pair.Value.DisplayName, pair.Value.Texture));
                }

                Action<int> onLangSelected = (index) => EditorLanguageManager.ActiveLanguageName = langNames[index];
                
                AdvancedDropdownWindow.ShowWindow(controlId, scrRect, options, true, null, onLangSelected);
            }
            
            //EditorGUI.DrawTextureTransparent(iconRect, localizationManager.LoadedLanguage.Header.Texture, ScaleMode.ScaleToFit);

            using (ListPool<LocalizedString>.Get(out var localizedStrings))
            {
                EditorReflectionUtility.GetVariable(property.propertyPath, property.serializedObject.targetObjects, localizedStrings);
                string key = localizedStrings[0].Key ?? string.Empty;
                loadedLanguage.Texts.TryGetValue(key, out var oldText);
                
                GUI.enabled = EditorLanguageManager.ActiveLanguage.Value.Texts.ContainsKey(keyProp.stringValue);
                string newText = EditorGUI.DelayedTextField(valueRect, oldText);
                
                // apply changes to the language, if change
                if(oldText != newText && GUI.enabled)
                {
                    loadedLanguage.SetText(key, newText);
                    loadedLanguage.Save();
                    // LocalizationEditor.SetLanguageText(editorLangSettings.Manager.LoadedLanguageName, keyProp.stringValue, newText);
                }
                GUI.enabled = true;
            }
        }

    }
}
