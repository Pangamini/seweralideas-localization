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
                LanguageInfo langInfo = GlobalLanguage.Language.Value;
                return langInfo.GetText(this);
            }
        }
        
        
        public bool Equals(LocalizedString other) => m_key == other.m_key;

        public override bool Equals(object obj) => obj is LocalizedString other && Equals(other);

        public override int GetHashCode() => (m_key != null ? m_key.GetHashCode() : 0);

        public static bool operator ==(LocalizedString left, LocalizedString right) => left.Equals(right);

        public static bool operator !=(LocalizedString left, LocalizedString right) => !left.Equals(right);
    }
}
