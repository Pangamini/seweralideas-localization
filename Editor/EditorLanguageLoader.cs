using SeweralIdeas.Utils;
using UnityEditor;

namespace SeweralIdeas.Localization.Editor
{
    public static class EditorLanguageLoader
    {
        public const string PlayerPrefsKey = "EditorLanguageSettings";

        public static Observable<LanguageData>.Readonly LoadedLanguage => Loader.LoadedLanguage;
        
        private static readonly LanguageLoader Loader = new();

        static EditorLanguageLoader()
        {
            var json = EditorPrefs.GetString(PlayerPrefsKey);
            
            if(string.IsNullOrWhiteSpace(json))
                return;

            object settings = Loader.LoadParams;
            EditorJsonUtility.FromJsonOverwrite(json, settings);
            Loader.LoadParams = (LanguageLoader.Params)settings;
        }
        
        public static LanguageLoader.Params LoadParams
        {
            get => Loader.LoadParams;
            set
            {
                Loader.LoadParams = value;
                
                // write
                var json = EditorJsonUtility.ToJson(value);
                EditorPrefs.SetString(PlayerPrefsKey, json);
            }
        }
    }
}
