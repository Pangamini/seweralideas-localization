using System.Collections.Generic;
using SeweralIdeas.UnityUtils.Drawers;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace SeweralIdeas.Localization
{
    public class LocalizedStringReader : MonoBehaviour
    {
        [SerializeField]
        private bool _hasValue = true;
        
        [SerializeField] 
        [Condition(nameof(_hasValue))]
        [FormerlySerializedAs("m_localizedString")] 
        private LocalizedString _localizedString;
        
        [SerializeField] 
        [FormerlySerializedAs("m_updateEvent")] 
        private UnityEvent<string> _updateEvent = new();

        public LocalizedString? LocalizedString
        {
            get => _hasValue ? _localizedString : null; 
            set
            {
                if (!_hasValue && !value.HasValue)
                    return;
                
                if ( value is {} newValue)
                {
                    if(_hasValue && _localizedString == newValue) 
                        return;
                    
                    _localizedString = newValue;
                    _hasValue = true;
                }
                else
                {
                    _hasValue = false;
                }

                UpdateText();
            }
        }

        private void OnEnable() => GlobalLanguage.Language.Changed += OnLanguageLoaded;

        private void OnDisable() => GlobalLanguage.Language.Changed -= OnLanguageLoaded;

        private void OnLanguageLoaded(LanguageInfo languageData, LanguageInfo oldData) => UpdateText();

        private void UpdateText() => _updateEvent.Invoke(GlobalLanguage.Language.Value.GetText(_localizedString));

        #if UNITY_EDITOR
        protected void OnValidate()
        {
            UpdateText();
        }
        #endif
    }
}
