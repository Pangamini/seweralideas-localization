using System;
using SeweralIdeas.UnityUtils;
using SeweralIdeas.Utils;
using UnityEngine;

namespace SeweralIdeas.Localization
{
    /// <summary>
    /// Holds a reference to a global runtime language.
    /// </summary>
    public static class GlobalLanguage
    {
        public static readonly Observable<LanguageInfo> Language = new(LanguageInfo.Default);

    }

    public struct LanguageInfo
    {
        public static LanguageInfo Default => new LanguageInfo()
        {
            LanguageData = null,
            Settings = LanguageSettings.Default
        };
        
        public LanguageData     LanguageData;
        public LanguageSettings Settings;

        public readonly string GetText(LocalizedString str)
        {
            if (LanguageData == null)
                return Settings.NoLanguage.Evaluate(LanguageData, str);

            // ReSharper disable once CanSimplifyDictionaryTryGetValueWithGetValueOrDefault
            if (LanguageData.Texts.TryGetValue(str.Key ?? string.Empty, out string localized))
                return localized;
            
            return Settings.NoKey.Evaluate(LanguageData, str);
        }
    }

    [Serializable]
    public struct LanguageSettings
    {
        [SerializeField] private FallbackText m_noLanguage;
        [SerializeField] private FallbackText m_noKey;

        public static LanguageSettings Default => new()
        {
            NoKey = FallbackText.Default,
            NoLanguage = FallbackText.Default,
        };
        
        public FallbackText NoLanguage
        {
            get => m_noLanguage;
            set => m_noLanguage = value;
        }

        public FallbackText NoKey
        {
            get => m_noKey;
            set => m_noKey = value;
        }
    }
    
    [Serializable]
    public struct FallbackText
    {
        public static FallbackText Default =>new(){Format = "<{key}>"};
        
        [SerializeField] private string m_format;

        public string Format
        {
            get => m_format;
            set => m_format = value;
        }

        public string Evaluate(LanguageData languageData, LocalizedString str)
        {
            using (StringBuilderPool.Get(out var sb))
            {
                sb.Append(m_format);
                sb.Replace("{key}", str.Key);
                return sb.ToString();
            }
        }
    }
}
