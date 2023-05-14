using System;
using System.IO;
using UnityEngine;
namespace SeweralIdeas.Localization
{
    [Serializable]
    public class LanguageHeader
    {
        [SerializeField] private string m_name;
        [SerializeField] private string m_author;
        [SerializeField] private string m_icon;

        private string m_directory;
        private string m_headerFile;
        private Sprite m_sprite;
        private Texture2D m_texture;

        public string DisplayName => m_name;
        public string Author => m_author;
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

        public static LanguageHeader Load(string headerFileFullName)
        {
            var header = JsonUtility.FromJson<LanguageHeader>(File.ReadAllText(headerFileFullName));

            header.m_headerFile = headerFileFullName;
            header.m_directory = Path.GetDirectoryName(headerFileFullName);

            return header;
        }
    }
}
