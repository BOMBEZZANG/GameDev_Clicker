using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using GameDevClicker.Core.Patterns;

namespace GameDevClicker.Core.Managers
{
    public class SaveManager : Singleton<SaveManager>
    {
        private const string SAVE_KEY = "GameDevClickerSaveData";
        private const string BACKUP_SAVE_KEY = "GameDevClickerSaveDataBackup";
        private const int SAVE_VERSION = 2;

        [Header("Save Settings")]
        [SerializeField] private bool _useEncryption = false;
        [SerializeField] private bool _useCompression = false;
        [SerializeField] private bool _createBackups = true;

        private GameData _currentGameData;
        private bool _isDirty = false;

        public GameData CurrentGameData => _currentGameData;
        public bool HasUnsavedChanges => _isDirty;

        public event Action OnSaveStarted;
        public event Action OnSaveCompleted;
        public event Action OnLoadStarted;
        public event Action OnLoadCompleted;
        public event Action<string> OnSaveError;
        public event Action<string> OnLoadError;

        protected override void Awake()
        {
            base.Awake();
            InitializeSaveSystem();
        }

        private void InitializeSaveSystem()
        {
            _currentGameData = new GameData();
            _currentGameData.saveVersion = SAVE_VERSION;
            _currentGameData.firstPlayTime = DateTime.Now;
        }

        public bool HasSavedGame()
        {
            return PlayerPrefs.HasKey(SAVE_KEY);
        }

        public void SaveGame()
        {
            try
            {
                OnSaveStarted?.Invoke();

                if (_createBackups && HasSavedGame())
                {
                    CreateBackup();
                }

                _currentGameData.lastSaveTime = DateTime.Now;
                _currentGameData.totalPlayTime += (float)(DateTime.Now - _currentGameData.lastPlayTime).TotalSeconds;
                _currentGameData.saveCount++;

                string jsonData = JsonUtility.ToJson(_currentGameData, true);

                if (_useCompression)
                {
                    jsonData = CompressString(jsonData);
                }

                if (_useEncryption)
                {
                    jsonData = EncryptString(jsonData);
                }

                PlayerPrefs.SetString(SAVE_KEY, jsonData);
                PlayerPrefs.Save();

                _isDirty = false;
                OnSaveCompleted?.Invoke();
                GameEvents.InvokeGameSaved();

                Debug.Log($"[SaveManager] Game saved successfully. Save count: {_currentGameData.saveCount}");
            }
            catch (Exception e)
            {
                string errorMessage = $"Failed to save game: {e.Message}";
                Debug.LogError($"[SaveManager] {errorMessage}");
                OnSaveError?.Invoke(errorMessage);
            }
        }

        public void LoadGame()
        {
            try
            {
                LoadGameFromKey(SAVE_KEY);
                Debug.Log($"[SaveManager] Game loaded successfully. Save version: {_currentGameData.saveVersion}");
            }
            catch (Exception e)
            {
                string errorMessage = $"Failed to load game: {e.Message}";
                Debug.LogError($"[SaveManager] {errorMessage}");
                OnLoadError?.Invoke(errorMessage);

                if (_createBackups && PlayerPrefs.HasKey(BACKUP_SAVE_KEY))
                {
                    Debug.Log("[SaveManager] Attempting to load from backup...");
                    LoadFromBackup();
                }
            }
        }

        private void CreateBackup()
        {
            string currentSave = PlayerPrefs.GetString(SAVE_KEY);
            PlayerPrefs.SetString(BACKUP_SAVE_KEY, currentSave);
            Debug.Log("[SaveManager] Backup created");
        }

        private void LoadFromBackup()
        {
            try
            {
                LoadGameFromKey(BACKUP_SAVE_KEY);
                Debug.Log("[SaveManager] Successfully loaded from backup");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Failed to load from backup: {e.Message}");
            }
        }

        private void LoadGameFromKey(string saveKey)
        {
            OnLoadStarted?.Invoke();

            if (!PlayerPrefs.HasKey(saveKey))
            {
                Debug.LogWarning($"[SaveManager] No save file found at key: {saveKey}. Creating new game data.");
                _currentGameData = new GameData();
                _currentGameData.saveVersion = SAVE_VERSION;
                _currentGameData.firstPlayTime = DateTime.Now;
                OnLoadCompleted?.Invoke();
                return;
            }

            string jsonData = PlayerPrefs.GetString(saveKey);

            if (_useEncryption)
            {
                jsonData = DecryptString(jsonData);
            }

            if (_useCompression)
            {
                jsonData = DecompressString(jsonData);
            }

            GameData loadedData = JsonUtility.FromJson<GameData>(jsonData);

            if (loadedData == null)
            {
                throw new Exception("Failed to deserialize save data");
            }

            if (loadedData.saveVersion != SAVE_VERSION)
            {
                loadedData = MigrateSaveData(loadedData);
            }

            _currentGameData = loadedData;
            _currentGameData.lastPlayTime = DateTime.Now;

            _isDirty = false;
            OnLoadCompleted?.Invoke();
            GameEvents.InvokeGameLoaded();
        }

        public void DeleteSave()
        {
            PlayerPrefs.DeleteKey(SAVE_KEY);
            PlayerPrefs.DeleteKey(BACKUP_SAVE_KEY);
            PlayerPrefs.Save();

            _currentGameData = new GameData();
            _currentGameData.saveVersion = SAVE_VERSION;
            _currentGameData.firstPlayTime = DateTime.Now;
            _isDirty = false;

            Debug.Log("[SaveManager] Save data deleted");
        }

        public void MarkDirty()
        {
            _isDirty = true;
        }

        private GameData MigrateSaveData(GameData oldData)
        {
            Debug.Log($"[SaveManager] Migrating save data from version {oldData.saveVersion} to {SAVE_VERSION}");
            
            GameData newData = new GameData();
            
            // Migrate common fields
            newData.money = oldData.money;
            newData.experience = oldData.experience;
            newData.currentStage = oldData.currentStage;
            newData.firstPlayTime = oldData.firstPlayTime;
            newData.lastSaveTime = oldData.lastSaveTime;
            newData.lastPlayTime = oldData.lastPlayTime;
            newData.totalPlayTime = oldData.totalPlayTime;
            newData.saveCount = oldData.saveCount;
            newData.totalClicks = oldData.totalClicks;
            newData.totalMoneyEarned = oldData.totalMoneyEarned;
            newData.totalExperienceEarned = oldData.totalExperienceEarned;
            
            // Migrate upgrade levels if they exist
            if (oldData.upgradeLevels != null)
            {
                newData.upgradeLevels = new Dictionary<string, int>(oldData.upgradeLevels);
            }
            
            // Set default values for new V2 fields
            newData.moneyPerClick = 0f; // Will be unlocked at level 10
            newData.expPerClick = 1f;
            newData.autoMoney = 0f;
            newData.autoExp = 0f;
            
            // Calculate player level from experience
            newData.playerLevel = CalculatePlayerLevel(newData.experience);
            
            // Initialize multipliers
            newData.multipliers = new Dictionary<string, float>
            {
                {"money", 1f},
                {"exp", 1f},
                {"all", 1f}
            };
            
            // Initialize unlocked features based on current progress
            newData.unlockedFeatures = new HashSet<string>();
            if (newData.playerLevel >= 10)
            {
                newData.unlockedFeatures.Add("money");
            }
            if (newData.currentStage >= 2)
            {
                newData.unlockedFeatures.Add("project_system");
            }
            
            // Initialize new statistics
            newData.totalUpgradesPurchased = 0;
            newData.totalProjectsCompleted = 0;
            newData.totalAutoIncome = 0f;
            
            // Migrate version-specific data
            switch (oldData.saveVersion)
            {
                case 1:
                    // Migration from version 1 to 2
                    MigrateFromV1ToV2(oldData, newData);
                    break;
                    
                default:
                    Debug.LogWarning($"[SaveManager] Unknown save version {oldData.saveVersion}, using defaults");
                    break;
            }
            
            newData.saveVersion = SAVE_VERSION;
            return newData;
        }
        
        private void MigrateFromV1ToV2(GameData oldData, GameData newData)
        {
            // In V1, we had clickPower and autoIncome as single values
            // In V2, we have separate money and exp click values
            
            // Try to get old values via reflection if they exist
            var oldType = oldData.GetType();
            
            var clickPowerField = oldType.GetField("clickPower");
            if (clickPowerField != null && clickPowerField.FieldType == typeof(float))
            {
                float oldClickPower = (float)clickPowerField.GetValue(oldData);
                newData.expPerClick = Mathf.Max(1f, oldClickPower);
                
                // Money per click starts at 0 unless already unlocked
                if (newData.playerLevel >= 10)
                {
                    newData.moneyPerClick = oldClickPower * 0.5f; // 50% of exp click power
                }
            }
            
            var autoIncomeField = oldType.GetField("autoIncome");
            if (autoIncomeField != null && autoIncomeField.FieldType == typeof(float))
            {
                float oldAutoIncome = (float)autoIncomeField.GetValue(oldData);
                newData.autoExp = oldAutoIncome;
                
                // Auto money starts at 0 unless already unlocked
                if (newData.playerLevel >= 10)
                {
                    newData.autoMoney = oldAutoIncome * 0.3f; // 30% of exp auto income
                }
            }
            
            Debug.Log($"[SaveManager] Migrated from V1: ExpPerClick={newData.expPerClick}, MoneyPerClick={newData.moneyPerClick}");
        }
        
        private int CalculatePlayerLevel(long experience)
        {
            if (experience <= 0) return 1;
            
            const long baseExpPerLevel = 100;
            const float expLevelMultiplier = 1.5f;
            
            int level = 1;
            long totalExpRequired = 0;
            
            while (totalExpRequired + (long)(baseExpPerLevel * Mathf.Pow(expLevelMultiplier, level - 1)) <= experience)
            {
                totalExpRequired += (long)(baseExpPerLevel * Mathf.Pow(expLevelMultiplier, level - 1));
                level++;
            }
            
            return level;
        }

        private string CompressString(string text)
        {
            return text;
        }

        private string DecompressString(string compressedText)
        {
            return compressedText;
        }

        private string EncryptString(string text)
        {
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(text));
        }

        private string DecryptString(string encryptedText)
        {
            return System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encryptedText));
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && _isDirty && GameManager.Instance.CurrentState == GameState.Playing)
            {
                SaveGame();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && _isDirty && GameManager.Instance.CurrentState == GameState.Playing)
            {
                SaveGame();
            }
        }
    }

    [System.Serializable]
    public class GameData
    {
        public int saveVersion;
        
        // Dual Currency
        public long money;
        public long experience;
        
        // Click Values
        public float moneyPerClick;
        public float expPerClick;
        
        // Auto Income
        public float autoMoney;
        public float autoExp;
        
        // Progression
        public int currentStage;
        public int playerLevel;
        public Dictionary<string, int> upgradeLevels;
        public Dictionary<string, float> multipliers;
        public HashSet<string> unlockedFeatures;
        
        // Meta Data
        public DateTime firstPlayTime;
        public DateTime lastSaveTime;
        public DateTime lastPlayTime;
        public float totalPlayTime;
        public int saveCount;
        public long totalClicks;
        public long totalMoneyEarned;
        public long totalExperienceEarned;
        
        // Statistics
        public long totalUpgradesPurchased;
        public long totalProjectsCompleted;
        public float totalAutoIncome;
        
        public GameData()
        {
            saveVersion = 2;
            
            // Dual Currency
            money = 0;
            experience = 0;
            
            // Click Values
            moneyPerClick = 0f;
            expPerClick = 1f;
            
            // Auto Income
            autoMoney = 0f;
            autoExp = 0f;
            
            // Progression
            currentStage = 1;
            playerLevel = 1;
            upgradeLevels = new Dictionary<string, int>();
            multipliers = new Dictionary<string, float>
            {
                {"money", 1f},
                {"exp", 1f},
                {"all", 1f}
            };
            unlockedFeatures = new HashSet<string>();
            
            // Meta Data
            firstPlayTime = DateTime.Now;
            lastSaveTime = DateTime.Now;
            lastPlayTime = DateTime.Now;
            totalPlayTime = 0f;
            saveCount = 0;
            totalClicks = 0;
            totalMoneyEarned = 0;
            totalExperienceEarned = 0;
            
            // Statistics
            totalUpgradesPurchased = 0;
            totalProjectsCompleted = 0;
            totalAutoIncome = 0f;
        }
    }
}