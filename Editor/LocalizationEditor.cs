using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SeweralIdeas.Localization.Editor
{
    public static class LocalizationEditor
    {
        /// <summary>
        /// True when the loaded language was edited and should be saved to file
        /// </summary>
        private static readonly Dictionary<string, LanguageData> s_languages = new();
        private static readonly HashSet<string> s_dirtyLanguages = new();

        public static void DiscardAllChanges()
        {
            s_languages.Clear();
            s_dirtyLanguages.Clear();
        }
        
        public static void SetLanguageText(string languageName, string key, string newText)
        {
            if(!TryGetLanguage(languageName, out var languageData))
            {
                Debug.LogError($"Couldn't load language \"{languageName}\"");
                return;
            }
            
            s_dirtyLanguages.Add(languageName);
            languageData.SetText(key, newText);

            var manager = LocalizationManager.GetInstance();
            if(manager != null && manager.LoadedLanguageName == languageName)
            {
                manager.LoadedLanguage.SetText(key, newText);
            }
            SaveChanges();
        }
        
        public static void DeleteLanguageText(string languageName, string key)
        {
            if(!TryGetLanguage(languageName, out var languageData))
            {
                Debug.LogError($"Couldn't load language \"{languageName}\"");
                return;
            }
            
            s_dirtyLanguages.Add(languageName);
            languageData.RemoveText(key);

            var manager = LocalizationManager.GetInstance();
            if(manager != null && manager.LoadedLanguageName == languageName)
            {
                manager.LoadedLanguage.RemoveText(key);
            }
            SaveChanges();
        }

        public static bool TryGetLanguage(string languageName, out LanguageData languageData)
        {
            if(!s_languages.TryGetValue(languageName, out languageData))
            {
                if(!LocalizationManager.GetInstance().Headers.TryGetValue(languageName, out var header))
                {
                    return false;
                }
                
                languageData = LanguageData.Load(header);
                if(languageData == null)
                {
                    return false;
                }
                
                s_languages.Add(languageName, languageData);
                return true;
            }

            return true;
        }
        
        public static void SaveChanges()
        {
            foreach (var dirtyLang in s_dirtyLanguages)
            {
                if(!s_languages.TryGetValue(dirtyLang, out var languageData))
                    continue;
                
                Debug.Log($"Saving language \"{dirtyLang}\"");
                languageData.Save();
            }
            
            s_dirtyLanguages.Clear();
        }

    }
}
