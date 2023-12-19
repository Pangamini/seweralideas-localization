using System;
using UnityEngine;

namespace SeweralIdeas.Localization
{
    [Serializable]
    public struct LocalizedString
    {
        [SerializeField] private string m_key;
        public string Text
        {
            get
            {
                if(string.IsNullOrEmpty(m_key))
                    return null;

                LanguageData language = LocalizationManager.GetInstance().LoadedLanguage;
                if(language == null)
                    return m_key;
                
                if(language.Texts.TryGetValue(m_key, out string text))
                {
                    return text;
                }
                return m_key;
            }
        }
        public string Key => m_key;
    }
}
