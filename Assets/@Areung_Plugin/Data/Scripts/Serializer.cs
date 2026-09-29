using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace Areung_Plugin.Data.Scripts
{
    public static class Serializer
    {
        private static string _persistentDataPath;

        public static void Init()
        {
            _persistentDataPath = Application.persistentDataPath;
        }

        /// <summary>
        /// Deserializes file located at Persistent Data Path.
        /// </summary>
        /// <param name="fileName">Name of input file.</param>
        /// <returns>Deserialized object if file exists or new instance if doesn't.</returns>
        public static T Deserialize<T>(string fileName, bool logIfFileNotExists = false) where T : new()
        {
            string absolutePath = Path.Combine(GetPersistentDataPath(), fileName);

            if (FileExistsAtPath(absolutePath))
            {
                try
                {
                    using FileStream file = File.Open(absolutePath, FileMode.Open);
                    return (T)new BinaryFormatter().Deserialize(file);
                }
                catch (Exception ex)
                {
                    // 메인 파일 손상 — 직전 정상본(.bak)으로 복구 시도 후, 그것도 실패하면 새 세이브
                    Debug.LogError($"[Serializer] Deserialize 실패: {ex.Message}\n경로: {absolutePath}");
                    return DeserializeBackup<T>(absolutePath + ".bak");
                }
            }
            else
            {
                if (logIfFileNotExists)
                {
                    Debug.LogWarning("File at path : \"" + absolutePath + "\" does not exist.");
                }
                return new T();
            }
        }

        private static T DeserializeBackup<T>(string backupPath) where T : new()
        {
            if (FileExistsAtPath(backupPath))
            {
                try
                {
                    using FileStream file = File.Open(backupPath, FileMode.Open);
                    Debug.LogWarning($"[Serializer] .bak에서 복구: {backupPath}");
                    return (T)new BinaryFormatter().Deserialize(file);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Serializer] .bak 복구도 실패: {ex.Message}");
                }
            }
            return new T();
        }

        /// <summary>
        /// Serializes file to Persistent Data Path.
        /// </summary>
        /// <param name="fileName">Name of output file.</param>
        /// <param name="objectToSerialize">Reference to object that should be serialized.</param>
        public static void Serialize<T>(T objectToSerialize, string fileName)
        {
            string path = Path.Combine(GetPersistentDataPath(), fileName);
            string tmpPath = path + ".tmp";
            try
            {
                using (FileStream file = File.Open(tmpPath, FileMode.Create))
                {
                    new BinaryFormatter().Serialize(file, objectToSerialize);
                }

                if (File.Exists(path))
                    File.Replace(tmpPath, path, path + ".bak");
                else
                    File.Move(tmpPath, path);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Serializer] Serialize 실패: {ex.Message}\n경로: {path}");
                try { if (File.Exists(tmpPath)) File.Delete(tmpPath); } catch { /* 임시파일 정리 실패는 무시 */ }
            }
        }

        /// <summary>
        /// Checks if file exists at Persistent Data Path.
        /// </summary>
        /// <param name="fileName">Name of file to check.</param>
        /// <returns>True if file exists ans false otherwise.</returns>
        public static bool FileExistsAtPDP(string fileName)
        {
            return File.Exists(Path.Combine(GetPersistentDataPath(), fileName));
        }

        /// <summary>
        /// Checks if file exists at Persistent Data Path.
        /// </summary>
        /// <param name="absolutePath">Absolute path to file(including file name and extention.</param>
        /// <returns>True if file exists ans false otherwise.</returns>
        public static bool FileExistsAtPath(string absolutePath)
        {
            return File.Exists(absolutePath);
        }

        /// <summary>
        /// Checks if file exists add specified directory.
        /// </summary>
        /// <param name="directoryPath">Full path to directory. Ends with directory name (without "/").</param>
        /// <param name="fileName">Name of file to check.</param>
        /// <returns>True if file exists and false otherwise.</returns>
        public static bool FileExistsAtPath(string directoryPath, string fileName)
        {
            return File.Exists(Path.Combine(directoryPath, fileName));
        }

        /// <summary>
        /// Delete file at Persistent Data Path.
        /// </summary>
        /// <param name="fileName">Name of file to check.</param>
        public static void DeleteFileAtPDP(string fileName)
        {
            File.Delete(Path.Combine(GetPersistentDataPath(), fileName));
        }

        /// <summary>
        /// Delete file at specified path.
        /// </summary>
        /// <param name="absolutePath">Absolute path to file(including file name and extention.</param>
        public static void DeleteFileAtPath(string absolutePath)
        {
            File.Delete(absolutePath);
        }

        /// <summary>
        /// Delete file at specified path.
        /// </summary>
        /// <param name="fileName">Name of file to check.</param>
        /// <param name="directoryPath">Full path to directory. Ends with directory name (without "/").</param>
        public static void DeleteFileAtPath(string directoryPath, string fileName)
        {
            File.Delete(Path.Combine(directoryPath, fileName));
        }

        private static string GetPersistentDataPath()
        {
            if (string.IsNullOrEmpty(_persistentDataPath))
            {
                _persistentDataPath = Application.persistentDataPath;

                return _persistentDataPath;
            }

            return _persistentDataPath;
        }
    }
}