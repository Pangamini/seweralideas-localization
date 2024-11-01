using UnityEngine;
namespace SeweralIdeas.Localization
{
    [ExecuteAlways]
    public class GlobalLanguageLoader : MonoBehaviour
    {
        [SerializeField] private LanguageLoader.Params             m_params;

        private readonly LanguageLoader m_languageLoader = new();

        protected void Awake()
        {
            m_languageLoader.LoadedLanguage.Changed += OnLoadedLangChanged;
            m_languageLoader.LoadParams = m_params;
        }

        protected void OnDestroy()
        {
            m_languageLoader.LoadedLanguage.Changed -= OnLoadedLangChanged;
            OnLoadedLangChanged(null);
            m_languageLoader.LoadParams = default;
        }
        
        protected void OnValidate() => m_languageLoader.LoadParams = m_params;
        
        private void OnLoadedLangChanged(LanguageData language) => GlobalLanguage.Language.Value = language;


    }
}
