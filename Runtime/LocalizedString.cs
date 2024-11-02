using System;
using UnityEngine;

namespace SeweralIdeas.Localization
{
    [Serializable]
    public struct LocalizedString
    {
        [SerializeField] private string m_key;
        public string Key => m_key;
        public string Text
        {
            get
            {
                LanguageData language = GlobalLanguage.Language.Value;
                if(language == null)
                    return "NO LANGUAGE";
                
                return language.Texts.TryGetValue(m_key, out var value) ? value : m_key;

            }
        }
    }
}
