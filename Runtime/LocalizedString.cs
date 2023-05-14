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
                
                if(LocalizationManager.GetInstance().LoadedLanguage.Texts.TryGetValue(m_key, out string text))
                {
                    return text;
                }
                return m_key;
            }
        }
    }
}
