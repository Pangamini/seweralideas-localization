using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SeweralIdeas.Collections;
using SeweralIdeas.Utils;
using UnityEngine.Pool;
#if UNITY_EDITOR
using UnityEngine;
#endif

namespace SeweralIdeas.Localization
{
    [CreateAssetMenu(menuName = "SeweralIdeas/"+nameof(LanguageManager), fileName = nameof(LanguageManager))]
    public class LanguageManager : ScriptableObject
    {
        [SerializeField] private string                                                        m_streamingAssetsPath = "Languages/";
        [SerializeField] private string                                                        m_headerFilename      = "header.json";
        [SerializeField] private string                                                        m_webGLHeader         = "languages.txt";

        private Task<ReadonlyDictView<string, LanguageHeader>> m_currentTask = null;
        
        private readonly Observable<ReadonlyDictView<string, LanguageHeader>> m_headers = new();
        public Observable<ReadonlyDictView<string, LanguageHeader>>.Readonly Headers => m_headers.ReadOnly;

        private void OnEnable() => Reload();

        protected void OnDisable()
        {
            if(m_currentTask != null)
            {
                m_currentTask.Dispose();
                m_currentTask = null;
            }
        }

        public async Task Reload()
        {
            m_headers.Value = default;
            if (m_currentTask != null)
                m_currentTask.Dispose();
            
            var task = LoadLanguageHeadersAsync();
            m_currentTask = task;
            
            var result = await task;
            
            if(task == m_currentTask)
                m_headers.Value = result;
        }
        
        private async Task<ReadonlyDictView<string, LanguageHeader>> LoadLanguageHeadersAsync()
        {
            (string name, string header)[] headerInfos = await GetHeadersInfoAsync();

            // store header tasks in an array
            var tasks = new Task<LanguageHeader>[headerInfos.Length];
            
            // start loading headers
            for( int index = 0; index < headerInfos.Length; index++ )
            {
                (string name, string header) headerPair = headerInfos[index];
                tasks[index] = LanguageHeader.Load(headerPair.header);
            }
            
            // finish loading headers
            Dictionary<string, LanguageHeader> headers = new();
            for( int index = 0; index < tasks.Length; index++ )
            {
                LanguageHeader header = await tasks[index];
                var info = headerInfos[index];
                headers.Add(info.name, header);
            }
            
            return new(headers);
        }
        
        private async Task<(string name, string header)[]> GetHeadersInfoAsync()
        {
            using (ListPool<(string name, string header)>.Get(out var languages))
            {
                string langRootDirPath = Path.Combine(Application.streamingAssetsPath, m_streamingAssetsPath);
        
                #if UNITY_WEBGL
                
                // In WebGL, a languages file must contain a list of all languages
                string content = await LocalizationUtils.LoadTextFile(Path.Combine(langRootDirPath, m_webGLHeader));
                using StringReader reader = new StringReader(content);
                while(true)
                {
                    string line = reader.ReadLine();
                    if(line == null)
                        break;
        
                    string headerPath = Path.Combine(Path.Combine(langRootDirPath, line), m_headerFilename);
                    languages.Add((line, headerPath));
                }
        
                #else
                
                // In standalone build, language list can be generated from dictionaries
                DirectoryInfo langRootDir = Directory.Exists(langRootDirPath) ? new DirectoryInfo(langRootDirPath) : Directory.CreateDirectory(langRootDirPath);
                foreach (DirectoryInfo langDir in langRootDir.EnumerateDirectories())
                {
                    foreach (FileInfo headerFile in langDir.EnumerateFiles(m_headerFilename, SearchOption.TopDirectoryOnly))
                    {
                        languages.Add((langDir.Name, headerFile.FullName));
                        break;
                    }
                }
                
                #endif
                
                return languages.ToArray();
            }
        }
    }
}
