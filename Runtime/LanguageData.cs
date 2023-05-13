using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
        
        public ReadonlyDictView<string, string> Texts => new(m_texts);
        public LanguageHeader Header => m_header;
        
        public void SetText(string key, string newText)
        {
            m_texts[key] = newText;
        }

        public void RemoveText(string key)
        {
            m_texts.Remove(key);
        }

        private IEnumerable<FileInfo> EnumerateTextFiles()
        {
            string fullHeaderPath = Path.GetFullPath(m_header.HeaderFile);
            foreach (var jsonFile in new DirectoryInfo(m_header.Directory).EnumerateFiles("*.json", SearchOption.AllDirectories))
            {
                // skip the header file
                if(string.Equals(fullHeaderPath, jsonFile.FullName, StringComparison.OrdinalIgnoreCase))
                    continue;
                yield return jsonFile;
            }
        }
        
        public static LanguageData Load(LanguageHeader header)
        {
            var data = new LanguageData();
            data.m_header = header;
            
            foreach (var jsonFile in data.EnumerateTextFiles())
            {
                ReadTextsFile(jsonFile, data.m_texts.Add);
            }
            
            return data;
        }

        public void Save()
        {
            using (DictionaryPool<string, string>.Get(out var origToTempFile))
            using (HashSetPool<string>.Get(out var unsavedKeys))
            {
                FileInfo defaultFile = new FileInfo(Path.Combine(m_header.Directory, DefaultTextFilename));
                
                string GetTempFile(FileInfo origFile)
                {
                    if(!origToTempFile.TryGetValue(origFile.FullName, out string tempFile))
                    {
                        tempFile = Path.GetTempFileName();
                        origToTempFile.Add(origFile.FullName, tempFile);
                    }
                    return tempFile;
                }

                // mark all keys as unsaved
                foreach (var pair in m_texts)
                {
                    unsavedKeys.Add(pair.Key);
                }

                // save all texts to its original locations
                foreach (FileInfo jsonFile in EnumerateTextFiles())
                {
                    // skip the default file, we handle it later
                    if(jsonFile.FullName.Equals(defaultFile.FullName, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    // prepare writer
                    string tempFilePath = GetTempFile(jsonFile);
                    using var fileStream = File.Open(tempFilePath, FileMode.Create, FileAccess.Write);
                    using var streamWriter = new StreamWriter(fileStream);
                    using var writer = new JsonTextWriter(streamWriter);
                    writer.Formatting = Formatting.Indented;
                    
                    writer.WriteToken(JsonToken.StartObject);
                    
                    // visit all keys in the original file, write only those, in order
                    void KeyValVisitor(string key, string _)
                    {
                        if(!m_texts.TryGetValue(key, out var newValue))
                            return;

                        if(!unsavedKeys.Remove(key))
                            return;

                        // ReSharper disable AccessToDisposedClosure
                        writer.WriteToken(JsonToken.PropertyName, key);
                        writer.WriteToken(JsonToken.String, newValue);
                        // ReSharper restore AccessToDisposedClosure
                    }

                    ReadTextsFile(jsonFile, KeyValVisitor);

                    writer.WriteToken(JsonToken.EndObject);
                }

                
                // save all yet unsaved keys to the Default file (or rather its temp mirror)
                if(unsavedKeys.Count > 0)
                {
                    // prepare writer
                    string tempFilePath = GetTempFile(defaultFile);
                    using var fileStream = File.Open(tempFilePath, FileMode.Create, FileAccess.Write);
                    using var streamWriter = new StreamWriter(fileStream);
                    using var writer = new JsonTextWriter(streamWriter);
                    writer.Formatting = Formatting.Indented;
                    
                    writer.WriteToken(JsonToken.StartObject);

                    foreach (string key in unsavedKeys)
                    {
                        string value = m_texts[key];
                        writer.WriteToken(JsonToken.PropertyName, key);
                        writer.WriteToken(JsonToken.String, value);
                    }
                    
                    writer.WriteToken(JsonToken.EndObject);
                }

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

        private static void ReadTextsFile(FileInfo jsonFile, Action<string, string> keyValVisitor)
        {
            using var fileStream = jsonFile.OpenRead();
            using var streamReader = new StreamReader(fileStream);
            using var reader = new JsonTextReader(streamReader);
            
            static void CheckToken(JsonTextReader reader, JsonToken token)
            {
                if(reader.TokenType != token)
                    throw new Exception($"Unexpected token \"{reader.TokenType}\", expected \"{token}\"");
            }
            
            static void ReadValue<T>(JsonTextReader reader, out T value)
            {
                if(reader.ValueType != typeof( T ))
                {
                    throw new Exception($"Unexpected value type \"{reader.ValueType}\", expected \"{typeof(T)}\"");
                }
                value = (T)reader.Value;
            }

            reader.Read();
            CheckToken(reader, JsonToken.StartObject);

            while (reader.Read())
            {
                if(reader.TokenType == JsonToken.EndObject)
                    break;
                
                CheckToken(reader, JsonToken.PropertyName);
                ReadValue(reader, out string propertyName);
                
                reader.Read();
                CheckToken(reader, JsonToken.String);
                ReadValue(reader, out string propertyValue);
                
                keyValVisitor(propertyName, propertyValue);
            }
        }
        
    }
}
