using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SeweralIdeas.Localization
{
    public class LocalizedStringReader : MonoBehaviour
    {
        [SerializeField] private LocalizedString m_localizedString;
        [SerializeField] private UnityEvent<string> m_updateEvent = new();
        
        private void OnEnable()
        {
            LocalizationManager.GetInstance().LanguageLoaded += OnLanguageLoaded;
            Refresh();
        }

        private void OnDisable()
        {
            LocalizationManager.GetInstance().LanguageLoaded -= OnLanguageLoaded;
        }
        
        private void OnLanguageLoaded(LanguageData obj) => Refresh();

        private void Refresh()
        {
            string str = m_localizedString.Text;
            m_updateEvent.Invoke(str);
        }
    }
}
