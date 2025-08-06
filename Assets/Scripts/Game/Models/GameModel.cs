using System;
using System.Collections.Generic;
using UnityEngine;
using GameDevClicker.Core.Patterns;
using GameDevClicker.Core.Managers;
using GameDevClicker.Core.Utilities;

namespace GameDevClicker.Game.Models
{
    public class GameModel : Singleton<GameModel>
    {
        private GameData _data;
        
        [Header("Level Configuration")]
        [SerializeField] private long _baseExpPerLevel = 100;
        [SerializeField] private float _expLevelMultiplier = 1.5f;
        
        [Header("Critical Hit")]
        [SerializeField] private float _baseCriticalChance = 0.05f;
        [SerializeField] private float _baseCriticalMultiplier = 2f;
        
        [Header("Auto Income Timer")]
        private float _autoIncomeTimer = 0f;
        private const float AUTO_INCOME_INTERVAL = 1f;

        // Properties
        public long Money => _data?.money ?? 0;
        public long Experience => _data?.experience ?? 0;
        public float MoneyPerClick => _data?.moneyPerClick ?? 0f;
        public float ExpPerClick => _data?.expPerClick ?? 1f;
        public float AutoMoney => _data?.autoMoney ?? 0f;
        public float AutoExp => _data?.autoExp ?? 0f;
        public int CurrentStage => _data?.currentStage ?? 1;
        public int PlayerLevel => _data?.playerLevel ?? 1;
        public Dictionary<string, float> Multipliers => _data?.multipliers;
        public HashSet<string> UnlockedFeatures => _data?.unlockedFeatures;

        public bool IsMoneyUnlocked => PlayerLevel >= 10;
        
        protected override void Awake()
        {
            base.Awake();
            InitializeData();
        }

        private void Start()
        {
            SubscribeToEvents();
        }

        private void Update()
        {
            if (_data != null && GameManager.Instance.CurrentState == GameState.Playing)
            {
                ProcessAutoIncome();
            }
        }

        private void InitializeData()
        {
            _data = SaveManager.Instance.CurrentGameData;
            if (_data == null)
            {
                Debug.LogError("[GameModel] No game data available from SaveManager");
                return;
            }
        }

        private void SubscribeToEvents()
        {
            GameEvents.OnGameLoaded += OnGameLoaded;
        }

        private void OnGameLoaded()
        {
            _data = SaveManager.Instance.CurrentGameData;
            RefreshAllValues();
        }

        private void RefreshAllValues()
        {
            GameEvents.InvokeMoneyChanged(_data.money);
            GameEvents.InvokeExperienceChanged(_data.experience);
            GameEvents.InvokeClickValueChanged(_data.moneyPerClick, _data.expPerClick);
            GameEvents.InvokeAutoIncomeChanged(_data.autoMoney, _data.autoExp);
        }

        #region Currency Management

        public void PerformClick(Vector2 clickPosition = default)
        {
            if (_data == null) return;

            float expGain = CalculateExpGain();
            float moneyGain = 0f;
            
            // Money only after Level 10
            if (IsMoneyUnlocked)
            {
                moneyGain = CalculateMoneyGain();
            }

            // Check for critical hit
            bool isCritical = UnityEngine.Random.Range(0f, 1f) < GetCriticalChance();
            if (isCritical)
            {
                float critMultiplier = GetCriticalMultiplier();
                expGain *= critMultiplier;
                moneyGain *= critMultiplier;
                GameEvents.InvokeCriticalClick(clickPosition);
            }

            AddExperience((long)expGain);
            if (moneyGain > 0)
            {
                AddMoney((long)moneyGain);
            }

            _data.totalClicks++;
            SaveManager.Instance.MarkDirty();
            
            GameEvents.InvokeClickPerformed(moneyGain, expGain);
        }

        public void AddMoney(long amount)
        {
            if (_data == null || amount <= 0) return;
            
            _data.money += amount;
            _data.totalMoneyEarned += amount;
            GameEvents.InvokeMoneyChanged(_data.money);
            SaveManager.Instance.MarkDirty();
        }

        public void AddExperience(long amount)
        {
            if (_data == null || amount <= 0) return;
            
            long oldLevel = CalculateLevel(_data.experience);
            _data.experience += amount;
            _data.totalExperienceEarned += amount;
            
            long newLevel = CalculateLevel(_data.experience);
            if (newLevel > oldLevel)
            {
                LevelUp((int)newLevel);
            }
            
            GameEvents.InvokeExperienceChanged(_data.experience);
            SaveManager.Instance.MarkDirty();
        }

        public bool SpendMoney(long amount)
        {
            if (_data == null || _data.money < amount) return false;
            
            _data.money -= amount;
            GameEvents.InvokeMoneyChanged(_data.money);
            SaveManager.Instance.MarkDirty();
            return true;
        }

        public bool SpendExperience(long amount)
        {
            if (_data == null || _data.experience < amount) return false;
            
            _data.experience -= amount;
            GameEvents.InvokeExperienceChanged(_data.experience);
            SaveManager.Instance.MarkDirty();
            return true;
        }

        #endregion

        #region Click Values & Auto Income

        public void AddMoneyPerClick(float amount)
        {
            if (_data == null) return;
            
            _data.moneyPerClick += amount;
            GameEvents.InvokeClickValueChanged(_data.moneyPerClick, _data.expPerClick);
            SaveManager.Instance.MarkDirty();
        }

        public void AddExpPerClick(float amount)
        {
            if (_data == null) return;
            
            _data.expPerClick += amount;
            GameEvents.InvokeClickValueChanged(_data.moneyPerClick, _data.expPerClick);
            SaveManager.Instance.MarkDirty();
        }

        public void AddAutoMoney(float amount)
        {
            if (_data == null) return;
            
            _data.autoMoney += amount;
            _data.totalAutoIncome += amount;
            GameEvents.InvokeAutoIncomeChanged(_data.autoMoney, _data.autoExp);
            SaveManager.Instance.MarkDirty();
        }

        public void AddAutoExp(float amount)
        {
            if (_data == null) return;
            
            _data.autoExp += amount;
            _data.totalAutoIncome += amount;
            GameEvents.InvokeAutoIncomeChanged(_data.autoMoney, _data.autoExp);
            SaveManager.Instance.MarkDirty();
        }

        #endregion

        #region Multipliers

        public void MultiplyMoney(float multiplier)
        {
            if (_data?.multipliers == null) return;
            
            _data.multipliers["money"] *= multiplier;
            SaveManager.Instance.MarkDirty();
        }

        public void MultiplyExp(float multiplier)
        {
            if (_data?.multipliers == null) return;
            
            _data.multipliers["exp"] *= multiplier;
            SaveManager.Instance.MarkDirty();
        }

        public void MultiplyAll(float multiplier)
        {
            if (_data?.multipliers == null) return;
            
            _data.multipliers["all"] *= multiplier;
            SaveManager.Instance.MarkDirty();
        }

        #endregion

        #region Calculations

        private float CalculateMoneyGain()
        {
            if (!IsMoneyUnlocked) return 0f;
            
            float baseValue = _data.moneyPerClick;
            float levelBonus = Mathf.Min((PlayerLevel - 10) * 0.1f, 1.0f);
            
            return baseValue * 
                   (1f + levelBonus) * 
                   _data.multipliers["money"] * 
                   _data.multipliers["all"];
        }

        private float CalculateExpGain()
        {
            return _data.expPerClick * 
                   _data.multipliers["exp"] * 
                   _data.multipliers["all"];
        }

        private float GetCriticalChance()
        {
            return _baseCriticalChance; // Can be modified by upgrades later
        }

        private float GetCriticalMultiplier()
        {
            return _baseCriticalMultiplier; // Can be modified by upgrades later
        }

        private long CalculateLevel(long experience)
        {
            if (experience <= 0) return 1;
            
            long level = 1;
            long expRequired = _baseExpPerLevel;
            long totalExpRequired = 0;
            
            while (totalExpRequired + expRequired <= experience)
            {
                totalExpRequired += expRequired;
                level++;
                expRequired = (long)(_baseExpPerLevel * Mathf.Pow(_expLevelMultiplier, level - 1));
            }
            
            return level;
        }

        public long GetExpRequiredForNextLevel()
        {
            long currentLevel = PlayerLevel;
            long expForNextLevel = (long)(_baseExpPerLevel * Mathf.Pow(_expLevelMultiplier, currentLevel));
            long totalExpForCurrentLevel = GetTotalExpRequiredForLevel(currentLevel);
            
            return totalExpForCurrentLevel + expForNextLevel - _data.experience;
        }

        private long GetTotalExpRequiredForLevel(long level)
        {
            long total = 0;
            for (int i = 1; i < level; i++)
            {
                total += (long)(_baseExpPerLevel * Mathf.Pow(_expLevelMultiplier, i - 1));
            }
            return total;
        }

        #endregion

        #region Level System

        private void LevelUp(int newLevel)
        {
            int oldLevel = _data.playerLevel;
            _data.playerLevel = newLevel;
            
            Debug.Log($"[GameModel] Level up! {oldLevel} -> {newLevel}");
            
            // Check for money unlock at level 10
            if (newLevel == 10 && !_data.unlockedFeatures.Contains("money"))
            {
                _data.unlockedFeatures.Add("money");
                GameEvents.InvokeFeatureUnlocked("money");
                GameEvents.InvokeNotificationShown("First Sale!", "Your game is selling! You now earn money from development!");
            }
            
            GameEvents.InvokeLevelUp(newLevel);
        }

        #endregion

        #region Auto Income Processing

        private void ProcessAutoIncome()
        {
            _autoIncomeTimer += Time.deltaTime;
            
            if (_autoIncomeTimer >= AUTO_INCOME_INTERVAL)
            {
                if (_data.autoMoney > 0)
                {
                    long moneyGain = (long)(_data.autoMoney * 
                                          _data.multipliers["money"] * 
                                          _data.multipliers["all"] * 
                                          AUTO_INCOME_INTERVAL);
                    AddMoney(moneyGain);
                }
                
                if (_data.autoExp > 0)
                {
                    long expGain = (long)(_data.autoExp * 
                                        _data.multipliers["exp"] * 
                                        _data.multipliers["all"] * 
                                        AUTO_INCOME_INTERVAL);
                    AddExperience(expGain);
                }
                
                _autoIncomeTimer = 0f;
            }
        }

        #endregion

        #region Utility Methods

        public string GetFormattedMoney()
        {
            return NumberFormatter.FormatCurrency(Money);
        }

        public string GetFormattedExperience()
        {
            return NumberFormatter.Format(Experience);
        }

        public string GetFormattedMoneyPerClick()
        {
            return NumberFormatter.FormatCurrency((long)MoneyPerClick) + "/click";
        }

        public string GetFormattedExpPerClick()
        {
            return NumberFormatter.Format((long)ExpPerClick) + "/click";
        }

        public string GetFormattedAutoMoney()
        {
            return NumberFormatter.FormatRate((long)AutoMoney);
        }

        public string GetFormattedAutoExp()
        {
            return NumberFormatter.Format((long)AutoExp) + "/sec";
        }

        #endregion

        protected override void OnDestroy()
        {
            GameEvents.OnGameLoaded -= OnGameLoaded;
            base.OnDestroy();
        }
    }
}