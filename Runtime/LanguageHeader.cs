using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;

namespace SeweralIdeas.Localization
{
    [Serializable]
    public class LanguageHeader
    {
        [SerializeField]      
        private string       name;
        
        [SerializeField]  
        private string       author;
        
        [SerializeField]     
        private string       icon;
        
        [SerializeField] 
        private List<string> dataFiles = new();

        private string _directory;
        private string _headerFile;
        private Sprite _sprite;
        private Texture2D _texture;

        public Texture2D Texture
        {
            get
            {
                if (_texture != null)
                    return _texture;

                if (string.IsNullOrEmpty(icon))
                    return null;
                
                string dirPath = Path.GetDirectoryName(_headerFile);
                string iconPath = Path.Combine(dirPath, icon);

                if (!File.Exists(iconPath))
                    return null;
                
                byte[] data = File.ReadAllBytes(iconPath);
                _texture = new Texture2D(2, 2);
                _texture.LoadImage(data, false);
                //header.m_texture.Compress(true);
                _sprite = Sprite.Create(_texture, new Rect(0, 0, _texture.width, _texture.height), new Vector2(0.5f, 0.5f), 1, 0, SpriteMeshType.FullRect);

                return _texture;
            }
        }

        public Sprite Sprite => _sprite;
        public string Directory => _directory;
        public string HeaderFile => _headerFile;

        public string DisplayName => name;
        public string Author => author;
        public List<string> DataFiles => dataFiles;

        public static async Task<LanguageHeader> Load(string headerFileFullName)
        {
            string json = await LocalizationUtils.LoadTextFileAsync(headerFileFullName);
            LanguageHeader header = JsonUtility.FromJson<LanguageHeader>(json);

            if(header == null)
                throw new InvalidDataException($"Failed to load language header from {headerFileFullName}");
            
            header._headerFile = headerFileFullName;
            header._directory = headerFileFullName.Substring(0, headerFileFullName.Length - Path.GetFileName(headerFileFullName).Length);

            return header;
        }

        public IEnumerable<string> EnumerateDataUrls()
        {
            foreach (var file in dataFiles)
            {
                yield return Path.Combine(_directory, file);
            }
        }
    }
}
