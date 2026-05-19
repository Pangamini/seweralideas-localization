using System;
using System.Threading.Tasks;
using SeweralIdeas.Collections;
using SeweralIdeas.Utils;
using UnityEngine;

namespace SeweralIdeas.Localization
{
    public class LanguageLoader
    {
        private          Params                   _params;
        private          Task<LanguageData>       _currentTask;
        private readonly Observable<LanguageData> _loadedLanguage = new();
        
        public Observable<LanguageData>.Readonly LoadedLanguage => _loadedLanguage.ReadOnly;
        
        [Serializable]
        public struct Params : IEquatable<Params>
        {
            [SerializeField] public LanguageManager Manager;
            [SerializeField] public string          LanguageName;

            public bool Equals(Params other) => Equals(Manager, other.Manager) && LanguageName == other.LanguageName;
            public override bool Equals(object obj) => obj is Params other && Equals(other);
            public override int GetHashCode() => HashCode.Combine(Manager, LanguageName);
            public static bool operator ==(Params left, Params right) => left.Equals(right);
            public static bool operator !=(Params left, Params right) => !left.Equals(right);
        }
        
        public Params LoadParams
        {
            get => _params;
            set
            {
                if(_params == value)
                    return;

                if(_params.Manager != null)
                    _params.Manager.Headers.Changed -= OnHeadersChanged;

                _params = value;

                if(_params.Manager != null)
                    _params.Manager.Headers.Changed += OnHeadersChanged;
                else
                    OnHeadersChanged(null, null);
            }
        }

        private void OnHeadersChanged(ReadonlyDictView<string, LanguageHeader>? maybeTarget, ReadonlyDictView<string, LanguageHeader>? _)
        {
            var task = LoadLanguageDataAsync(maybeTarget);
            _currentTask = task;
            var __ = CompleteLoadingAsync(task);
        }

        private async Task CompleteLoadingAsync(Task<LanguageData> task)
        {
            try
            {
                var result = await task;
                if (task == _currentTask)
                    _loadedLanguage.Value = result;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
        
        private async Task<LanguageData> LoadLanguageDataAsync(ReadonlyDictView<string, LanguageHeader>? maybeTarget)
        {
            if(maybeTarget is not {} target)
                return null;
            
            if(string.IsNullOrEmpty(LoadParams.LanguageName) || !target.TryGetValue(LoadParams.LanguageName, out LanguageHeader languageHeader))
                return null;
            
            var task = LanguageData.LoadAsync(languageHeader);
            LanguageData result = await task;
            return result;
        }
    }
}
