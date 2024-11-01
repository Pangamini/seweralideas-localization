using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SeweralIdeas.Collections;
using SeweralIdeas.Utils;
using UnityEngine;

namespace SeweralIdeas.Localization
{
    public class LanguageLoader
    {
        private          Params                   m_params;
        private          Task<LanguageData>       m_currentTask;
        private readonly Observable<LanguageData> m_loadedLanguage = new();
        
        public Observable<LanguageData>.Readonly LoadedLanguage => m_loadedLanguage.ReadOnly;
        
        [Serializable]
        public struct Params
        {
            [SerializeField] public LocalizationManager Manager;
            [SerializeField] public string              LanguageName;
        }
        
        public Params LoadParams
        {
            get => m_params;
            set
            {
                if(EqualityComparer<Params>.Default.Equals(m_params, value))
                    return;

                if(m_params.Manager != null)
                    m_params.Manager.Headers.Changed -= OnHeadersChanged;

                m_params = value;
                m_params = value;

                if(m_params.Manager != null)
                    m_params.Manager.Headers.Changed += OnHeadersChanged;
                else
                    OnHeadersChanged(default);
            }
        }
        
        private async void OnHeadersChanged(ReadonlyDictView<string, LanguageHeader> target)
        {

            if(m_currentTask != null)
            {
                m_currentTask.Dispose();
                m_currentTask = null;
            }

            if(!target.TryGetValue(LoadParams.LanguageName, out LanguageHeader languageHeader))
            {
                m_loadedLanguage.Value = null;
                return;
            }
            
            var task = LanguageData.LoadAsync(languageHeader);
            m_currentTask = task;
            LanguageData result = await task;

            if(task == m_currentTask)
                m_loadedLanguage.Value = result;
        }
    }
}
