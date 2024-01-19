using System;
using System.Collections.Generic;
using System.IO;
using SeweralIdeas.Collections;
using SeweralIdeas.Config;
using SeweralIdeas.UnityUtils;
using UnityEditor;
using UnityEngine;

namespace SeweralIdeas.Localization
{
    [CreateAssetMenu(menuName = "AdventureEngine/"+nameof(LocalizationManager), fileName = nameof(LocalizationManager))]
    public class LocalizationManager : SingletonAsset<LocalizationManager>
    {
        [SerializeField] private StringConfigValue m_language;
        [SerializeField] private string m_streamingAssetsPath = "Languages/";
        [SerializeField] private string m_headerFilename = "header.json";
        
        [NonSerialized] private StringConfigValue m_subscribedLanguageVar;
        [NonSerialized] private readonly Dictionary<string, LanguageHeader> m_langHeaders = new();
        [NonSerialized] private string m_loadedLanguageName;
        [NonSerialized] private LanguageHeader m_loadedLanguageHeader;
        [NonSerialized] private LanguageData m_loadedLanguageData;
        [NonSerialized] private bool m_initialized;

        
        [InitializeOnLoadMethod]
        private static void Reinit()
        {
            var inst = GetInstance();
            if (!inst.EnsureInitialized())
                inst.DetectLanguages();
        }
        
        public event Action<LanguageData> LanguageLoaded;
        
        public ReadonlyDictView<string, LanguageHeader> Headers => new(m_langHeaders);

        private void SetSubscribedConfigValue(StringConfigValue variable)
        {
            if(m_subscribedLanguageVar == variable)
                return;
            
            if(m_subscribedLanguageVar != null)
            {
                m_subscribedLanguageVar.onValueChanged -= LoadLanguageVar;
            }
            m_subscribedLanguageVar = variable;
            if(m_subscribedLanguageVar != null)
            {
                m_subscribedLanguageVar.onValueChanged += LoadLanguageVar;
                LoadLanguageVar(m_subscribedLanguageVar.Value);
            }
        }

        public void DetectLanguages()
        {
            m_langHeaders.Clear();
            
            string langRootDirPath = Path.Combine(Application.streamingAssetsPath, m_streamingAssetsPath);
            DirectoryInfo langRootDir = Directory.Exists(langRootDirPath) ? new DirectoryInfo(langRootDirPath) : Directory.CreateDirectory(langRootDirPath);

            foreach (DirectoryInfo langDir in langRootDir.EnumerateDirectories())
            {
                foreach (FileInfo headerFile in langDir.EnumerateFiles(m_headerFilename, SearchOption.TopDirectoryOnly))
                {
                    try
                    {
                        LanguageHeader header = LanguageHeader.Load(headerFile.FullName);
                        if(header == null)
                        {
                            Debug.LogError($"Failed to read language header \"{headerFile.FullName}\"");
                            continue;
                        }
                        
                        string langName = langDir.Name;
                        m_langHeaders.Add(langName, header);
                    }
                    catch( Exception e )
                    {
                        Debug.LogException(new Exception($"Failed to read language header \"{headerFile.FullName}\"", e));
                        continue;
                    }
                    break;
                }
            }
            
            LoadLanguageVar(m_subscribedLanguageVar.Value);
        }

        public void SetLanguage(string languageName)
        {
            m_subscribedLanguageVar.Value = languageName;
        }
        
        private void LoadLanguageVar(string langId)
        {
            m_loadedLanguageData = null;
            
            if(!string.IsNullOrWhiteSpace(m_loadedLanguageName) && m_langHeaders.TryGetValue(langId, out m_loadedLanguageHeader))
            {
                m_loadedLanguageData = LanguageData.Load(m_loadedLanguageHeader);
            }

            m_loadedLanguageName = langId;

            LanguageLoaded?.Invoke(m_loadedLanguageData);
        }
        
        public string LoadedLanguageName => m_loadedLanguageName;
        public LanguageHeader LoadedLanguageHeader => m_loadedLanguageHeader;
        public LanguageData LoadedLanguage
        {
            get
            {
                EnsureInitialized();
                return m_loadedLanguageData;
            }
        }

        private void OnEnable()
        {
            EnsureInitialized();
        }
        
        private bool EnsureInitialized()
        {
            if(m_initialized)
                return false;
            
            SetSubscribedConfigValue(m_language);
            DetectLanguages();
            m_initialized = true;
            return true;
        }

        private void OnDisable()
        {
            SetSubscribedConfigValue(null);
        }

        private void OnValidate()
        {
            SetSubscribedConfigValue(m_language);
        }
        
    }
}
