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
        
        public string Name => m_name;
        public string Author => m_author;
        public Texture2D Texture => m_texture;
        public Sprite Sprite => m_sprite;
        public string Directory => m_directory;
        public string HeaderFile => m_headerFile;

        public static LanguageHeader Load(string headerFileFullName)
        {
            var header = JsonUtility.FromJson<LanguageHeader>(File.ReadAllText(headerFileFullName));

            header.m_headerFile = headerFileFullName;
            header.m_directory = Path.GetDirectoryName(headerFileFullName);
            
            var dirPath = Path.GetDirectoryName(headerFileFullName);
            var iconPath = Path.Combine(dirPath, header.m_icon);
            
            if(File.Exists(iconPath))
            {
                byte[] data = File.ReadAllBytes(iconPath);
                header.m_texture = new Texture2D(2, 2);
                header.m_texture.LoadImage(data, false);
                //header.m_texture.Compress(true);
                header.m_sprite = Sprite.Create(header.m_texture, new Rect(0, 0, header.m_texture.width, header.m_texture.height), new Vector2(0.5f, 0.5f), 1);
            }
            
            return header;
        }
    }
}
