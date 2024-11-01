using UnityEngine;
namespace SeweralIdeas.Localization
{
    public class GlobalLanguageLoader : MonoBehaviour
    {
        [SerializeField] private LanguageLoader.Params             m_params;

        private LanguageLoader m_languageLoader;

        protected void Awake()
        {
            m_languageLoader = new();
            m_languageLoader.LoadedLanguage.Changed += OnLoadedLangChanged;
            m_languageLoader.LoadParams = m_params;
        }

        protected void OnDestroy()
        {
            m_languageLoader.LoadedLanguage.Changed -= OnLoadedLangChanged;
            OnLoadedLangChanged(null, m_languageLoader.LoadedLanguage.Value);
            m_languageLoader.LoadParams = default;
            m_languageLoader = null;
        }
        
        protected void OnValidate()
        {
            if(m_languageLoader != null)
                m_languageLoader.LoadParams = m_params;
        }

        private void OnLoadedLangChanged(LanguageData language, LanguageData oldLang) => GlobalLanguage.Language.Value = language;


    }
}
