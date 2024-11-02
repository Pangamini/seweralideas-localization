using UnityEngine;
namespace SeweralIdeas.Localization
{
    public class GlobalLanguageLoader : MonoBehaviour
    {
        [SerializeField] private LanguageManager m_manager;
        [SerializeField] private string          m_languageName;

        private LanguageLoader m_languageLoader;
        
        public LanguageManager Manager
        {
            get => m_manager;
            set
            {
                m_manager = value;
                UpdateParams();
            }
        }
        public string LanguageName
        {
            get => m_languageName;
            set
            {
                m_languageName = value;
                UpdateParams();
            }
        }

        protected void Awake()
        {
            m_languageLoader = new();
            m_languageLoader.LoadedLanguage.Changed += OnLoadedLangChanged;
            m_languageLoader.LoadParams = new()
            {
                Manager = m_manager,
                LanguageName = m_languageName
            };
        }

        protected void OnDestroy()
        {
            m_languageLoader.LoadedLanguage.Changed -= OnLoadedLangChanged;
            OnLoadedLangChanged(null, m_languageLoader.LoadedLanguage.Value);
            m_languageLoader.LoadParams = default;
            m_languageLoader = null;
        }
        
        protected void OnValidate() => UpdateParams();
        
        private void UpdateParams()
        {
            if(m_languageLoader != null)
                m_languageLoader.LoadParams = m_languageLoader.LoadParams = new()
                {
                    Manager = m_manager,
                    LanguageName = m_languageName
                };
        }

        private void OnLoadedLangChanged(LanguageData language, LanguageData oldLang) => GlobalLanguage.Language.Value = language;


    }
}
