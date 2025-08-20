using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GameDevClicker.Core.Patterns;
using GameDevClicker.Data;

namespace GameDevClicker.Core.Managers
{
    public class BalanceManager : Singleton<BalanceManager>
    {
        [Header("Balance Settings")]
        [SerializeField] private bool autoLoadOnStart = true;
        [SerializeField] private bool useLocalizedText = false;
        [SerializeField] private CSVLoader csvLoaderReference; // Drag CSVLoader component here
        
        private CSVLoader _csvLoader;
        private BalanceData _balanceData;
        
        public BalanceData CurrentBalanceData => _balanceData;
        public bool IsDataLoaded { get; private set; }
        
        public event Action OnBalanceDataLoaded;
        public event Action<string> OnBalanceDataError;
        
        protected override void Awake()
        {
            base.Awake();
            InitializeBalanceManager();
        }
        
        private void InitializeBalanceManager()
        {
            try
            {
                Debug.Log("[BalanceManager] Initializing BalanceManager...");
                
                // Ensure this object persists across scenes
                if (transform.parent == null)
                {
                    DontDestroyOnLoad(gameObject);
                    Debug.Log("[BalanceManager] Set DontDestroyOnLoad for BalanceManager");
                }
                
                // Wait a frame before trying to get CSVLoader to ensure all singletons are initialized
                StartCoroutine(InitializeCSVLoaderDelayed());
            }
            catch (Exception e)
            {
                Debug.LogError($"[BalanceManager] Error initializing BalanceManager: {e.Message}");
                CreateFallbackBalanceData();
            }
        }
        
        private System.Collections.IEnumerator InitializeCSVLoaderDelayed()
        {
            // Wait a frame to ensure all objects are initialized
            yield return null;
            
            int retryCount = 0;
            const int maxRetries = 5;
            
            while (_csvLoader == null && retryCount < maxRetries)
            {
                try
                {
                    Debug.Log($"[BalanceManager] Attempting to get CSVLoader (attempt {retryCount + 1}/{maxRetries})...");
                    
                    // Try serialized reference first
                    if (csvLoaderReference != null)
                    {
                        _csvLoader = csvLoaderReference;
                        Debug.Log("[BalanceManager] Using serialized reference CSVLoader");
                    }
                    
                    // Try to get CSVLoader from same GameObject
                    if (_csvLoader == null)
                    {
                        _csvLoader = GetComponent<CSVLoader>();
                        if (_csvLoader != null)
                            Debug.Log("[BalanceManager] Found CSVLoader on same GameObject");
                    }
                    
                    // If not found, try singleton instance
                    if (_csvLoader == null)
                    {
                        _csvLoader = CSVLoader.Instance;
                        if (_csvLoader != null)
                            Debug.Log("[BalanceManager] Using CSVLoader singleton instance");
                    }
                    
                    // If CSVLoader is null, try to find it in scene
                    if (_csvLoader == null)
                    {
                        var csvLoaderComponent = FindObjectOfType<CSVLoader>();
                        if (csvLoaderComponent != null)
                        {
                            _csvLoader = csvLoaderComponent;
                            Debug.Log("[BalanceManager] Found CSVLoader in scene");
                        }
                    }
                    
                    // If still null, try to create one
                    if (_csvLoader == null)
                    {
                        Debug.LogWarning("[BalanceManager] Creating new CSVLoader...");
                        GameObject csvLoaderGO = new GameObject("CSVLoader");
                        _csvLoader = csvLoaderGO.AddComponent<CSVLoader>();
                        DontDestroyOnLoad(csvLoaderGO);
                    }
                    
                    if (_csvLoader != null)
                    {
                        Debug.Log("[BalanceManager] CSVLoader initialized successfully");
                        break;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[BalanceManager] Error getting CSVLoader: {e.Message}");
                }
                
                retryCount++;
                yield return new WaitForSeconds(0.1f);
            }
            
            if (_csvLoader != null && autoLoadOnStart)
            {
                LoadBalanceData();
            }
            else if (_csvLoader == null)
            {
                Debug.LogError("[BalanceManager] Failed to initialize CSVLoader after all retries. Creating fallback data.");
                CreateFallbackBalanceData();
            }
        }
        
        public void LoadBalanceData()
        {
            try
            {
                Debug.Log($"[BalanceManager] LoadBalanceData called. _csvLoader is null: {_csvLoader == null}");
                Debug.Log($"[BalanceManager] csvLoaderReference is null: {csvLoaderReference == null}");
                
                if (_csvLoader == null)
                {
                    Debug.LogError("[BalanceManager] CSVLoader is null. Cannot load balance data.");
                    Debug.LogError("[BalanceManager] This suggests LoadBalanceData was called before initialization completed.");
                    OnBalanceDataError?.Invoke("CSVLoader not initialized");
                    return;
                }
                
                Debug.Log("[BalanceManager] Loading balance data from CSV files...");
                
                _csvLoader.LoadAllCSVData();
                _balanceData = _csvLoader.LoadedBalanceData;
                
                if (_balanceData != null && ValidateBalanceData())
                {
                    IsDataLoaded = true;
                    OnBalanceDataLoaded?.Invoke();
                    Debug.Log("[BalanceManager] Balance data loaded successfully!");
                }
                else
                {
                    string error = "Balance data validation failed";
                    Debug.LogError($"[BalanceManager] {error}");
                    
                    // Create minimal fallback data to prevent crashes
                    CreateFallbackBalanceData();
                    OnBalanceDataError?.Invoke(error);
                }
            }
            catch (Exception e)
            {
                string error = $"Failed to load balance data: {e.Message}";
                Debug.LogError($"[BalanceManager] {error}");
                
                // Create minimal fallback data to prevent crashes
                CreateFallbackBalanceData();
                OnBalanceDataError?.Invoke(error);
            }
        }
        
        private void CreateFallbackBalanceData()
        {
            Debug.Log("[BalanceManager] Creating fallback balance data to prevent crashes...");
            
            _balanceData = new BalanceData
            {
                Upgrades = new List<UpgradeInfo>(),
                Levels = new List<LevelInfo>(),
                Projects = new List<ProjectInfo>(),
                Stages = new List<StageInfo>()
            };
            
            // Add a basic level to prevent crashes
            _balanceData.Levels.Add(new LevelInfo
            {
                level = 1,
                requiredExp = 100
            });
            
            IsDataLoaded = true;
            Debug.Log("[BalanceManager] Fallback balance data created");
        }
        
        private bool ValidateBalanceData()
        {
            if (_balanceData == null) return false;
            
            bool hasAnyData = false;
            
            if (_balanceData.Upgrades != null && _balanceData.Upgrades.Count > 0)
            {
                Debug.Log($"[BalanceManager] Found {_balanceData.Upgrades.Count} upgrades");
                hasAnyData = true;
            }
            else
            {
                Debug.LogWarning("[BalanceManager] No upgrade data found");
            }
            
            if (_balanceData.Levels != null && _balanceData.Levels.Count > 0)
            {
                Debug.Log($"[BalanceManager] Found {_balanceData.Levels.Count} levels");
                hasAnyData = true;
            }
            else
            {
                Debug.LogWarning("[BalanceManager] No level data found");
            }
            
            if (_balanceData.Projects != null && _balanceData.Projects.Count > 0)
            {
                Debug.Log($"[BalanceManager] Found {_balanceData.Projects.Count} projects");
                hasAnyData = true;
            }
            else
            {
                Debug.LogWarning("[BalanceManager] No project data found");
            }
            
            if (_balanceData.Stages != null && _balanceData.Stages.Count > 0)
            {
                Debug.Log($"[BalanceManager] Found {_balanceData.Stages.Count} stages");
                hasAnyData = true;
            }
            else
            {
                Debug.LogWarning("[BalanceManager] No stage data found");
            }
            
            // Return true if we have at least some data
            return hasAnyData;
        }
        
        [ContextMenu("Retry Load Balance Data")]
        public void RetryLoadBalanceData()
        {
            IsDataLoaded = false;
            _balanceData = null;
            InitializeBalanceManager();
        }
        
        public UpgradeInfo GetUpgrade(string upgradeId)
        {
            if (!IsDataLoaded) return null;
            return _balanceData.Upgrades.FirstOrDefault(u => u.upgradeId == upgradeId);
        }
        
        public List<UpgradeInfo> GetUpgradesByCategory(string category)
        {
            if (!IsDataLoaded) return new List<UpgradeInfo>();
            return _balanceData.Upgrades.Where(u => u.category == category).ToList();
        }
        
        public LevelInfo GetLevelInfo(int level)
        {
            if (!IsDataLoaded) return null;
            return _balanceData.Levels.FirstOrDefault(l => l.level == level);
        }
        
        public long GetLevelRequiredExp(int level)
        {
            var levelInfo = GetLevelInfo(level);
            return levelInfo?.requiredExp ?? 0;
        }
        
        public float GetLevelMoneyMultiplier(int level)
        {
            var levelInfo = GetLevelInfo(level);
            return levelInfo?.moneyMultiplier ?? 1f;
        }
        
        public ProjectInfo GetProject(string projectId)
        {
            if (!IsDataLoaded) return null;
            return _balanceData.Projects.FirstOrDefault(p => p.projectId == projectId);
        }
        
        public List<ProjectInfo> GetProjectsByStage(int stage)
        {
            if (!IsDataLoaded) return new List<ProjectInfo>();
            return _balanceData.Projects.Where(p => p.stage == stage).ToList();
        }
        
        public ProjectInfo GetNextAvailableProject(int currentStage, long currentExp)
        {
            if (!IsDataLoaded) return null;
            
            var stageProjects = GetProjectsByStage(currentStage);
            return stageProjects.FirstOrDefault(p => p.requiredExp <= currentExp);
        }
        
        public StageInfo GetStageInfo(int stage)
        {
            if (!IsDataLoaded) return null;
            return _balanceData.Stages.FirstOrDefault(s => s.stage == stage);
        }
        
        public float CalculateUpgradePrice(string upgradeId, int currentLevel)
        {
            var upgrade = GetUpgrade(upgradeId);
            if (upgrade == null) return 0;
            
            if (currentLevel == 0) return upgrade.basePrice;
            
            if (upgrade.priceMultiplier <= 0) return upgrade.basePrice;
            
            return upgrade.basePrice * Mathf.Pow(upgrade.priceMultiplier, currentLevel);
        }
        
        public float CalculateUpgradeEffect(string upgradeId, int level)
        {
            var upgrade = GetUpgrade(upgradeId);
            if (upgrade == null) return 0;
            
            return upgrade.effectValue * level;
        }
        
        public bool CanPurchaseUpgrade(string upgradeId, int currentLevel, long playerMoney, long playerExp)
        {
            var upgrade = GetUpgrade(upgradeId);
            if (upgrade == null) return false;
            
            if (upgrade.maxLevel > 0 && currentLevel >= upgrade.maxLevel)
                return false;
            
            float price = CalculateUpgradePrice(upgradeId, currentLevel);
            
            if (upgrade.currencyType == "money")
                return playerMoney >= price;
            else if (upgrade.currencyType == "experience")
                return playerExp >= price;
            
            return false;
        }
        
        public bool IsUpgradeUnlocked(string upgradeId, int playerLevel, int playerStage)
        {
            var upgrade = GetUpgrade(upgradeId);
            if (upgrade == null) return false;
            
            if (string.IsNullOrEmpty(upgrade.unlockCondition) || upgrade.unlockCondition == "none")
                return true;
            
            if (upgrade.unlockCondition.StartsWith("level_"))
            {
                if (int.TryParse(upgrade.unlockCondition.Substring(6), out int requiredLevel))
                {
                    return playerLevel >= requiredLevel;
                }
            }
            else if (upgrade.unlockCondition.StartsWith("stage_"))
            {
                if (int.TryParse(upgrade.unlockCondition.Substring(6), out int requiredStage))
                {
                    return playerStage >= requiredStage;
                }
            }
            
            return false;
        }
        
        public string GetLocalizedUpgradeName(string upgradeId)
        {
            var upgrade = GetUpgrade(upgradeId);
            if (upgrade == null) return "";
            
            return useLocalizedText ? upgrade.nameKo : upgrade.nameEn;
        }
        
        public string GetLocalizedUpgradeDescription(string upgradeId)
        {
            var upgrade = GetUpgrade(upgradeId);
            if (upgrade == null) return "";
            
            return useLocalizedText ? upgrade.descriptionKo : upgrade.descriptionEn;
        }
        
        public string GetLocalizedProjectName(string projectId)
        {
            var project = GetProject(projectId);
            if (project == null) return "";
            
            return useLocalizedText ? project.nameKo : project.nameEn;
        }
        
        public void ReloadBalanceData()
        {
            IsDataLoaded = false;
            _balanceData = null;
            LoadBalanceData();
        }
        
        public Dictionary<string, float> GetAllActiveEffects(Dictionary<string, int> purchasedUpgrades)
        {
            Dictionary<string, float> effects = new Dictionary<string, float>();
            
            foreach (var kvp in purchasedUpgrades)
            {
                var upgrade = GetUpgrade(kvp.Key);
                if (upgrade == null) continue;
                
                string effectType = upgrade.effectType;
                float effectValue = CalculateUpgradeEffect(kvp.Key, kvp.Value);
                
                if (effects.ContainsKey(effectType))
                {
                    if (effectType.Contains("multiplier"))
                        effects[effectType] *= (1 + effectValue);
                    else
                        effects[effectType] += effectValue;
                }
                else
                {
                    effects[effectType] = effectValue;
                }
            }
            
            return effects;
        }
        
        public BalanceSnapshot CreateBalanceSnapshot()
        {
            return new BalanceSnapshot
            {
                Timestamp = DateTime.Now,
                UpgradeCount = _balanceData?.Upgrades?.Count ?? 0,
                LevelCount = _balanceData?.Levels?.Count ?? 0,
                ProjectCount = _balanceData?.Projects?.Count ?? 0,
                StageCount = _balanceData?.Stages?.Count ?? 0
            };
        }
        
        [Serializable]
        public class BalanceSnapshot
        {
            public DateTime Timestamp;
            public int UpgradeCount;
            public int LevelCount;
            public int ProjectCount;
            public int StageCount;
        }
    }
}