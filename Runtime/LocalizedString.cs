using System;
using UnityEngine;

namespace SeweralIdeas.Localization
{
    [Serializable]
    public struct LocalizedString : IEquatable<LocalizedString>
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
        
        
        public bool Equals(LocalizedString other) => m_key == other.m_key;

        public override bool Equals(object obj) => obj is LocalizedString other && Equals(other);

        public override int GetHashCode() => (m_key != null ? m_key.GetHashCode() : 0);

        public static bool operator ==(LocalizedString left, LocalizedString right) => left.Equals(right);

        public static bool operator !=(LocalizedString left, LocalizedString right) => !left.Equals(right);
    }
}
