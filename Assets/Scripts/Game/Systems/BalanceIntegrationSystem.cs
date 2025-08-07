using System;
using System.Collections.Generic;
using UnityEngine;
using GameDevClicker.Core.Managers;
using GameDevClicker.Data;

namespace GameDevClicker.Game.Systems
{
    public class BalanceIntegrationSystem : MonoBehaviour
    {
        [Header("Integration Settings")]
        [SerializeField] private bool autoInitialize = true;
        [SerializeField] private bool debugMode = false;
        
        [Header("Current Game State")]
        [SerializeField] private int currentPlayerLevel = 1;
        [SerializeField] private long currentPlayerExp = 0;
        [SerializeField] private long currentPlayerMoney = 0;
        [SerializeField] private int currentStage = 1;
        
        [Header("Active Upgrades")]
        [SerializeField] private List<ActiveUpgrade> activeUpgrades = new List<ActiveUpgrade>();
        
        private BalanceManager _balanceManager;
        private Dictionary<string, int> _purchasedUpgrades = new Dictionary<string, int>();
        private Dictionary<string, float> _activeEffects = new Dictionary<string, float>();
        
        public event Action<string, int> OnUpgradePurchased;
        public event Action<int> OnLevelUp;
        public event Action<int> OnStageUnlocked;
        public event Action<string> OnProjectCompleted;
        
        [Serializable]
        public class ActiveUpgrade
        {
            public string upgradeId;
            public int level;
            public float currentEffect;
        }
        
        private void Start()
        {
            if (autoInitialize)
            {
                Initialize();
            }
        }
        
        public void Initialize()
        {
            _balanceManager = BalanceManager.Instance;
            
            if (_balanceManager == null)
            {
                Debug.LogError("[BalanceIntegrationSystem] BalanceManager not found!");
                return;
            }
            
            _balanceManager.OnBalanceDataLoaded += OnBalanceDataLoaded;
            
            if (!_balanceManager.IsDataLoaded)
            {
                _balanceManager.LoadBalanceData();
            }
            else
            {
                OnBalanceDataLoaded();
            }
        }
        
        private void OnBalanceDataLoaded()
        {
            Debug.Log("[BalanceIntegrationSystem] Balance data loaded, initializing game systems...");
            UpdateActiveEffects();
            CheckUnlocks();
        }
        
        public void SetPlayerData(int level, long exp, long money, int stage)
        {
            currentPlayerLevel = level;
            currentPlayerExp = exp;
            currentPlayerMoney = money;
            currentStage = stage;
            
            CheckUnlocks();
            UpdateActiveEffects();
        }
        
        public bool TryPurchaseUpgrade(string upgradeId)
        {
            if (!_balanceManager.IsDataLoaded)
            {
                Debug.LogWarning("[BalanceIntegrationSystem] Balance data not loaded");
                return false;
            }
            
            int currentLevel = _purchasedUpgrades.ContainsKey(upgradeId) ? _purchasedUpgrades[upgradeId] : 0;
            
            if (!_balanceManager.CanPurchaseUpgrade(upgradeId, currentLevel, currentPlayerMoney, currentPlayerExp))
            {
                if (debugMode) Debug.Log($"[BalanceIntegrationSystem] Cannot purchase upgrade {upgradeId}");
                return false;
            }
            
            var upgrade = _balanceManager.GetUpgrade(upgradeId);
            if (upgrade == null) return false;
            
            float price = _balanceManager.CalculateUpgradePrice(upgradeId, currentLevel);
            
            if (upgrade.currencyType == "money")
            {
                currentPlayerMoney -= (long)price;
            }
            else if (upgrade.currencyType == "experience")
            {
                currentPlayerExp -= (long)price;
            }
            
            if (_purchasedUpgrades.ContainsKey(upgradeId))
            {
                _purchasedUpgrades[upgradeId]++;
            }
            else
            {
                _purchasedUpgrades[upgradeId] = 1;
            }
            
            UpdateActiveUpgrade(upgradeId);
            UpdateActiveEffects();
            
            OnUpgradePurchased?.Invoke(upgradeId, _purchasedUpgrades[upgradeId]);
            
            if (debugMode) Debug.Log($"[BalanceIntegrationSystem] Purchased upgrade {upgradeId} to level {_purchasedUpgrades[upgradeId]}");
            
            return true;
        }
        
        private void UpdateActiveUpgrade(string upgradeId)
        {
            var existingUpgrade = activeUpgrades.Find(u => u.upgradeId == upgradeId);
            
            if (existingUpgrade != null)
            {
                existingUpgrade.level = _purchasedUpgrades[upgradeId];
                existingUpgrade.currentEffect = _balanceManager.CalculateUpgradeEffect(upgradeId, existingUpgrade.level);
            }
            else
            {
                activeUpgrades.Add(new ActiveUpgrade
                {
                    upgradeId = upgradeId,
                    level = _purchasedUpgrades[upgradeId],
                    currentEffect = _balanceManager.CalculateUpgradeEffect(upgradeId, _purchasedUpgrades[upgradeId])
                });
            }
        }
        
        private void UpdateActiveEffects()
        {
            _activeEffects = _balanceManager.GetAllActiveEffects(_purchasedUpgrades);
            
            if (debugMode)
            {
                Debug.Log("[BalanceIntegrationSystem] Active effects updated:");
                foreach (var effect in _activeEffects)
                {
                    Debug.Log($"  {effect.Key}: {effect.Value}");
                }
            }
        }
        
        public void AddExperience(long amount)
        {
            currentPlayerExp += amount;
            
            CheckLevelUp();
        }
        
        public void AddMoney(long amount)
        {
            currentPlayerMoney += amount;
        }
        
        private void CheckLevelUp()
        {
            if (!_balanceManager.IsDataLoaded) return;
            
            var nextLevelInfo = _balanceManager.GetLevelInfo(currentPlayerLevel + 1);
            
            while (nextLevelInfo != null && currentPlayerExp >= nextLevelInfo.requiredExp)
            {
                currentPlayerLevel++;
                
                if (nextLevelInfo.bonusReward > 0)
                {
                    currentPlayerMoney += nextLevelInfo.bonusReward;
                }
                
                OnLevelUp?.Invoke(currentPlayerLevel);
                
                if (!string.IsNullOrEmpty(nextLevelInfo.unlockFeature) && nextLevelInfo.unlockFeature != "none")
                {
                    HandleFeatureUnlock(nextLevelInfo.unlockFeature);
                }
                
                if (debugMode) Debug.Log($"[BalanceIntegrationSystem] Level up! Now level {currentPlayerLevel}");
                
                nextLevelInfo = _balanceManager.GetLevelInfo(currentPlayerLevel + 1);
            }
            
            CheckUnlocks();
        }
        
        private void HandleFeatureUnlock(string feature)
        {
            switch (feature)
            {
                case "money_system":
                    Debug.Log("[BalanceIntegrationSystem] Money system unlocked!");
                    break;
                case "first_skill":
                    Debug.Log("[BalanceIntegrationSystem] First skill unlocked!");
                    break;
                default:
                    if (debugMode) Debug.Log($"[BalanceIntegrationSystem] Feature unlocked: {feature}");
                    break;
            }
        }
        
        private void CheckUnlocks()
        {
            if (!_balanceManager.IsDataLoaded) return;
            
            var stageInfo = _balanceManager.GetStageInfo(currentStage + 1);
            if (stageInfo != null && currentPlayerLevel >= stageInfo.requiredLevel)
            {
                currentStage++;
                OnStageUnlocked?.Invoke(currentStage);
                
                if (debugMode) Debug.Log($"[BalanceIntegrationSystem] Stage {currentStage} unlocked!");
            }
        }
        
        public bool TryStartProject(string projectId)
        {
            if (!_balanceManager.IsDataLoaded) return false;
            
            var project = _balanceManager.GetProject(projectId);
            if (project == null) return false;
            
            if (project.stage > currentStage)
            {
                if (debugMode) Debug.Log($"[BalanceIntegrationSystem] Project {projectId} requires stage {project.stage}");
                return false;
            }
            
            if (currentPlayerExp < project.requiredExp)
            {
                if (debugMode) Debug.Log($"[BalanceIntegrationSystem] Not enough experience for project {projectId}");
                return false;
            }
            
            StartCoroutine(RunProject(project));
            return true;
        }
        
        private System.Collections.IEnumerator RunProject(ProjectInfo project)
        {
            if (debugMode) Debug.Log($"[BalanceIntegrationSystem] Starting project: {project.nameEn}");
            
            float timeMultiplier = GetEffectValue("project_speed", 1f);
            float adjustedTime = project.completionTime / timeMultiplier;
            
            yield return new WaitForSeconds(adjustedTime);
            
            long reward = (long)(project.baseReward * GetEffectValue("all_multiplier", 1f));
            currentPlayerMoney += reward;
            
            OnProjectCompleted?.Invoke(project.projectId);
            
            if (debugMode) Debug.Log($"[BalanceIntegrationSystem] Project {project.nameEn} completed! Reward: {reward}");
        }
        
        public float GetEffectValue(string effectType, float defaultValue = 0f)
        {
            return _activeEffects.ContainsKey(effectType) ? _activeEffects[effectType] : defaultValue;
        }
        
        public float CalculateClickValue()
        {
            float baseClick = 1f;
            
            baseClick += GetEffectValue("exp_per_click", 0);
            baseClick *= GetEffectValue("all_multiplier", 1f);
            baseClick *= GetEffectValue("exp_multiplier", 1f);
            
            return baseClick;
        }
        
        public float CalculateMoneyPerClick()
        {
            float baseMoney = 0f;
            
            if (currentPlayerLevel >= 10)
            {
                baseMoney = 1f;
            }
            
            baseMoney += GetEffectValue("money_per_click", 0);
            baseMoney *= GetEffectValue("all_multiplier", 1f);
            baseMoney *= GetEffectValue("money_multiplier", 1f);
            baseMoney *= _balanceManager.GetLevelMoneyMultiplier(currentPlayerLevel);
            
            return baseMoney;
        }
        
        public float CalculateAutoIncome()
        {
            float autoMoney = GetEffectValue("auto_money", 0);
            float autoIncome = GetEffectValue("auto_income", 0);
            
            float total = autoMoney + autoIncome;
            total *= GetEffectValue("all_multiplier", 1f);
            total *= GetEffectValue("auto_multiplier", 1f);
            
            return total;
        }
        
        public float CalculateAutoExp()
        {
            float autoExp = GetEffectValue("auto_exp", 0);
            float autoIncome = GetEffectValue("auto_income", 0) * 0.5f;
            
            float total = autoExp + autoIncome;
            total *= GetEffectValue("all_multiplier", 1f);
            total *= GetEffectValue("exp_multiplier", 1f);
            
            return total;
        }
        
        public List<UpgradeInfo> GetAvailableUpgrades()
        {
            if (!_balanceManager.IsDataLoaded) return new List<UpgradeInfo>();
            
            List<UpgradeInfo> available = new List<UpgradeInfo>();
            
            foreach (var upgrade in _balanceManager.CurrentBalanceData.Upgrades)
            {
                if (_balanceManager.IsUpgradeUnlocked(upgrade.upgradeId, currentPlayerLevel, currentStage))
                {
                    int currentLevel = _purchasedUpgrades.ContainsKey(upgrade.upgradeId) ? 
                        _purchasedUpgrades[upgrade.upgradeId] : 0;
                    
                    if (upgrade.maxLevel < 0 || currentLevel < upgrade.maxLevel)
                    {
                        available.Add(upgrade);
                    }
                }
            }
            
            return available;
        }
        
        public List<ProjectInfo> GetAvailableProjects()
        {
            if (!_balanceManager.IsDataLoaded) return new List<ProjectInfo>();
            
            return _balanceManager.GetProjectsByStage(currentStage)
                .FindAll(p => p.requiredExp <= currentPlayerExp);
        }
        
        public SaveData CreateSaveData()
        {
            return new SaveData
            {
                playerLevel = currentPlayerLevel,
                playerExp = currentPlayerExp,
                playerMoney = currentPlayerMoney,
                currentStage = currentStage,
                purchasedUpgrades = new Dictionary<string, int>(_purchasedUpgrades)
            };
        }
        
        public void LoadSaveData(SaveData saveData)
        {
            if (saveData == null) return;
            
            currentPlayerLevel = saveData.playerLevel;
            currentPlayerExp = saveData.playerExp;
            currentPlayerMoney = saveData.playerMoney;
            currentStage = saveData.currentStage;
            _purchasedUpgrades = new Dictionary<string, int>(saveData.purchasedUpgrades);
            
            activeUpgrades.Clear();
            foreach (var upgrade in _purchasedUpgrades)
            {
                UpdateActiveUpgrade(upgrade.Key);
            }
            
            UpdateActiveEffects();
            CheckUnlocks();
        }
        
        [Serializable]
        public class SaveData
        {
            public int playerLevel;
            public long playerExp;
            public long playerMoney;
            public int currentStage;
            public Dictionary<string, int> purchasedUpgrades;
        }
        
        private void OnDestroy()
        {
            if (_balanceManager != null)
            {
                _balanceManager.OnBalanceDataLoaded -= OnBalanceDataLoaded;
            }
        }
    }
}