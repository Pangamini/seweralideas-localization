using System;
using System.Collections.Generic;
using SeweralIdeas.Utils;
using UnityEditor;
using UnityEngine;
namespace SeweralIdeas.Localization.Editor
{
    public static class EditorLanguageManager
    {
        public const   string                PlayerPrefsKey = "EditorLanguageSettings";
        
        private static readonly Dictionary<string, Record>  Records = new();
        private static          Request                     s_activeLanguageRequest;

        public static readonly  Observable<LanguageManager> ActiveManager            = new();
        
        private static readonly Observable<LanguageData>    ActiveLanguageObservable = new();
        public static Observable<LanguageData>.Readonly ActiveLanguage => ActiveLanguageObservable.ReadOnly;
        
        public static Request CreateRequest(string languageName) => new Request(languageName);

        public static string ActiveLanguageName
        {
            get => s_activeLanguageRequest?.LangName;
            set
            {
                if(s_activeLanguageRequest != null && s_activeLanguageRequest.LangName == value)
                    return;

                if(s_activeLanguageRequest != null)
                    s_activeLanguageRequest.Language.Changed -= OnActiveLanguageRequestChanged;
                
                s_activeLanguageRequest?.Dispose();
                s_activeLanguageRequest = CreateRequest(value);

                s_activeLanguageRequest.Language.Changed += OnActiveLanguageRequestChanged;
            }
        }
        
        private static void OnActiveLanguageRequestChanged(LanguageData lang, LanguageData oldLang) => ActiveLanguageObservable.Value = lang;

        static EditorLanguageManager()
        {
            var json = EditorPrefs.GetString(PlayerPrefsKey);
            
            if(string.IsNullOrWhiteSpace(json))
                return;
        
            // deserialize
            object obj = new LanguageLoader.Params();
            EditorJsonUtility.FromJsonOverwrite(json, obj);
            var loadedParams = (LanguageLoader.Params)obj;
            
            ActiveManager.Value = loadedParams.Manager;
            ActiveLanguageName = loadedParams.LanguageName;
            
            ActiveManager.Changed += (newManager, oldManager) =>
            {
                foreach (var pair in Records)
                    pair.Value.SetLanguageManager(newManager);
            };
        }
        
        private class Record
        {
            public int Count;
            public string LangName { get; private set; }
            public readonly LanguageLoader Loader = new LanguageLoader();

            public void SetLanguageManager(LanguageManager manager)
            {
                Loader.LoadParams = new()
                {
                    LanguageName = LangName,
                    Manager = manager
                };
            }
            
            public Record(string langName)
            {
                LangName = langName;
            }
            
            public void Dispose()
            {
                Loader.LoadParams = default;
                LangName = null;
            }
        }
        
        private static Record RegisterRequest(Request request)
        {
            if(!Records.TryGetValue(request.LangName, out var record))
            {
                record = new Record(request.LangName);
                record.SetLanguageManager(ActiveManager.Value);
                Records.Add(request.LangName, record);
            }
            
            record.Count++;
            return record;
        }

        private static void UnregisterRequest(Request request)
        {
            if(!Records.TryGetValue(request.LangName, out var record))
                return;

            record.Count--;

            if(record.Count <= 0)
            {
                Records.Remove(request.LangName);
                record.Dispose();
            }
        }
        
        
        public class Request : IDisposable
        {
            private Record m_record;
            public string LangName { get; private set; }
            public Observable<LanguageData>.Readonly Language => m_record.Loader.LoadedLanguage;
            
            public Request(string languageName)
            {
                LangName = languageName;
                m_record = RegisterRequest(this);
            }
            
            public void Dispose()
            {
                if(LangName == null)
                    return;
                
                UnregisterRequest(this);
                m_record = null;
                LangName = null;
                GC.SuppressFinalize(this);
            }

            ~Request() => Dispose();
        }

    }
}
