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
            _csvLoader = CSVLoader.Instance;
            
            if (autoLoadOnStart)
            {
                LoadBalanceData();
            }
        }
        
        public void LoadBalanceData()
        {
            try
            {
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
                    OnBalanceDataError?.Invoke(error);
                }
            }
            catch (Exception e)
            {
                string error = $"Failed to load balance data: {e.Message}";
                Debug.LogError($"[BalanceManager] {error}");
                OnBalanceDataError?.Invoke(error);
            }
        }
        
        private bool ValidateBalanceData()
        {
            if (_balanceData == null) return false;
            
            bool isValid = true;
            
            if (_balanceData.Upgrades == null || _balanceData.Upgrades.Count == 0)
            {
                Debug.LogWarning("[BalanceManager] No upgrade data found");
                isValid = false;
            }
            
            if (_balanceData.Levels == null || _balanceData.Levels.Count == 0)
            {
                Debug.LogWarning("[BalanceManager] No level data found");
                isValid = false;
            }
            
            if (_balanceData.Projects == null || _balanceData.Projects.Count == 0)
            {
                Debug.LogWarning("[BalanceManager] No project data found");
                isValid = false;
            }
            
            return isValid;
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