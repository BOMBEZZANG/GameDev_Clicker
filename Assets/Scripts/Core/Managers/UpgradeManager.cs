using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GameDevClicker.Core.Patterns;
using GameDevClicker.Data.ScriptableObjects;
using GameDevClicker.Game.Models;

namespace GameDevClicker.Core.Managers
{
    public class UpgradeManager : Singleton<UpgradeManager>
    {
        [Header("Upgrade Configuration")]
        [SerializeField] private UpgradeData[] allUpgrades;
        [SerializeField] private bool loadUpgradesFromResources = true;
        [SerializeField] private string upgradesResourcePath = "Upgrades";
        
        private Dictionary<string, int> _purchasedLevels;
        private Dictionary<string, UpgradeData> _upgradeDataLookup;
        private Dictionary<UpgradeData.UpgradeCategory, List<UpgradeData>> _upgradesByCategory;

        public event Action<UpgradeData> OnUpgradeAvailable;
        public event Action<UpgradeData> OnUpgradeMaxedOut;
        public event Action<UpgradeData.UpgradeCategory> OnCategoryUnlocked;

        protected override void Awake()
        {
            base.Awake();
            InitializeUpgrades();
        }

        private void Start()
        {
            SubscribeToEvents();
            LoadUpgradeLevels();
        }

        private void InitializeUpgrades()
        {
            _purchasedLevels = new Dictionary<string, int>();
            _upgradeDataLookup = new Dictionary<string, UpgradeData>();
            _upgradesByCategory = new Dictionary<UpgradeData.UpgradeCategory, List<UpgradeData>>();

            if (loadUpgradesFromResources)
            {
                LoadUpgradesFromResources();
            }

            if (allUpgrades != null && allUpgrades.Length > 0)
            {
                ProcessUpgradeData();
            }

            Debug.Log($"[UpgradeManager] Initialized with {_upgradeDataLookup.Count} upgrades");
        }

        private void LoadUpgradesFromResources()
        {
            UpgradeData[] resourceUpgrades = Resources.LoadAll<UpgradeData>(upgradesResourcePath);
            
            if (resourceUpgrades != null && resourceUpgrades.Length > 0)
            {
                allUpgrades = allUpgrades?.Concat(resourceUpgrades).ToArray() ?? resourceUpgrades;
                Debug.Log($"[UpgradeManager] Loaded {resourceUpgrades.Length} upgrades from Resources");
            }
        }

        private void ProcessUpgradeData()
        {
            foreach (UpgradeData upgrade in allUpgrades)
            {
                if (upgrade == null) continue;

                _upgradeDataLookup[upgrade.upgradeId] = upgrade;
                _purchasedLevels[upgrade.upgradeId] = 0;

                if (!_upgradesByCategory.ContainsKey(upgrade.category))
                {
                    _upgradesByCategory[upgrade.category] = new List<UpgradeData>();
                }
                _upgradesByCategory[upgrade.category].Add(upgrade);
            }
        }

        private void SubscribeToEvents()
        {
            GameEvents.OnGameLoaded += LoadUpgradeLevels;
            GameEvents.OnLevelUp += OnPlayerLevelUp;
            GameEvents.OnStageUnlocked += OnStageUnlocked;
        }

        private void LoadUpgradeLevels()
        {
            var gameData = SaveManager.Instance.CurrentGameData;
            if (gameData?.upgradeLevels != null)
            {
                foreach (var kvp in gameData.upgradeLevels)
                {
                    if (_purchasedLevels.ContainsKey(kvp.Key))
                    {
                        _purchasedLevels[kvp.Key] = kvp.Value;
                    }
                }
            }
        }

        private void SaveUpgradeLevels()
        {
            var gameData = SaveManager.Instance.CurrentGameData;
            if (gameData != null)
            {
                gameData.upgradeLevels = new Dictionary<string, int>(_purchasedLevels);
                SaveManager.Instance.MarkDirty();
            }
        }

        #region Upgrade Purchase Logic

        public bool CanAffordUpgrade(UpgradeData upgrade)
        {
            if (upgrade == null) return false;

            long cost = GetUpgradeCost(upgrade);
            
            switch (upgrade.currencyType)
            {
                case UpgradeData.CurrencyType.Money:
                    return GameModel.Instance.Money >= cost && GameModel.Instance.IsMoneyUnlocked;
                case UpgradeData.CurrencyType.Experience:
                    return GameModel.Instance.Experience >= cost;
                default:
                    return false;
            }
        }

        public bool CanPurchaseUpgrade(UpgradeData upgrade)
        {
            if (!CanAffordUpgrade(upgrade)) return false;
            if (!IsUpgradeUnlocked(upgrade)) return false;
            if (IsUpgradeMaxedOut(upgrade)) return false;
            
            return true;
        }

        public bool PurchaseUpgrade(UpgradeData upgrade)
        {
            if (!CanPurchaseUpgrade(upgrade))
            {
                Debug.LogWarning($"[UpgradeManager] Cannot purchase upgrade: {upgrade.upgradeName}");
                return false;
            }

            long cost = GetUpgradeCost(upgrade);
            
            // Deduct cost
            bool success = false;
            switch (upgrade.currencyType)
            {
                case UpgradeData.CurrencyType.Money:
                    success = GameModel.Instance.SpendMoney(cost);
                    break;
                case UpgradeData.CurrencyType.Experience:
                    success = GameModel.Instance.SpendExperience(cost);
                    break;
            }

            if (!success)
            {
                Debug.LogError($"[UpgradeManager] Failed to deduct cost for upgrade: {upgrade.upgradeName}");
                return false;
            }

            // Update level
            _purchasedLevels[upgrade.upgradeId]++;
            
            // Apply effects
            ApplyUpgradeEffects(upgrade);
            
            // Update statistics
            var gameData = SaveManager.Instance.CurrentGameData;
            if (gameData != null)
            {
                gameData.totalUpgradesPurchased++;
            }

            // Save changes
            SaveUpgradeLevels();

            // Trigger events
            GameEvents.InvokeUpgradePurchased(upgrade);
            
            // Check if upgrade is now maxed out
            if (IsUpgradeMaxedOut(upgrade))
            {
                OnUpgradeMaxedOut?.Invoke(upgrade);
            }

            Debug.Log($"[UpgradeManager] Purchased {upgrade.upgradeName} (Level {GetUpgradeLevel(upgrade.upgradeId)})");
            return true;
        }

        #endregion

        #region Upgrade Effect Application

        private void ApplyUpgradeEffects(UpgradeData upgrade)
        {
            int currentLevel = GetUpgradeLevel(upgrade.upgradeId);
            
            foreach (var effect in upgrade.effects)
            {
                ApplyUpgradeEffect(effect, currentLevel);
            }
        }

        private void ApplyUpgradeEffect(UpgradeEffect effect, int level)
        {
            float effectValue = effect.CalculateEffectValue(level);
            
            switch (effect.type)
            {
                case UpgradeEffect.EffectType.MoneyPerClick:
                    GameModel.Instance.AddMoneyPerClick(effectValue);
                    break;
                    
                case UpgradeEffect.EffectType.ExpPerClick:
                    GameModel.Instance.AddExpPerClick(effectValue);
                    break;
                    
                case UpgradeEffect.EffectType.AutoMoney:
                    GameModel.Instance.AddAutoMoney(effectValue);
                    break;
                    
                case UpgradeEffect.EffectType.AutoExp:
                    GameModel.Instance.AddAutoExp(effectValue);
                    break;
                    
                case UpgradeEffect.EffectType.AllMultiplier:
                    if (effect.isMultiplier)
                        GameModel.Instance.MultiplyAll(effectValue);
                    break;
                    
                case UpgradeEffect.EffectType.MoneyMultiplier:
                    if (effect.isMultiplier)
                        GameModel.Instance.MultiplyMoney(effectValue);
                    break;
                    
                case UpgradeEffect.EffectType.ExpMultiplier:
                    if (effect.isMultiplier)
                        GameModel.Instance.MultiplyExp(effectValue);
                    break;
                    
                // TODO: Implement other effect types as needed
                case UpgradeEffect.EffectType.ClickCriticalChance:
                case UpgradeEffect.EffectType.ClickCriticalMultiplier:
                case UpgradeEffect.EffectType.OfflineEarnings:
                case UpgradeEffect.EffectType.ProjectSpeedMultiplier:
                case UpgradeEffect.EffectType.GlobalEfficiency:
                case UpgradeEffect.EffectType.SkillLearningSpeed:
                case UpgradeEffect.EffectType.TeamProductivity:
                    Debug.Log($"[UpgradeManager] Effect type {effect.type} not yet implemented");
                    break;
                    
                default:
                    Debug.LogWarning($"[UpgradeManager] Unknown effect type: {effect.type}");
                    break;
            }
        }

        #endregion

        #region Upgrade Query Methods

        public long GetUpgradeCost(UpgradeData upgrade)
        {
            if (upgrade == null) return 0;
            int currentLevel = GetUpgradeLevel(upgrade.upgradeId);
            return upgrade.CalculatePrice(currentLevel);
        }

        public int GetUpgradeLevel(string upgradeId)
        {
            return _purchasedLevels.TryGetValue(upgradeId, out int level) ? level : 0;
        }

        public bool IsUpgradeUnlocked(UpgradeData upgrade)
        {
            if (upgrade == null) return false;
            
            var gameData = SaveManager.Instance.CurrentGameData;
            if (gameData == null) return false;
            
            var purchasedUpgradeIds = new HashSet<string>(
                _purchasedLevels.Where(kvp => kvp.Value > 0).Select(kvp => kvp.Key)
            );
            
            return upgrade.IsUnlocked(gameData.currentStage, gameData.playerLevel, purchasedUpgradeIds);
        }

        public bool IsUpgradeMaxedOut(UpgradeData upgrade)
        {
            if (upgrade == null) return true;
            if (upgrade.maxLevel <= 0) return false; // No max level
            
            return GetUpgradeLevel(upgrade.upgradeId) >= upgrade.maxLevel;
        }

        public List<UpgradeData> GetAvailableUpgrades(UpgradeData.UpgradeCategory category)
        {
            if (!_upgradesByCategory.ContainsKey(category))
                return new List<UpgradeData>();
            
            return _upgradesByCategory[category]
                .Where(upgrade => IsUpgradeUnlocked(upgrade) && !IsUpgradeMaxedOut(upgrade))
                .ToList();
        }

        public List<UpgradeData> GetUpgradesByCategory(UpgradeData.UpgradeCategory category)
        {
            return _upgradesByCategory.ContainsKey(category) 
                ? new List<UpgradeData>(_upgradesByCategory[category])
                : new List<UpgradeData>();
        }

        public UpgradeData GetUpgradeData(string upgradeId)
        {
            return _upgradeDataLookup.TryGetValue(upgradeId, out UpgradeData upgrade) ? upgrade : null;
        }

        #endregion

        #region Event Handlers

        private void OnPlayerLevelUp(int newLevel)
        {
            CheckForNewlyUnlockedUpgrades();
        }

        private void OnStageUnlocked(int stage)
        {
            CheckForNewlyUnlockedUpgrades();
        }

        private void CheckForNewlyUnlockedUpgrades()
        {
            foreach (var upgrade in allUpgrades)
            {
                if (IsUpgradeUnlocked(upgrade) && GetUpgradeLevel(upgrade.upgradeId) == 0)
                {
                    OnUpgradeAvailable?.Invoke(upgrade);
                }
            }
        }

        #endregion

        #region Utility Methods

        public void DebugPrintUpgradeInfo()
        {
            Debug.Log($"[UpgradeManager] Total Upgrades: {allUpgrades?.Length ?? 0}");
            
            foreach (var category in _upgradesByCategory)
            {
                Debug.Log($"[UpgradeManager] Category {category.Key}: {category.Value.Count} upgrades");
            }
            
            foreach (var upgrade in _purchasedLevels.Where(kvp => kvp.Value > 0))
            {
                Debug.Log($"[UpgradeManager] {upgrade.Key}: Level {upgrade.Value}");
            }
        }

        #endregion

        protected override void OnDestroy()
        {
            GameEvents.OnGameLoaded -= LoadUpgradeLevels;
            GameEvents.OnLevelUp -= OnPlayerLevelUp;
            GameEvents.OnStageUnlocked -= OnStageUnlocked;
            base.OnDestroy();
        }
    }
}