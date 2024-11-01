using System;
using System.Collections;
using System.Collections.Generic;
using SeweralIdeas.UnityUtils;
using SeweralIdeas.Utils;
using UnityEngine;
using UnityEngine.Events;

namespace SeweralIdeas.Localization
{
    public class LocalizedAudioReader : MonoBehaviour
    {
        [SerializeField] private string m_key;
        [SerializeField] private UnityEvent<AudioClip> m_updateEvent = new();
        private AsyncLoadManager<string, AudioClip>.Request m_request;
        private Coroutine m_routine;

        
        protected void OnEnable() => GlobalLanguage.Language.Changed += OnLanguageLoaded;


        protected void OnDisable()
        {
            GlobalLanguage.Language.Changed -= OnLanguageLoaded;
            ClearOldRequest();
        }
        
        private void ClearOldRequest()
        {
            if(m_routine != null)
            {
                StopCoroutine(m_routine);
                m_routine = null;
            }
            
            if(m_request != null)
            {
                m_request.Dispose();
                m_request = null;
            }
        }

        private void OnLanguageLoaded(LanguageData languageData, LanguageData oldData) => UpdateAudio();
        
        
        void UpdateAudio()
        {
            ClearOldRequest();

            LanguageData languageData = GlobalLanguage.Language.Value;
                
            if(languageData == null || !languageData.TryGetAudioUrl(m_key, out string audioFile))
            {
                m_updateEvent.Invoke(null);
                return;
            }

            m_request = AudioManager.Instance.CreateRequest(audioFile);
            m_routine = StartCoroutine(LoadRoutine());
        }
        
        private IEnumerator LoadRoutine()
        {
            while(!m_request.LoadTask.IsCompleted)
            {
                yield return Waits.waitForNextFrame;
            }

            if(m_request.LoadTask.IsCompletedSuccessfully)
            {
                AudioClip result = m_request.LoadTask.Result;
                m_updateEvent.Invoke(result);
            }
        }
    }
}
