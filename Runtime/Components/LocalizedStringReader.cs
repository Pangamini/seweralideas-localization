using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
namespace SeweralIdeas.Localization
{
    public class LocalizedStringReader : MonoBehaviour
    {
        [SerializeField] private LocalizedString m_localizedString;
        [SerializeField] private UnityEvent<string> m_updateEvent = new();
        
        private void OnEnable() => GlobalLanguage.Language.Changed += OnLanguageLoaded;

        private void OnDisable() => GlobalLanguage.Language.Changed -= OnLanguageLoaded;

        private void OnLanguageLoaded(LanguageData languageData, LanguageData oldData) => UpdateText();

        private void UpdateText() => m_updateEvent.Invoke(GetText(GlobalLanguage.Language.Value));

        private string GetText(LanguageData languageData)
        {
            if(languageData == null)
                return "NO LANGUAGE";
            var text = languageData.Texts.GetValueOrDefault(m_localizedString.Key, "NOT FOUND");
            return text;
        }

        #if UNITY_EDITOR
        protected void OnValidate()
        {
            if(Application.isPlaying && gameObject.scene.IsValid())
                UpdateText();
        }
        #endif
    }
}
