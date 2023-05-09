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
            
            LocalizationManager localizationManager = LocalizationManager.GetInstance();
            var keyProp = property.FindPropertyRelative("m_key");
            Rect keyRect = new Rect(position.x, position.y, position.width - KeyPopupWidth - KeyAddWidth, EditorGUIUtility.singleLineHeight);
            Rect popupRect = new Rect(keyRect.xMax, keyRect.y, KeyPopupWidth, keyRect.height);
            Rect addRect = new Rect(popupRect.xMax, popupRect.y, KeyAddWidth, keyRect.height);
            
            Rect valueRowRect = new Rect(position.x, keyRect.yMax, position.width, position.height - keyRect.height);

            EditorGUI.PropertyField(keyRect, keyProp, label);

            if(GUI.Button(popupRect, GUIContent.none, EditorStyles.popup))
            {
                var texts = localizationManager.LoadedLanguage.Texts;
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
                
                AdvancedPopupWindow.ShowWindow(controlId, scrRect, 0, options, true, null, onKeySelected);
            }

            if (GUI.Button(addRect, GUIContent.none, s_addKeyStyle))
            {
                
            }

            // Show localized value field
            const float imageWidth = 32;
            var iconRect = new Rect(valueRowRect.x, valueRowRect.y, imageWidth, valueRowRect.height);
            var valueRect = new Rect(valueRowRect.x + EditorGUIUtility.labelWidth, valueRowRect.y, valueRowRect.width - EditorGUIUtility.labelWidth, valueRowRect.height);

            if(GUI.Button(iconRect, localizationManager.LoadedLanguage.Header.Texture, EditorStyles.label))
            {
                var controlId = GUIUtility.GetControlID(FocusType.Keyboard, iconRect);
                var scrRect = new Rect(GUIUtility.GUIToScreenPoint(iconRect.position), new Vector2(128, popupRect.height));
                
                List<GUIContent> options = new();
                List<string> langNames = new();
                foreach (var pair in localizationManager.Headers)
                {
                    langNames.Add(pair.Key);
                    options.Add(new GUIContent(pair.Value.Name, pair.Value.Texture));
                }
                
                Action<int> onLangSelected = (index) =>
                {
                    localizationManager.SetLanguage(langNames[index]);
                };
                AdvancedPopupWindow.ShowWindow(controlId, scrRect, 0, options, true, null, onLangSelected);
            }
            //EditorGUI.DrawTextureTransparent(iconRect, localizationManager.LoadedLanguage.Header.Texture, ScaleMode.ScaleToFit);
            
            using (ListPool<LocalizedString>.Get(out var localizedStrings))
            {
                EditorReflectionUtility.GetVariable(property.propertyPath, property.serializedObject.targetObjects, localizedStrings);
                string oldText = localizedStrings[0].Text;
                string newText = EditorGUI.DelayedTextField(valueRect, localizedStrings[0].Text);
                
                if(oldText != newText)
                {
                    localizationManager.SetLoadedLanguageText(keyProp.stringValue, newText);
                }
            }
        }
        
    }
}
