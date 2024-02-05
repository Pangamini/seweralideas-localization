using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace SeweralIdeas.Localization.Editor
{
    public static class LocalizationEditor
    {
        private static readonly Dictionary<string, Task<LanguageData>> s_languages = new();
        private static readonly HashSet<string> s_dirtyLanguages = new();

        public static void DiscardAllChanges()
        {
            s_languages.Clear();
            s_dirtyLanguages.Clear();
        }

        public static void SetLanguageText(string languageName, string key, string newText)
        {
            var task = SetLanguageTextAsync(languageName, key, newText);
            s_languages[languageName] = task;
        }
        
        public async static Task<LanguageData> SetLanguageTextAsync(string languageName, string key, string newText)
        {
            var languageData = await TryGetLanguage(languageName);
            if(languageData == null)
            {
                Debug.LogError($"Couldn't load language \"{languageName}\"");
                return null;
            }
            
            s_dirtyLanguages.Add(languageName);
            languageData.SetText(key, newText);

            var manager = LocalizationManager.GetInstance();
            if(manager != null && manager.LoadedLanguageName == languageName)
            {
                manager.LoadedLanguage.SetText(key, newText);
            }
            await SaveChangesAsync();
            return languageData;
        }

        public static void DeleteLanguageText(string languageName, string key)
        {
            var task = DeleteLanguageTextAsync(languageName, key);
            s_languages[languageName] = task;
        }
        
        public async static Task<LanguageData> DeleteLanguageTextAsync(string languageName, string key)
        {
            var languageData = await TryGetLanguage(languageName);
            if(languageData == null)
            {
                Debug.LogError($"Couldn't load language \"{languageName}\"");
                return null;
            }
            
            s_dirtyLanguages.Add(languageName);
            languageData.RemoveText(key);

            var manager = LocalizationManager.GetInstance();
            if(manager != null && manager.LoadedLanguageName == languageName)
            {
                manager.LoadedLanguage.RemoveText(key);
            }
            await SaveChangesAsync();
            return languageData;
        }

        
        public static Task<LanguageData> TryGetLanguage(string languageName)
        {
            if(s_languages.TryGetValue(languageName, out var languageData))
                return languageData;
            
            if(!LocalizationManager.GetInstance().Headers.TryGetValue(languageName, out var header))
            {
                return null;
            }
                
            languageData = LanguageData.LoadAsync(header);
            s_languages.Add(languageName, languageData);
            return languageData;

        }
        
        public async static Task SaveChangesAsync()
        {
            foreach (var dirtyLang in s_dirtyLanguages)
            {
                if(!s_languages.TryGetValue(dirtyLang, out var languageData))
                    continue;
                
                Debug.Log($"Saving language \"{dirtyLang}\"");
                await (await languageData).Save();
            }
            
            s_dirtyLanguages.Clear();
        }

    }
}
