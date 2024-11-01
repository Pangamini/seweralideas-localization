// #if UNITY_EDITOR
// using UnityEditor;
// #endif
// using System;
// using System.Collections.Generic;
// using System.IO;
// using System.Threading.Tasks;
// using SeweralIdeas.Collections;
// using SeweralIdeas.Config;
// using SeweralIdeas.UnityUtils;
// using UnityEngine;
// using UnityEngine.Networking;
// using UnityEngine.Pool;
//
// namespace SeweralIdeas.Localization
// {
//     [CreateAssetMenu(menuName = "AdventureEngine/"+nameof(LocalizationManager), fileName = nameof(LocalizationManager))]
//     public class LocalizationManager : SingletonAsset<LocalizationManager>
//     {
//         [SerializeField] private StringConfigField m_language;
//         [SerializeField] private string m_streamingAssetsPath = "Languages/";
//         [SerializeField] private string m_headerFilename = "header.json";
//         [SerializeField] private string m_webGLHeader = "languages.txt";
//         
//         [NonSerialized] private StringConfigField m_subscribedLanguageVar;
//         [NonSerialized] private readonly Dictionary<string, LanguageHeader> m_langHeaders = new();
//         [NonSerialized] private string m_loadedLanguageName;
//         [NonSerialized] private LanguageHeader m_loadedLanguageHeader;
//         [NonSerialized] private LanguageData m_loadedLanguageData;
//         [NonSerialized] private bool m_initialized;
//
//         private Task m_detectingLanguagesTask = null;
//         
//         [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
//         #if UNITY_EDITOR
//         [InitializeOnLoadMethod]
//         #endif
//         private async static void Reinit()
//         {
//             var inst = GetInstance();
//             if (!await inst.EnsureInitializedAsync())
//                 await inst.DetectLanguagesAsync();
//         }
//         
//         public event Action<LanguageData> LanguageLoaded;
//         
//         public ReadonlyDictView<string, LanguageHeader> Headers => new(m_langHeaders);
//
//         private void SetSubscribedConfigValue(StringConfigField variable)
//         {
//             if(m_subscribedLanguageVar == variable)
//                 return;
//             
//             if(m_subscribedLanguageVar != null)
//             {
//                 m_subscribedLanguageVar.ValueChanged -= LoadLanguageAsync;
//             }
//             m_subscribedLanguageVar = variable;
//             if(m_subscribedLanguageVar != null)
//             {
//                 m_subscribedLanguageVar.ValueChanged += LoadLanguageAsync;
//                 LoadLanguageAsync(m_subscribedLanguageVar.Value);
//             }
//         }
//
//         
//         public async Task DetectLanguagesAsync()
//         {
//             if(m_detectingLanguagesTask == null)
//             {
//                 m_detectingLanguagesTask = DetectLanguagesInternalAsync();
//                 
//             }
//
//             try
//             {
//                 await m_detectingLanguagesTask;
//             }
//             catch( Exception e )
//             {
//                 Debug.LogException(e);
//             }
//             finally
//             {
//                 m_detectingLanguagesTask = null;
//             }
//         }
//         
//         private async Task DetectLanguagesInternalAsync()
//         {
//             m_langHeaders.Clear();
//
//             var headers = await GetHeadersAsync();
//
//             if(headers == null)
//                 return;
//             
//             foreach (var language in headers)
//             {
//                 try
//                 {
//                     LanguageHeader header = await LanguageHeader.Load(language.header);
//                     if(header == null)
//                     {
//                         Debug.LogError($"Failed to read language header \"{language.header}\"");
//                         continue;
//                     }
//
//                     m_langHeaders.Add(language.name, header);
//                 }
//                 catch( Exception e )
//                 {
//                     Debug.LogException(new Exception($"Failed to read language header \"{language.header}\"", e));
//                     continue;
//                 }
//             }
//             
//             LoadLanguageAsync(m_subscribedLanguageVar.Value);
//         }
//
//
//         public async static Task<string> LoadTextFile(string url)
//         {
//             UnityWebRequest langListReq = UnityWebRequest.Get(url);
//             langListReq.SendWebRequest();
//
//             while(!langListReq.isDone)
//             {
//                 await Task.Yield();
//             }
//
//             if(langListReq.result != UnityWebRequest.Result.Success)
//             {
//                 Debug.LogError($"Failed to load file \"{url}\" with error \"{langListReq.error}\"");
//                 return null;
//             }
//             
//             return langListReq.downloadHandler.text;
//         }
//         
//         private async Task<(string name, string header)[]> GetHeadersAsync()
//         {
//             using (ListPool<(string name, string header)>.Get(out var languages))
//             {
//                 string langRootDirPath = Path.Combine(Application.streamingAssetsPath, m_streamingAssetsPath);
//
//                 #if UNITY_WEBGL
//                 
//                 // In WebGL, a languages file must contain a list of all languages
//                 string content = await LoadTextFile(Path.Combine(langRootDirPath, m_webGLHeader));
//                 using StringReader reader = new StringReader(content);
//                 while(true)
//                 {
//                     string line = reader.ReadLine();
//                     if(line == null)
//                         break;
//
//                     string headerPath = Path.Combine(Path.Combine(langRootDirPath, line), m_headerFilename);
//                     languages.Add((line, headerPath));
//                 }
//
//                 #else
//                 
//                 // In standalone build, language list can be generated from dictionaries
//                 DirectoryInfo langRootDir = Directory.Exists(langRootDirPath) ? new DirectoryInfo(langRootDirPath) : Directory.CreateDirectory(langRootDirPath);
//                 foreach (DirectoryInfo langDir in langRootDir.EnumerateDirectories())
//                 {
//                     foreach (FileInfo headerFile in langDir.EnumerateFiles(m_headerFilename, SearchOption.TopDirectoryOnly))
//                     {
//                         languages.Add((langDir.Name, headerFile.FullName));
//                         break;
//                     }
//                 }
//                 
//                 #endif
//                 
//                 return languages.ToArray();
//             }
//         }
//
//         public void SetLanguage(string languageName)
//         {
//             m_subscribedLanguageVar.Value = languageName;
//         }
//         
//         private async void LoadLanguageAsync(string langId)
//         {
//             m_loadedLanguageData = null;
//             
//             if(!string.IsNullOrWhiteSpace(langId) && m_langHeaders.TryGetValue(langId, out m_loadedLanguageHeader))
//             {
//                 m_loadedLanguageData = await LanguageData.LoadAsync(m_loadedLanguageHeader);
//             }
//
//             m_loadedLanguageName = langId;
//
//             LanguageLoaded?.Invoke(m_loadedLanguageData);
//         }
//         
//         public string LoadedLanguageName => m_loadedLanguageName;
//         public LanguageHeader LoadedLanguageHeader => m_loadedLanguageHeader;
//         public LanguageData LoadedLanguage => m_loadedLanguageData;
//         
//         private async Task<bool> EnsureInitializedAsync()
//         {
//             if(m_initialized)
//                 return false;
//             
//             SetSubscribedConfigValue(m_language);
//             await DetectLanguagesAsync();
//             m_initialized = true;
//             return true;
//         }
//
//         private void OnDisable()
//         {
//             SetSubscribedConfigValue(null);
//         }
//
//         private void OnValidate()
//         {
//             SetSubscribedConfigValue(m_language);
//         }
//         
//     }
// }
