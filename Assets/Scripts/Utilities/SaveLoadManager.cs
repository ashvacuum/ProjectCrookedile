using System;
using System.IO;
using UnityEngine;
using Crookedile.Core;

namespace Crookedile.Utilities
{
    public class SaveLoadManager : Singleton<SaveLoadManager>
    {
        private string _saveDirectory;

        protected override void OnAwake()
        {
            _saveDirectory = Application.persistentDataPath;
        }

        public void Save<T>(string fileName, T data)
        {
            if (data == null)
            {
                Debug.LogError("Cannot save null data");
                return;
            }

            try
            {
                string filePath = Path.Combine(_saveDirectory, fileName);
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(filePath, json);
                Debug.Log($"Saved to: {filePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save {fileName}: {e.Message}");
            }
        }

        public T Load<T>(string fileName)
        {
            try
            {
                string filePath = Path.Combine(_saveDirectory, fileName);

                if (!File.Exists(filePath))
                {
                    Debug.LogWarning($"Save file not found: {filePath}");
                    return default;
                }

                string json = File.ReadAllText(filePath);
                T data = JsonUtility.FromJson<T>(json);
                Debug.Log($"Loaded from: {filePath}");
                return data;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load {fileName}: {e.Message}");
                return default;
            }
        }

        public bool SaveExists(string fileName)
        {
            string filePath = Path.Combine(_saveDirectory, fileName);
            return File.Exists(filePath);
        }

        public void DeleteSave(string fileName)
        {
            try
            {
                string filePath = Path.Combine(_saveDirectory, fileName);

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    Debug.Log($"Deleted save file: {filePath}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to delete {fileName}: {e.Message}");
            }
        }

        public void DeleteAllSaves()
        {
            try
            {
                DirectoryInfo directory = new DirectoryInfo(_saveDirectory);
                FileInfo[] files = directory.GetFiles("*.json");

                foreach (FileInfo file in files)
                {
                    file.Delete();
                }

                Debug.Log("Deleted all save files");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to delete all saves: {e.Message}");
            }
        }
    }
}
