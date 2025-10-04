using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using Crookedile.Data;
using Crookedile.Core;
using Crookedile.Utilities;

namespace Crookedile.Managers
{
    /// <summary>
    /// Handles saving and loading game data with encryption.
    /// Saves to Application.persistentDataPath for cross-platform compatibility.
    /// </summary>
    [Debuggable("SaveManager", LogLevel.Info)]
    public class SaveManager : Singleton<SaveManager>
    {
        [Header("Save Settings")]
        [SerializeField] private bool _useEncryption = true;
        [SerializeField] private bool _usePrettyPrint = false;

        private const int MAX_SAVE_SLOTS = 3;
        private const string SAVE_FILE_PREFIX = "save_slot_";
        private const string AUTOSAVE_FILE_PREFIX = "autosave_slot_";
        private const string SAVE_FILE_EXTENSION = ".dat";

        // Encryption key - in production, this should be obfuscated or derived from device info
        private const string ENCRYPTION_KEY = "CrookedileGame2025SecretKey123"; // 32 chars for AES-256
        private const string ENCRYPTION_IV = "1234567890123456"; // 16 chars for AES

        private int _currentSlot = 0;

        private string GetSaveFilePath(int slot, bool isAutoSave = false)
        {
            if (slot < 0 || slot >= MAX_SAVE_SLOTS)
            {
                GameLogger.LogError("SaveManager", $"Invalid save slot: {slot}. Must be 0-{MAX_SAVE_SLOTS - 1}");
                slot = 0;
            }
            string prefix = isAutoSave ? AUTOSAVE_FILE_PREFIX : SAVE_FILE_PREFIX;
            return Path.Combine(Application.persistentDataPath, $"{prefix}{slot}{SAVE_FILE_EXTENSION}");
        }

        private string SaveFilePath => GetSaveFilePath(_currentSlot);
        private string AutoSaveFilePath => GetSaveFilePath(_currentSlot, isAutoSave: true);

        #region Save Slot Management

        /// <summary>
        /// Sets the active save slot (0-2).
        /// </summary>
        public bool SetActiveSlot(int slot)
        {
            if (slot < 0 || slot >= MAX_SAVE_SLOTS)
            {
                GameLogger.LogError("SaveManager", $"Invalid slot: {slot}. Must be 0-{MAX_SAVE_SLOTS - 1}");
                return false;
            }

            _currentSlot = slot;
            GameLogger.LogInfo("SaveManager", $"Active save slot set to: {slot}");
            return true;
        }

        /// <summary>
        /// Gets the currently active save slot.
        /// </summary>
        public int GetActiveSlot()
        {
            return _currentSlot;
        }

        /// <summary>
        /// Checks if a specific save slot has data.
        /// </summary>
        public bool SlotHasSave(int slot)
        {
            if (slot < 0 || slot >= MAX_SAVE_SLOTS)
                return false;

            return File.Exists(GetSaveFilePath(slot));
        }

        /// <summary>
        /// Gets info about all save slots.
        /// </summary>
        public SaveSlotInfo[] GetAllSlotInfo()
        {
            SaveSlotInfo[] slots = new SaveSlotInfo[MAX_SAVE_SLOTS];

            for (int i = 0; i < MAX_SAVE_SLOTS; i++)
            {
                slots[i] = GetSlotInfo(i);
            }

            return slots;
        }

        /// <summary>
        /// Gets info about a specific save slot.
        /// </summary>
        public SaveSlotInfo GetSlotInfo(int slot)
        {
            SaveSlotInfo info = new SaveSlotInfo
            {
                slotIndex = slot,
                isEmpty = !SlotHasSave(slot)
            };

            if (!info.isEmpty)
            {
                try
                {
                    // Load just to get metadata
                    int previousSlot = _currentSlot;
                    _currentSlot = slot;
                    SaveData data = LoadGame();
                    _currentSlot = previousSlot;

                    if (data != null)
                    {
                        info.origin = data.origin;
                        info.currentDay = data.currentDay;
                        info.lastSaveTime = data.lastSaveTime;
                        info.totalPlaytime = data.totalPlaytime;
                    }
                }
                catch (Exception e)
                {
                    GameLogger.LogError("SaveManager", $"Failed to read slot {slot} info: {e.Message}");
                    info.isCorrupted = true;
                }
            }

            return info;
        }

        /// <summary>
        /// Deletes a specific save slot.
        /// </summary>
        public bool DeleteSlot(int slot)
        {
            try
            {
                string path = GetSaveFilePath(slot);
                if (File.Exists(path))
                {
                    File.Delete(path);
                    GameLogger.LogInfo("SaveManager", $"Deleted save slot {slot}");
                    return true;
                }
                return false;
            }
            catch (Exception e)
            {
                GameLogger.LogError("SaveManager", $"Failed to delete slot {slot}: {e.Message}");
                return false;
            }
        }

        #endregion

        #region Save Operations

        /// <summary>
        /// Saves the game data to the current active slot.
        /// </summary>
        /// <param name="saveData">Data to save</param>
        /// <param name="isAutoSave">If true, saves to hidden autosave file instead</param>
        /// <returns>True if save was successful</returns>
        public bool SaveGame(SaveData saveData, bool isAutoSave = false)
        {
            try
            {
                // Update save metadata
                saveData.UpdateSaveTime();

                // Convert to JSON
                string json = JsonUtility.ToJson(saveData, _usePrettyPrint);
                GameLogger.LogInfo("SaveManager", $"Serialized save data: {json.Length} characters");

                // Encrypt if enabled
                string dataToWrite = _useEncryption ? Encrypt(json) : json;

                // Write to file
                string filePath = isAutoSave ? AutoSaveFilePath : SaveFilePath;
                File.WriteAllText(filePath, dataToWrite);

                string saveType = isAutoSave ? "Auto-saved" : "Saved";
                GameLogger.LogInfo("SaveManager", $"{saveType} successfully to: {filePath}");
                return true;
            }
            catch (Exception e)
            {
                GameLogger.LogError("SaveManager", $"Failed to save game: {e.Message}\n{e.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Saves the game with validation - creates backup, saves, validates, then commits.
        /// </summary>
        private bool SaveGameWithValidation(SaveData saveData, bool isAutoSave = false)
        {
            try
            {
                string filePath = isAutoSave ? AutoSaveFilePath : SaveFilePath;
                string backupPath = filePath + ".backup";
                string tempPath = filePath + ".temp";

                // 1. Backup existing save if it exists
                if (File.Exists(filePath))
                {
                    File.Copy(filePath, backupPath, overwrite: true);
                }

                // 2. Update save metadata
                saveData.UpdateSaveTime();

                // 3. Write to temporary file first
                string json = JsonUtility.ToJson(saveData, _usePrettyPrint);
                string dataToWrite = _useEncryption ? Encrypt(json) : json;
                File.WriteAllText(tempPath, dataToWrite);

                // 4. Validate the temporary file by trying to read it back
                string validationData = File.ReadAllText(tempPath);
                string validationJson = _useEncryption ? Decrypt(validationData) : validationData;
                SaveData validatedData = JsonUtility.FromJson<SaveData>(validationJson);

                if (validatedData == null)
                {
                    throw new Exception("Validation failed: Could not deserialize saved data");
                }

                // 5. Validation successful - move temp to final location
                File.Copy(tempPath, filePath, overwrite: true);
                File.Delete(tempPath);

                string saveType = isAutoSave ? "Auto-saved" : "Saved";
                GameLogger.LogInfo("SaveManager", $"{saveType} with validation to: {filePath}");
                return true;
            }
            catch (Exception e)
            {
                GameLogger.LogError("SaveManager", $"Save with validation failed: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Loads the game data from disk.
        /// </summary>
        /// <param name="tryAutoSave">If true, tries to load autosave if main save fails</param>
        /// <returns>Loaded save data, or null if failed</returns>
        public SaveData LoadGame(bool tryAutoSave = true)
        {
            try
            {
                if (!File.Exists(SaveFilePath))
                {
                    GameLogger.LogWarning("SaveManager", "Save file does not exist");

                    // Try autosave as fallback
                    if (tryAutoSave && File.Exists(AutoSaveFilePath))
                    {
                        GameLogger.LogInfo("SaveManager", "Attempting to load autosave...");
                        return LoadAutoSave();
                    }

                    return null;
                }

                // Read from file
                string dataFromFile = File.ReadAllText(SaveFilePath);

                // Decrypt if enabled
                string json = _useEncryption ? Decrypt(dataFromFile) : dataFromFile;

                // Deserialize
                SaveData saveData = JsonUtility.FromJson<SaveData>(json);

                GameLogger.LogInfo("SaveManager", $"Game loaded successfully from: {SaveFilePath}");
                return saveData;
            }
            catch (Exception e)
            {
                GameLogger.LogError("SaveManager", $"Failed to load game: {e.Message}\n{e.StackTrace}");

                // Try autosave as fallback
                if (tryAutoSave)
                {
                    GameLogger.LogWarning("SaveManager", "Main save corrupted, trying autosave...");
                    return LoadAutoSave();
                }

                return null;
            }
        }

        /// <summary>
        /// Loads the autosave for the current slot.
        /// </summary>
        private SaveData LoadAutoSave()
        {
            try
            {
                if (!File.Exists(AutoSaveFilePath))
                {
                    GameLogger.LogWarning("SaveManager", "Autosave file does not exist");
                    return null;
                }

                string dataFromFile = File.ReadAllText(AutoSaveFilePath);
                string json = _useEncryption ? Decrypt(dataFromFile) : dataFromFile;
                SaveData saveData = JsonUtility.FromJson<SaveData>(json);

                GameLogger.LogInfo("SaveManager", $"Autosave loaded from: {AutoSaveFilePath}");
                return saveData;
            }
            catch (Exception e)
            {
                GameLogger.LogError("SaveManager", $"Failed to load autosave: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Checks if a save file exists.
        /// </summary>
        public bool SaveExists()
        {
            return File.Exists(SaveFilePath);
        }

        /// <summary>
        /// Deletes the save file.
        /// </summary>
        public bool DeleteSave()
        {
            try
            {
                if (File.Exists(SaveFilePath))
                {
                    File.Delete(SaveFilePath);
                    GameLogger.LogInfo("SaveManager", "Save file deleted");
                    return true;
                }
                return false;
            }
            catch (Exception e)
            {
                GameLogger.LogError("SaveManager", $"Failed to delete save: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Creates a backup of the current save file.
        /// </summary>
        public bool BackupSave()
        {
            try
            {
                if (!File.Exists(SaveFilePath))
                {
                    GameLogger.LogWarning("SaveManager", "No save file to backup");
                    return false;
                }

                string backupPath = SaveFilePath + ".backup";
                File.Copy(SaveFilePath, backupPath, overwrite: true);

                GameLogger.LogInfo("SaveManager", $"Save backed up to: {backupPath}");
                return true;
            }
            catch (Exception e)
            {
                GameLogger.LogError("SaveManager", $"Failed to backup save: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Restores save from backup.
        /// </summary>
        public bool RestoreBackup()
        {
            try
            {
                string backupPath = SaveFilePath + ".backup";
                if (!File.Exists(backupPath))
                {
                    GameLogger.LogWarning("SaveManager", "No backup file found");
                    return false;
                }

                File.Copy(backupPath, SaveFilePath, overwrite: true);

                GameLogger.LogInfo("SaveManager", "Save restored from backup");
                return true;
            }
            catch (Exception e)
            {
                GameLogger.LogError("SaveManager", $"Failed to restore backup: {e.Message}");
                return false;
            }
        }

        #endregion

        #region Encryption

        /// <summary>
        /// Encrypts a string using AES-256.
        /// </summary>
        private string Encrypt(string plainText)
        {
            try
            {
                using (Aes aes = Aes.Create())
                {
                    aes.Key = Encoding.UTF8.GetBytes(ENCRYPTION_KEY);
                    aes.IV = Encoding.UTF8.GetBytes(ENCRYPTION_IV);

                    ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                    using (MemoryStream msEncrypt = new MemoryStream())
                    {
                        using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                        {
                            using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                            {
                                swEncrypt.Write(plainText);
                            }
                        }
                        return Convert.ToBase64String(msEncrypt.ToArray());
                    }
                }
            }
            catch (Exception e)
            {
                GameLogger.LogError("SaveManager", $"Encryption failed: {e.Message}");
                return plainText; // Fallback to unencrypted
            }
        }

        /// <summary>
        /// Decrypts a string using AES-256.
        /// </summary>
        private string Decrypt(string cipherText)
        {
            try
            {
                using (Aes aes = Aes.Create())
                {
                    aes.Key = Encoding.UTF8.GetBytes(ENCRYPTION_KEY);
                    aes.IV = Encoding.UTF8.GetBytes(ENCRYPTION_IV);

                    ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                    using (MemoryStream msDecrypt = new MemoryStream(Convert.FromBase64String(cipherText)))
                    {
                        using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                        {
                            using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                            {
                                return srDecrypt.ReadToEnd();
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                GameLogger.LogError("SaveManager", $"Decryption failed: {e.Message}");
                throw; // Re-throw to indicate corruption
            }
        }

        #endregion

        #region Auto Save

        private float _autoSaveTimer = 0f;

        [Header("Auto Save")]
        [SerializeField] private bool _enableAutoSave = true;
        [SerializeField] private float _autoSaveIntervalSeconds = 300f;
        [SerializeField] private int _maxSaveRetries = 3;
        [SerializeField] private float _retryDelaySeconds = 1f;

        private SaveData _currentSaveData;
        private bool _isSaving = false;
        private Queue<SaveRequest> _saveQueue = new Queue<SaveRequest>();

        /// <summary>
        /// Represents a queued save request.
        /// </summary>
        private class SaveRequest
        {
            public SaveData data;
            public bool isAutoSave;
            public int retryCount;
            public float retryTimer;

            public SaveRequest(SaveData saveData, bool autoSave)
            {
                data = saveData;
                isAutoSave = autoSave;
                retryCount = 0;
                retryTimer = 0f;
            }
        }

        /// <summary>
        /// Sets the current save data for auto-saving.
        /// </summary>
        public void SetCurrentSaveData(SaveData saveData)
        {
            _currentSaveData = saveData;
        }

        /// <summary>
        /// Queues a save operation.
        /// </summary>
        public void QueueSave(SaveData saveData, bool isAutoSave = false)
        {
            _saveQueue.Enqueue(new SaveRequest(saveData, isAutoSave));
            GameLogger.LogInfo("SaveManager", $"Save queued ({(_saveQueue.Count)} in queue)");
        }

        private void Update()
        {
            // Auto-save timer
            if (_enableAutoSave && _currentSaveData != null)
            {
                _autoSaveTimer += Time.deltaTime;

                if (_autoSaveTimer >= _autoSaveIntervalSeconds)
                {
                    _autoSaveTimer = 0f;

                    // Only auto-save if data is dirty
                    if (_currentSaveData.IsDirty())
                    {
                        QueueSave(_currentSaveData, isAutoSave: true);
                        GameLogger.LogInfo("SaveManager", "Auto-save triggered (dirty data)");
                    }
                }
            }

            // Process save queue
            ProcessSaveQueue();
        }

        /// <summary>
        /// Processes the save queue, handling one save at a time with retry logic.
        /// </summary>
        private void ProcessSaveQueue()
        {
            if (_isSaving || _saveQueue.Count == 0)
                return;

            SaveRequest request = _saveQueue.Peek();

            // Handle retry delay
            if (request.retryCount > 0)
            {
                request.retryTimer += Time.deltaTime;
                if (request.retryTimer < _retryDelaySeconds)
                    return;

                request.retryTimer = 0f;
            }

            _isSaving = true;

            // Attempt save with validation
            bool success = SaveGameWithValidation(request.data, request.isAutoSave);

            if (success)
            {
                // Save successful - clear dirty flag and remove from queue
                request.data.ClearDirty();
                _saveQueue.Dequeue();
                GameLogger.LogInfo("SaveManager", $"Save completed ({_saveQueue.Count} remaining in queue)");
            }
            else
            {
                // Save failed - check retry count
                request.retryCount++;
                if (request.retryCount >= _maxSaveRetries)
                {
                    GameLogger.LogError("SaveManager", $"Save failed after {_maxSaveRetries} retries - discarding");
                    _saveQueue.Dequeue();
                }
                else
                {
                    GameLogger.LogWarning("SaveManager", $"Save failed - retry {request.retryCount}/{_maxSaveRetries} in {_retryDelaySeconds}s");
                }
            }

            _isSaving = false;
        }

        #endregion

        #region Utility

        /// <summary>
        /// Gets the save file path for debugging.
        /// </summary>
        public string GetSaveFilePath()
        {
            return SaveFilePath;
        }

        /// <summary>
        /// Gets the size of the save file in bytes.
        /// </summary>
        public long GetSaveFileSize()
        {
            if (!File.Exists(SaveFilePath))
                return 0;

            FileInfo fileInfo = new FileInfo(SaveFilePath);
            return fileInfo.Length;
        }

        /// <summary>
        /// Gets the last modified time of the save file.
        /// </summary>
        public DateTime? GetLastSaveTime()
        {
            if (!File.Exists(SaveFilePath))
                return null;

            FileInfo fileInfo = new FileInfo(SaveFilePath);
            return fileInfo.LastWriteTime;
        }

        #endregion

        #region Cheat Commands

        /// <summary>
        /// [CHEAT] Deletes ALL save files.
        /// </summary>
        [CheatCommand("clearsaves", "Deletes all save files")]
        public void CheatClearAllSaves()
        {
            for (int i = 0; i < MAX_SAVE_SLOTS; i++)
            {
                DeleteSlot(i);
            }
            GameLogger.LogWarning("SaveManager", "All save files deleted!");
        }

        /// <summary>
        /// [CHEAT] Deletes the current active slot.
        /// </summary>
        [CheatCommand("clearslot", "Deletes current save slot")]
        public void CheatClearCurrentSlot()
        {
            DeleteSlot(_currentSlot);
            GameLogger.LogWarning("SaveManager", $"Save slot {_currentSlot} deleted!");
        }

        /// <summary>
        /// [CHEAT] Opens the save folder in file explorer.
        /// </summary>
        [CheatCommand("opensavefolder", "Opens persistent data path")]
        public void CheatOpenSaveFolder()
        {
#if UNITY_EDITOR
            UnityEditor.EditorUtility.RevealInFinder(Application.persistentDataPath);
#else
            Application.OpenURL(Application.persistentDataPath);
#endif
            GameLogger.LogInfo("SaveManager", $"Save folder: {Application.persistentDataPath}");
        }

        /// <summary>
        /// [CHEAT] Lists all save slots and their status.
        /// </summary>
        [CheatCommand("listsaves", "Lists all save slots")]
        public void CheatListSaves()
        {
            SaveSlotInfo[] slots = GetAllSlotInfo();
            for (int i = 0; i < slots.Length; i++)
            {
                SaveSlotInfo slot = slots[i];
                if (slot.isEmpty)
                {
                    Debug.Log($"Slot {i}: EMPTY");
                }
                else if (slot.isCorrupted)
                {
                    Debug.Log($"Slot {i}: CORRUPTED");
                }
                else
                {
                    Debug.Log($"Slot {i}: {slot.origin} - Day {slot.currentDay} - {slot.lastSaveTime}");
                }
            }
        }

        #endregion

#if UNITY_EDITOR
        [Header("Debug")]
        [SerializeField] private bool _showDebugInfo = false;

        private void OnGUI()
        {
            if (!_showDebugInfo)
                return;

            GUILayout.BeginArea(new Rect(10, 10, 400, 200));
            GUILayout.Label($"Save Path: {SaveFilePath}");
            GUILayout.Label($"Save Exists: {SaveExists()}");
            GUILayout.Label($"File Size: {GetSaveFileSize()} bytes");
            GUILayout.Label($"Last Save: {GetLastSaveTime()?.ToString() ?? "Never"}");
            GUILayout.Label($"Encryption: {(_useEncryption ? "Enabled" : "Disabled")}");
            GUILayout.EndArea();
        }
#endif
    }

    /// <summary>
    /// Information about a save slot for display in UI.
    /// </summary>
    [Serializable]
    public class SaveSlotInfo
    {
        public int slotIndex;
        public bool isEmpty;
        public bool isCorrupted;
        public OriginType origin;
        public int currentDay;
        public string lastSaveTime;
        public float totalPlaytime;

        public string GetDisplayText()
        {
            if (isEmpty)
                return "Empty Slot";

            if (isCorrupted)
                return "Corrupted Save";

            int hours = Mathf.FloorToInt(totalPlaytime / 3600f);
            int minutes = Mathf.FloorToInt((totalPlaytime % 3600f) / 60f);

            return $"{origin} - Day {currentDay}\n{hours}h {minutes}m - {lastSaveTime}";
        }
    }
}
