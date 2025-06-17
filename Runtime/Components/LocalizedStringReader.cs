using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace SeweralIdeas.Localization
{
    public class LocalizedStringReader : MonoBehaviour
    {
        [SerializeField] 
        [FormerlySerializedAs("m_localizedString")] 
        private LocalizedString _localizedString;
        
        [SerializeField] 
        [FormerlySerializedAs("m_updateEvent")] 
        private UnityEvent<string> _updateEvent = new();

        public LocalizedString LocalizedString
        {
            get => _localizedString;
            set
            {
                if (_localizedString == value)
                    return;
                _localizedString = value;
                UpdateText();
            }
        }

        private void OnEnable() => GlobalLanguage.Language.Changed += OnLanguageLoaded;

        private void OnDisable() => GlobalLanguage.Language.Changed -= OnLanguageLoaded;

        private void OnLanguageLoaded(LanguageData languageData, LanguageData oldData) => UpdateText();

        private void UpdateText() => _updateEvent.Invoke(GetText(GlobalLanguage.Language.Value));

        private string GetText(LanguageData languageData)
        {
            if(languageData == null)
                return "NO LANGUAGE";
            var text = languageData.Texts.GetValueOrDefault(_localizedString.Key ?? string.Empty, "NOT FOUND");
            return text;
        }

        #if UNITY_EDITOR
        protected void OnValidate()
        {
            UpdateText();
        }
        #endif
    }
}
