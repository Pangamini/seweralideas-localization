using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace SeweralIdeas.Localization
{
    [Serializable]
    public class LanguageHeader
    {
        [SerializeField] private string m_name;
        [SerializeField] private string m_author;
        [SerializeField] private string m_icon;
        [SerializeField] private List<string> m_dataFiles = new();

        private string m_directory;
        private string m_headerFile;
        private Sprite m_sprite;
        private Texture2D m_texture;

        public Texture2D Texture
        {
            get
            {
                if(m_texture == null)
                {
                    string dirPath = Path.GetDirectoryName(m_headerFile);
                    string iconPath = Path.Combine(dirPath, m_icon);

                    if(File.Exists(iconPath))
                    {
                        byte[] data = File.ReadAllBytes(iconPath);
                        m_texture = new Texture2D(2, 2);
                        m_texture.LoadImage(data, false);
                        //header.m_texture.Compress(true);
                        m_sprite = Sprite.Create(m_texture, new Rect(0, 0, m_texture.width, m_texture.height), new Vector2(0.5f, 0.5f), 1);
                    }
                }
                return m_texture;
            }
        }

        public Sprite Sprite => m_sprite;
        public string Directory => m_directory;
        public string HeaderFile => m_headerFile;

        public string DisplayName => m_name;
        public string Author => m_author;
        public List<string> DataFiles => m_dataFiles;

        public async static Task<LanguageHeader> Load(string headerFileFullName)
        {
            string json = await LocalizationManager.LoadTextFile(headerFileFullName);
            LanguageHeader header = JsonUtility.FromJson<LanguageHeader>(json);

            header.m_headerFile = headerFileFullName;
            header.m_directory = headerFileFullName.Substring(0, headerFileFullName.Length - Path.GetFileName(headerFileFullName).Length);

            return header;
        }

        public IEnumerable<string> EnumerateDataURLs()
        {
            foreach (var file in m_dataFiles)
            {
                yield return Path.Combine(m_directory, file);
            }
        }
    }
}
