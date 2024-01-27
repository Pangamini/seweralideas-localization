using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SeweralIdeas.Collections;
using UnityEngine;
using UnityEngine.Pool;

namespace SeweralIdeas.Localization
{
    public class LanguageData
    {
        private LanguageHeader m_header;
        private const string DefaultTextFilename = "default.json";
        private readonly Dictionary<string, string> m_texts = new();
        private readonly Dictionary<string, string> m_audioFiles = new();
        
        public ReadonlyDictView<string, string> Texts => new(m_texts);
        public ReadonlyDictView<string, string> AudioFiles => new(m_audioFiles);

        public bool TryGetAudioUrl(string key, out string url)
        {
            url = null;
            
            if(!m_audioFiles.TryGetValue(key, out var audioFile))
                return false;

            url = Path.Combine(m_header.Directory, audioFile);
            return true;
        }
        
        public LanguageHeader Header => m_header;
        
        public void SetText(string key, string newText)
        {
            m_texts[key] = newText;
        }

        public void RemoveText(string key)
        {
            m_texts.Remove(key);
        }

        public async static Task<LanguageData> Load(LanguageHeader header)
        {
            var data = new LanguageData();
            data.m_header = header;
            
            foreach (var jsonUrl in header.EnumerateDataURLs())
            {
                await ReadDataUrl(jsonUrl, data.m_texts.Add, data.m_audioFiles.Add);
            }
            
            return data;
        }

        
        public async Task Save()
        {
            void SaveFile(string file, HashSet<string> textKeys, HashSet<string> audioKeys, Dictionary<string, string> origToTempFile)
            {

                string GetTempFile(string origFile)
                {
                    if(!origToTempFile.TryGetValue(origFile, out string tempFile))
                    {
                        tempFile = Path.GetTempFileName();
                        origToTempFile.Add(origFile, tempFile);
                    }
                    return tempFile;
                }

                void WriteHunk(string hunk, HashSet<string> keysToSave, Dictionary<string, string> data, JsonTextWriter writer)
                {
                    if(keysToSave.Count > 0)
                    {
                        writer.WritePropertyName(hunk);

                        writer.WriteToken(JsonToken.StartObject);

                        foreach (string key in keysToSave)
                        {
                            string value = data[key];
                            writer.WriteToken(JsonToken.PropertyName, key);
                            writer.WriteToken(JsonToken.String, value);
                        }

                        writer.WriteToken(JsonToken.EndObject);
                    }
                }

                {
                    string tempFilePath = GetTempFile(file);
                    using var fileStream = File.Open(tempFilePath, FileMode.Create, FileAccess.Write);
                    using var streamWriter = new StreamWriter(fileStream);
                    using var writer = new JsonTextWriter(streamWriter);
                    writer.Formatting = Formatting.Indented;
                    writer.WriteToken(JsonToken.StartObject);
                    
                    WriteHunk(HunkNameText, textKeys, m_texts, writer);
                    WriteHunk(HunkNameAudio, audioKeys, m_audioFiles, writer);
                    
                    writer.WriteToken(JsonToken.EndObject);
                }
            }

            using (DictionaryPool<string, string>.Get(out var origToTempFile))
            using (HashSetPool<string>.Get(out var unsavedTexts))
            using (HashSetPool<string>.Get(out var unsavedAudio))
            {
                string defaultFile = Path.Combine(m_header.Directory, DefaultTextFilename);


                // mark all keys as unsaved
                foreach (var pair in m_texts)
                {
                    unsavedTexts.Add(pair.Key);
                }
                
                foreach (var pair in m_audioFiles)
                {
                    unsavedAudio.Add(pair.Key);
                }
                
                // save all texts to its original locations
                foreach (string jsonFile in Header.EnumerateDataURLs())
                {
                    // skip the default file, we handle it later
                    if(jsonFile.Equals(defaultFile, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    // prepare writer
                    // string tempFilePath = GetTempFile(jsonFile);
                    // using var fileStream = File.Open(tempFilePath, FileMode.Create, FileAccess.Write);
                    // using var streamWriter = new StreamWriter(fileStream);
                    // using var writer = new JsonTextWriter(streamWriter);
                    // writer.Formatting = Formatting.Indented;
                    //
                    // writer.WriteToken(JsonToken.StartObject);
                    
                    // visit all keys in the original file, write only those, in order

                    using (HashSetPool<string>.Get(out var textKeys))
                    using (HashSetPool<string>.Get(out var audioKeys))
                    {
                        void KeyTextVisitor(string key, string _)
                        {
                            if(!unsavedTexts.Remove(key))
                                return;
                            textKeys.Add(key);
                        }
                        
                        void KeyAudioVisitor(string key, string _)
                        {
                            if(!unsavedAudio.Remove(key))
                                return;
                            audioKeys.Add(key);
                        }

                        await ReadDataUrl(jsonFile, KeyTextVisitor, KeyAudioVisitor);

                        SaveFile(jsonFile, textKeys, audioKeys, origToTempFile);
                    }
                }

                
                // save all yet unsaved keys to the Default file (or rather its temp mirror)
                SaveFile(defaultFile, unsavedTexts, unsavedAudio, origToTempFile);

                // Copy data from temp files to the target files.
                using (ListPool<(FileStream source, FileStream destination)>.Get(out var streams))
                {
                    try
                    {
                        // First, open all necessary streams to ensure atomicity
                        foreach (var pair in origToTempFile)
                        {
                            FileStream origFile = null;
                            FileStream tempFile = null;
                            try
                            {
                                origFile = new FileInfo(pair.Key).Open(FileMode.Create, FileAccess.Write);
                                tempFile = new FileInfo(pair.Value).Open(FileMode.Open, FileAccess.Read);
                            }
                            catch
                            {
                                origFile?.Close();
                                tempFile?.Close();
                                throw;
                            }
                            streams.Add((tempFile, origFile));
                        }

                        // all streams are open, do the writing
                        foreach (var pair in streams)
                        {
                            pair.source.CopyTo(pair.destination);
                        }
                    }
                    
                    catch
                    {
                        // We finished writing to temp files, but copying back somehow failed.
                        // Log the temp => original file map for recovery
                        var sb = new StringBuilder();
                        sb.AppendLine($"Unknown error during saving LanguageData at {m_header.Directory}. Data saved to temp files. Dumping temp file map:");
                        foreach (var pair in origToTempFile)
                        {
                            sb.Append(pair.Key);
                            sb.Append(" => ");
                            sb.AppendLine(pair.Value);
                        }
                        Debug.LogError(sb.ToString());
                        
                        throw;
                    }
                    
                    finally
                    {
                        foreach (var pair in streams)
                        {
                            pair.source?.Close();
                            pair.destination?.Close();
                        }
                    }

                    // if everything succeeded, delete the temp files
                    foreach (var pair in origToTempFile)
                    {
                        File.Delete(pair.Value);
                    }
                }
            }
        }

        
        private async static Task ReadDataUrl(string jsonFile, Action<string, string> keyTextVisitor, Action<string, string> keyAudioVisitor)
        {
            string text = await LocalizationManager.LoadTextFile(jsonFile);
            
            using var streamReader = new StringReader(text);
            using var reader = new JsonTextReader(streamReader);

            reader.Read();
            CheckToken(reader, JsonToken.StartObject);

            while (reader.Read())
            {
                if(reader.TokenType == JsonToken.EndObject)
                    break;
                
                CheckToken(reader, JsonToken.PropertyName);
                ReadValue(reader, out string hunkName);

                if(hunkName == HunkNameText)
                {
                    ReadHunk(keyTextVisitor, reader);
                }
                if(hunkName == HunkNameAudio)
                {
                    ReadHunk(keyAudioVisitor, reader);
                }
                else
                {
                    reader.Skip();
                }
            }
        }
        private const string HunkNameText = "text";
        private const string HunkNameAudio = "audio";

        private static void ReadHunk(Action<string, string> visitor, JsonTextReader reader)
        {
            reader.Read();
            CheckToken(reader, JsonToken.StartObject);
            while(true)
            {
                reader.Read();
                if(reader.TokenType == JsonToken.EndObject)
                    break;

                CheckToken(reader, JsonToken.PropertyName);
                ReadValue(reader, out string propertyName);
                reader.Read();
                CheckToken(reader, JsonToken.String);
                ReadValue(reader, out string propertyValue);

                visitor(propertyName, propertyValue);
            }
        }
        
        private static void ReadValue<T>(JsonTextReader reader, out T value)
        {
            if(reader.ValueType != typeof( T ))
            {
                throw new Exception($"Unexpected value type \"{reader.ValueType}\", expected \"{typeof(T)}\"");
            }
            value = (T)reader.Value;
        }
        private static void CheckToken(JsonTextReader reader, JsonToken token)
        {
            if(reader.TokenType != token)
                throw new Exception($"Unexpected token \"{reader.TokenType}\", expected \"{token}\"");
        }

    }
}
