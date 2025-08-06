using System;
using System.Collections.Generic;
using UnityEngine;
using GameDevClicker.Core.Patterns;
using GameDevClicker.Core.Managers;

namespace GameDevClicker.Game.Systems
{
    [System.Serializable]
    public class GameStatistics
    {
        // Lifetime Statistics
        public long totalMoneyEarned;
        public long totalExperienceEarned;
        public long totalClicks;
        public long totalUpgradesPurchased;
        public long totalProjectsCompleted;
        public float totalPlayTime;
        
        // Session Statistics
        public long sessionMoneyEarned;
        public long sessionExperienceEarned;
        public long sessionClicks;
        public DateTime sessionStartTime;
        public float sessionDuration;
        
        // Best Records
        public long highestMoney;
        public long highestExperience;
        public float bestMoneyPerSecond;
        public float bestExpPerSecond;
        public int highestStageReached;
        public int highestLevelReached;
        
        // Milestone Tracking
        public DateTime firstPlayDate;
        public DateTime lastPlayDate;
        public int totalDaysPlayed;
        public int consecutiveDaysPlayed;
        public DateTime lastDailyRewardClaim;
    }

    public class StatisticsSystem : Singleton<StatisticsSystem>
    {
        [SerializeField] private GameStatistics statistics;
        [SerializeField] private float saveInterval = 60f; // Save stats every minute
        
        private float lastSaveTime;
        private float sessionTimer;
        
        public GameStatistics Statistics => statistics;
        
        // Events
        public event Action<string, long> OnMilestoneReached;
        public event Action<GameStatistics> OnStatisticsUpdated;
        
        protected override void Awake()
        {
            base.Awake();
            LoadStatistics();
            InitializeSession();
            SubscribeToEvents();
        }
        
        private void Update()
        {
            // Update session time
            sessionTimer += Time.deltaTime;
            statistics.sessionDuration = sessionTimer;
            statistics.totalPlayTime += Time.deltaTime;
            
            // Auto-save statistics
            if (Time.time - lastSaveTime > saveInterval)
            {
                SaveStatistics();
                lastSaveTime = Time.time;
            }
        }
        
        private void InitializeSession()
        {
            statistics.sessionStartTime = DateTime.Now;
            statistics.sessionMoneyEarned = 0;
            statistics.sessionExperienceEarned = 0;
            statistics.sessionClicks = 0;
            sessionTimer = 0f;
            
            // Update play dates
            var today = DateTime.Today;
            if (statistics.lastPlayDate.Date == today.AddDays(-1))
            {
                statistics.consecutiveDaysPlayed++;
            }
            else if (statistics.lastPlayDate.Date != today)
            {
                statistics.consecutiveDaysPlayed = 1;
            }
            
            statistics.lastPlayDate = DateTime.Now;
            statistics.totalDaysPlayed++;
            
            if (statistics.firstPlayDate == default(DateTime))
            {
                statistics.firstPlayDate = DateTime.Now;
            }
        }
        
        private void SubscribeToEvents()
        {
            GameEvents.OnMoneyChanged += OnMoneyChanged;
            GameEvents.OnExperienceChanged += OnExperienceChanged;
            GameEvents.OnClickPerformed += OnClickPerformed;
            GameEvents.OnUpgradePurchased += OnUpgradePurchased;
            GameEvents.OnProjectCompleted += OnProjectCompleted;
            GameEvents.OnStageUnlocked += OnStageUnlocked;
            GameEvents.OnLevelUp += OnLevelUp;
        }
        
        #region Event Handlers
        
        private void OnMoneyChanged(long money)
        {
            if (money > statistics.highestMoney)
            {
                statistics.highestMoney = money;
                CheckMilestone("Highest Money", money);
            }
        }
        
        private void OnExperienceChanged(long experience)
        {
            if (experience > statistics.highestExperience)
            {
                statistics.highestExperience = experience;
                CheckMilestone("Highest Experience", experience);
            }
        }
        
        private void OnClickPerformed(float moneyGained, float expGained)
        {
            statistics.totalClicks++;
            statistics.sessionClicks++;
            
            if (moneyGained > 0)
            {
                statistics.totalMoneyEarned += (long)moneyGained;
                statistics.sessionMoneyEarned += (long)moneyGained;
            }
            
            if (expGained > 0)
            {
                statistics.totalExperienceEarned += (long)expGained;
                statistics.sessionExperienceEarned += (long)expGained;
            }
            
            CheckClickMilestones();
        }
        
        private void OnUpgradePurchased(Data.ScriptableObjects.UpgradeData upgrade)
        {
            statistics.totalUpgradesPurchased++;
        }
        
        private void OnProjectCompleted(long reward)
        {
            statistics.totalProjectsCompleted++;
            CheckProjectMilestones();
        }
        
        private void OnStageUnlocked(int stage)
        {
            if (stage > statistics.highestStageReached)
            {
                statistics.highestStageReached = stage;
                CheckMilestone($"Stage {stage} Reached", stage);
            }
        }
        
        private void OnLevelUp(int level)
        {
            if (level > statistics.highestLevelReached)
            {
                statistics.highestLevelReached = level;
                CheckMilestone($"Level {level} Reached", level);
            }
        }
        
        #endregion
        
        #region Milestone Checking
        
        private void CheckClickMilestones()
        {
            // Check click milestones
            long[] clickMilestones = { 100, 1000, 10000, 100000, 1000000 };
            foreach (var milestone in clickMilestones)
            {
                if (statistics.totalClicks == milestone)
                {
                    OnMilestoneReached?.Invoke($"{milestone} Clicks", milestone);
                }
            }
        }
        
        private void CheckProjectMilestones()
        {
            // Check project completion milestones
            long[] projectMilestones = { 10, 50, 100, 500, 1000 };
            foreach (var milestone in projectMilestones)
            {
                if (statistics.totalProjectsCompleted == milestone)
                {
                    OnMilestoneReached?.Invoke($"{milestone} Projects Completed", milestone);
                }
            }
        }
        
        private void CheckMilestone(string milestoneName, long value)
        {
            OnMilestoneReached?.Invoke(milestoneName, value);
        }
        
        #endregion
        
        #region Public Methods
        
        public void UpdateIncomeRecords(float moneyPerSecond, float expPerSecond)
        {
            if (moneyPerSecond > statistics.bestMoneyPerSecond)
            {
                statistics.bestMoneyPerSecond = moneyPerSecond;
            }
            
            if (expPerSecond > statistics.bestExpPerSecond)
            {
                statistics.bestExpPerSecond = expPerSecond;
            }
        }
        
        public float GetTotalPlayTimeHours()
        {
            return statistics.totalPlayTime / 3600f;
        }
        
        public float GetSessionDurationMinutes()
        {
            return statistics.sessionDuration / 60f;
        }
        
        public float GetAverageClicksPerMinute()
        {
            if (statistics.totalPlayTime <= 0) return 0;
            return (statistics.totalClicks / statistics.totalPlayTime) * 60f;
        }
        
        public void ClaimDailyReward()
        {
            statistics.lastDailyRewardClaim = DateTime.Now;
        }
        
        public bool CanClaimDailyReward()
        {
            if (statistics.lastDailyRewardClaim == default(DateTime)) return true;
            return DateTime.Now.Date > statistics.lastDailyRewardClaim.Date;
        }
        
        #endregion
        
        #region Save/Load
        
        private void SaveStatistics()
        {
            string json = JsonUtility.ToJson(statistics, true);
            PlayerPrefs.SetString("GameStatistics", json);
            PlayerPrefs.Save();
            
            OnStatisticsUpdated?.Invoke(statistics);
        }
        
        private void LoadStatistics()
        {
            if (PlayerPrefs.HasKey("GameStatistics"))
            {
                string json = PlayerPrefs.GetString("GameStatistics");
                statistics = JsonUtility.FromJson<GameStatistics>(json);
            }
            else
            {
                statistics = new GameStatistics();
            }
        }
        
        public void ResetStatistics()
        {
            statistics = new GameStatistics();
            InitializeSession();
            SaveStatistics();
        }
        
        #endregion
        
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                SaveStatistics();
            }
        }
        
        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                SaveStatistics();
            }
        }
        
        private void OnDestroy()
        {
            SaveStatistics();
            
            // Unsubscribe from events
            GameEvents.OnMoneyChanged -= OnMoneyChanged;
            GameEvents.OnExperienceChanged -= OnExperienceChanged;
            GameEvents.OnClickPerformed -= OnClickPerformed;
            GameEvents.OnUpgradePurchased -= OnUpgradePurchased;
            GameEvents.OnProjectCompleted -= OnProjectCompleted;
            GameEvents.OnStageUnlocked -= OnStageUnlocked;
            GameEvents.OnLevelUp -= OnLevelUp;
        }
    }
}