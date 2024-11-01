using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace SeweralIdeas.Localization
{
    public static class LocalizationUtils
    {
        public async static Task<string> LoadTextFileAsync(string url)
        {
            UnityWebRequest langListReq = UnityWebRequest.Get(url);
            langListReq.SendWebRequest();
            
            while(!langListReq.isDone)
            {
                await Task.Yield();
            }
            
            if(langListReq.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Failed to load file \"{url}\" with error \"{langListReq.error}\"");
                return null;
            }
            
            return langListReq.downloadHandler.text;
        }
    }
}
