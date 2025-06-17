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
        [SerializeField] private string _streamingAssetsPath = "Languages/";
        [SerializeField] private string _headerFilename      = "header.json";
        [SerializeField] private string _webGLHeader         = "languages.txt";

        private Task<ReadonlyDictView<string, LanguageHeader>?> _currentTask = null;
        
        private readonly Observable<ReadonlyDictView<string, LanguageHeader>?> _headers = new();
        public Observable<ReadonlyDictView<string, LanguageHeader>?>.Readonly Headers => _headers.ReadOnly;

        private void OnEnable() => Reload();

        protected void OnDisable()
        {
            if (_currentTask == null)
                return;
            _currentTask.Dispose();
            _currentTask = null;
        }

        public Task Reload()
        {
            var task = LoadLanguageHeadersAsync();
            _currentTask = task;
            _ = RunUpdateAsync(task); // fire-and-forget internally
            return task; // return the actual task for external awaiting
        }

        private async Task RunUpdateAsync(Task<ReadonlyDictView<string, LanguageHeader>?> task)
        {
            try
            {
                var result = await task;
                if (task == _currentTask)
                    _headers.Value = result;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex); // or store/log if needed
            }
        }
        
        private async Task<ReadonlyDictView<string, LanguageHeader>?> LoadLanguageHeadersAsync()
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
                try
                {
                    LanguageHeader header = await tasks[index];
                    var info = headerInfos[index];
                    headers.Add(info.name, header);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
            
            return new(headers);
        }
        
        private async Task<(string name, string header)[]> GetHeadersInfoAsync()
        {
            using (ListPool<(string name, string header)>.Get(out var languages))
            {
                string langRootDirPath = Path.Combine(Application.streamingAssetsPath, _streamingAssetsPath);
        
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
                    foreach (FileInfo headerFile in langDir.EnumerateFiles(_headerFilename, SearchOption.TopDirectoryOnly))
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
