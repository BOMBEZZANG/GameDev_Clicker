using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GameDevClicker.Core.Patterns;
using GameDevClicker.Core.Managers;
using GameDevClicker.Core.Utilities;
using GameDevClicker.Game.Models;

namespace GameDevClicker.Game.Systems
{
    [System.Serializable]
    public class Achievement
    {
        public string id;
        public string name;
        public string description;
        public AchievementType type;
        public AchievementRarity rarity;
        public Sprite icon;
        public bool isUnlocked;
        public DateTime unlockedDate;
        
        // Progress tracking
        public long targetValue;
        public long currentValue;
        public bool isProgressBased => targetValue > 0;
        
        // Rewards
        public long moneyReward;
        public long expReward;
        public float multiplierReward;
        public string rewardDescription;
    }
    
    public enum AchievementType
    {
        Click,
        Money,
        Experience,
        Upgrade,
        Project,
        Stage,
        Level,
        PlayTime,
        Special
    }
    
    public enum AchievementRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }
    
    public class AchievementSystem : Singleton<AchievementSystem>
    {
        [Header("Achievement Configuration")]
        [SerializeField] private List<Achievement> allAchievements;
        [SerializeField] private bool showUnlockNotifications = true;
        [SerializeField] private float notificationDuration = 3f;
        
        private Dictionary<string, Achievement> achievementDict;
        private List<Achievement> unlockedAchievements;
        private List<Achievement> pendingAchievements;
        
        // Events
        public event Action<Achievement> OnAchievementUnlocked;
        public event Action<Achievement> OnAchievementProgress;
        public event Action<List<Achievement>> OnAchievementsLoaded;
        
        protected override void Awake()
        {
            base.Awake();
            InitializeAchievements();
            LoadAchievementProgress();
        }
        
        private void Start()
        {
            SubscribeToEvents();
            CheckAllAchievements();
        }
        
        private void InitializeAchievements()
        {
            achievementDict = new Dictionary<string, Achievement>();
            unlockedAchievements = new List<Achievement>();
            pendingAchievements = new List<Achievement>();
            
            // Create default achievements if none exist
            if (allAchievements == null || allAchievements.Count == 0)
            {
                CreateDefaultAchievements();
            }
            
            // Build dictionary for quick lookup
            foreach (var achievement in allAchievements)
            {
                achievementDict[achievement.id] = achievement;
                if (achievement.isUnlocked)
                {
                    unlockedAchievements.Add(achievement);
                }
            }
        }
        
        private void CreateDefaultAchievements()
        {
            allAchievements = new List<Achievement>
            {
                // Click Achievements
                new Achievement
                {
                    id = "first_click",
                    name = "First Click",
                    description = "Make your first click",
                    type = AchievementType.Click,
                    rarity = AchievementRarity.Common,
                    targetValue = 1,
                    expReward = 10
                },
                new Achievement
                {
                    id = "click_100",
                    name = "Click Novice",
                    description = "Click 100 times",
                    type = AchievementType.Click,
                    rarity = AchievementRarity.Common,
                    targetValue = 100,
                    expReward = 100
                },
                new Achievement
                {
                    id = "click_1000",
                    name = "Click Master",
                    description = "Click 1,000 times",
                    type = AchievementType.Click,
                    rarity = AchievementRarity.Uncommon,
                    targetValue = 1000,
                    expReward = 1000,
                    moneyReward = 500
                },
                new Achievement
                {
                    id = "click_10000",
                    name = "Click Legend",
                    description = "Click 10,000 times",
                    type = AchievementType.Click,
                    rarity = AchievementRarity.Rare,
                    targetValue = 10000,
                    expReward = 10000,
                    moneyReward = 5000
                },
                
                // Money Achievements
                new Achievement
                {
                    id = "money_unlock",
                    name = "First Sale",
                    description = "Unlock money generation",
                    type = AchievementType.Money,
                    rarity = AchievementRarity.Common,
                    moneyReward = 100
                },
                new Achievement
                {
                    id = "money_1000",
                    name = "Thousand-aire",
                    description = "Earn 1,000 money total",
                    type = AchievementType.Money,
                    rarity = AchievementRarity.Uncommon,
                    targetValue = 1000,
                    expReward = 500
                },
                new Achievement
                {
                    id = "money_1M",
                    name = "Millionaire",
                    description = "Earn 1,000,000 money total",
                    type = AchievementType.Money,
                    rarity = AchievementRarity.Rare,
                    targetValue = 1000000,
                    multiplierReward = 0.1f,
                    rewardDescription = "+10% All Income"
                },
                
                // Experience Achievements
                new Achievement
                {
                    id = "exp_10000",
                    name = "Experience Gatherer",
                    description = "Earn 10,000 experience total",
                    type = AchievementType.Experience,
                    rarity = AchievementRarity.Common,
                    targetValue = 10000,
                    moneyReward = 1000
                },
                new Achievement
                {
                    id = "exp_1M",
                    name = "Experience Master",
                    description = "Earn 1,000,000 experience total",
                    type = AchievementType.Experience,
                    rarity = AchievementRarity.Rare,
                    targetValue = 1000000,
                    multiplierReward = 0.05f,
                    rewardDescription = "+5% Experience Gain"
                },
                
                // Level Achievements
                new Achievement
                {
                    id = "level_10",
                    name = "Level 10",
                    description = "Reach player level 10",
                    type = AchievementType.Level,
                    rarity = AchievementRarity.Common,
                    targetValue = 10,
                    moneyReward = 500,
                    expReward = 500
                },
                new Achievement
                {
                    id = "level_50",
                    name = "Experienced Developer",
                    description = "Reach player level 50",
                    type = AchievementType.Level,
                    rarity = AchievementRarity.Uncommon,
                    targetValue = 50,
                    moneyReward = 5000,
                    expReward = 5000
                },
                new Achievement
                {
                    id = "level_100",
                    name = "Master Developer",
                    description = "Reach player level 100",
                    type = AchievementType.Level,
                    rarity = AchievementRarity.Rare,
                    targetValue = 100,
                    multiplierReward = 0.2f,
                    rewardDescription = "+20% All Income"
                },
                
                // Stage Achievements
                new Achievement
                {
                    id = "stage_2",
                    name = "Mobile Developer",
                    description = "Reach Stage 2",
                    type = AchievementType.Stage,
                    rarity = AchievementRarity.Common,
                    targetValue = 2,
                    moneyReward = 1000,
                    expReward = 2000
                },
                new Achievement
                {
                    id = "stage_5",
                    name = "AI Pioneer",
                    description = "Reach Stage 5",
                    type = AchievementType.Stage,
                    rarity = AchievementRarity.Uncommon,
                    targetValue = 5,
                    moneyReward = 10000,
                    expReward = 20000
                },
                new Achievement
                {
                    id = "stage_10",
                    name = "Time Lord",
                    description = "Complete all 10 stages",
                    type = AchievementType.Stage,
                    rarity = AchievementRarity.Legendary,
                    targetValue = 10,
                    multiplierReward = 0.5f,
                    rewardDescription = "+50% All Income Forever"
                },
                
                // Project Achievements
                new Achievement
                {
                    id = "project_first",
                    name = "First Project",
                    description = "Complete your first project",
                    type = AchievementType.Project,
                    rarity = AchievementRarity.Common,
                    targetValue = 1,
                    moneyReward = 500
                },
                new Achievement
                {
                    id = "project_10",
                    name = "Project Manager",
                    description = "Complete 10 projects",
                    type = AchievementType.Project,
                    rarity = AchievementRarity.Uncommon,
                    targetValue = 10,
                    moneyReward = 5000,
                    expReward = 2500
                },
                new Achievement
                {
                    id = "project_100",
                    name = "Project Legend",
                    description = "Complete 100 projects",
                    type = AchievementType.Project,
                    rarity = AchievementRarity.Rare,
                    targetValue = 100,
                    multiplierReward = 0.15f,
                    rewardDescription = "+15% Project Rewards"
                },
                
                // Upgrade Achievements
                new Achievement
                {
                    id = "upgrade_first",
                    name = "First Upgrade",
                    description = "Purchase your first upgrade",
                    type = AchievementType.Upgrade,
                    rarity = AchievementRarity.Common,
                    targetValue = 1,
                    expReward = 100
                },
                new Achievement
                {
                    id = "upgrade_25",
                    name = "Upgrade Enthusiast",
                    description = "Purchase 25 upgrades",
                    type = AchievementType.Upgrade,
                    rarity = AchievementRarity.Uncommon,
                    targetValue = 25,
                    moneyReward = 2500,
                    expReward = 2500
                },
                
                // Play Time Achievements
                new Achievement
                {
                    id = "playtime_1h",
                    name = "Dedicated Player",
                    description = "Play for 1 hour total",
                    type = AchievementType.PlayTime,
                    rarity = AchievementRarity.Common,
                    targetValue = 3600,
                    expReward = 1000
                },
                new Achievement
                {
                    id = "playtime_10h",
                    name = "Addicted",
                    description = "Play for 10 hours total",
                    type = AchievementType.PlayTime,
                    rarity = AchievementRarity.Uncommon,
                    targetValue = 36000,
                    moneyReward = 10000,
                    expReward = 10000
                },
                
                // Special Achievements
                new Achievement
                {
                    id = "special_speedrun",
                    name = "Speed Runner",
                    description = "Reach Stage 5 in under 1 hour",
                    type = AchievementType.Special,
                    rarity = AchievementRarity.Epic,
                    multiplierReward = 0.25f,
                    rewardDescription = "+25% Click Power"
                },
                new Achievement
                {
                    id = "special_perfect_day",
                    name = "Perfect Day",
                    description = "Play for 7 consecutive days",
                    type = AchievementType.Special,
                    rarity = AchievementRarity.Rare,
                    moneyReward = 50000,
                    expReward = 50000
                }
            };
        }
        
        private void SubscribeToEvents()
        {
            // Subscribe to game events for achievement tracking
            GameEvents.OnClickPerformed += OnClickPerformed;
            GameEvents.OnMoneyChanged += OnMoneyChanged;
            GameEvents.OnExperienceChanged += OnExperienceChanged;
            GameEvents.OnLevelUp += OnLevelUp;
            GameEvents.OnStageUnlocked += OnStageUnlocked;
            GameEvents.OnUpgradePurchased += OnUpgradePurchased;
            GameEvents.OnProjectCompleted += OnProjectCompleted;
            
            // Subscribe to statistics events
            var stats = StatisticsSystem.Instance;
            if (stats != null)
            {
                stats.OnMilestoneReached += OnMilestoneReached;
            }
        }
        
        #region Achievement Checking
        
        private void CheckAllAchievements()
        {
            foreach (var achievement in allAchievements)
            {
                if (!achievement.isUnlocked)
                {
                    CheckAchievement(achievement);
                }
            }
        }
        
        private void CheckAchievement(Achievement achievement)
        {
            if (achievement.isUnlocked) return;
            
            bool shouldUnlock = false;
            
            // Check based on achievement type
            switch (achievement.type)
            {
                case AchievementType.Click:
                    var stats = StatisticsSystem.Instance?.Statistics;
                    if (stats != null)
                    {
                        achievement.currentValue = stats.totalClicks;
                        shouldUnlock = stats.totalClicks >= achievement.targetValue;
                    }
                    break;
                    
                case AchievementType.Money:
                    if (achievement.id == "money_unlock")
                    {
                        shouldUnlock = GameModel.Instance?.IsMoneyUnlocked ?? false;
                    }
                    else
                    {
                        stats = StatisticsSystem.Instance?.Statistics;
                        if (stats != null)
                        {
                            achievement.currentValue = stats.totalMoneyEarned;
                            shouldUnlock = stats.totalMoneyEarned >= achievement.targetValue;
                        }
                    }
                    break;
                    
                case AchievementType.Experience:
                    stats = StatisticsSystem.Instance?.Statistics;
                    if (stats != null)
                    {
                        achievement.currentValue = stats.totalExperienceEarned;
                        shouldUnlock = stats.totalExperienceEarned >= achievement.targetValue;
                    }
                    break;
                    
                case AchievementType.Level:
                    var level = GameModel.Instance?.PlayerLevel ?? 0;
                    achievement.currentValue = level;
                    shouldUnlock = level >= achievement.targetValue;
                    break;
                    
                case AchievementType.Stage:
                    var stage = SaveManager.Instance?.CurrentGameData?.currentStage ?? 0;
                    achievement.currentValue = stage;
                    shouldUnlock = stage >= achievement.targetValue;
                    break;
                    
                case AchievementType.Project:
                    stats = StatisticsSystem.Instance?.Statistics;
                    if (stats != null)
                    {
                        achievement.currentValue = stats.totalProjectsCompleted;
                        shouldUnlock = stats.totalProjectsCompleted >= achievement.targetValue;
                    }
                    break;
                    
                case AchievementType.Upgrade:
                    stats = StatisticsSystem.Instance?.Statistics;
                    if (stats != null)
                    {
                        achievement.currentValue = stats.totalUpgradesPurchased;
                        shouldUnlock = stats.totalUpgradesPurchased >= achievement.targetValue;
                    }
                    break;
                    
                case AchievementType.PlayTime:
                    stats = StatisticsSystem.Instance?.Statistics;
                    if (stats != null)
                    {
                        achievement.currentValue = (long)stats.totalPlayTime;
                        shouldUnlock = stats.totalPlayTime >= achievement.targetValue;
                    }
                    break;
                    
                case AchievementType.Special:
                    shouldUnlock = CheckSpecialAchievement(achievement);
                    break;
            }
            
            if (shouldUnlock)
            {
                UnlockAchievement(achievement);
            }
            else if (achievement.isProgressBased)
            {
                OnAchievementProgress?.Invoke(achievement);
            }
        }
        
        private bool CheckSpecialAchievement(Achievement achievement)
        {
            switch (achievement.id)
            {
                case "special_speedrun":
                    var stats = StatisticsSystem.Instance?.Statistics;
                    var stage = SaveManager.Instance?.CurrentGameData?.currentStage ?? 0;
                    if (stats != null && stage >= 5)
                    {
                        return stats.totalPlayTime < 3600f; // Under 1 hour
                    }
                    break;
                    
                case "special_perfect_day":
                    stats = StatisticsSystem.Instance?.Statistics;
                    if (stats != null)
                    {
                        return stats.consecutiveDaysPlayed >= 7;
                    }
                    break;
            }
            
            return false;
        }
        
        #endregion
        
        #region Achievement Unlocking
        
        private void UnlockAchievement(Achievement achievement)
        {
            if (achievement.isUnlocked) return;
            
            achievement.isUnlocked = true;
            achievement.unlockedDate = DateTime.Now;
            unlockedAchievements.Add(achievement);
            
            // Apply rewards
            ApplyAchievementRewards(achievement);
            
            // Show notification
            if (showUnlockNotifications)
            {
                ShowAchievementNotification(achievement);
            }
            
            // Fire event
            OnAchievementUnlocked?.Invoke(achievement);
            
            // Save progress
            SaveAchievementProgress();
            
            Debug.Log($"[AchievementSystem] Unlocked: {achievement.name}");
        }
        
        private void ApplyAchievementRewards(Achievement achievement)
        {
            var gameModel = GameModel.Instance;
            if (gameModel == null) return;
            
            // Apply money reward
            if (achievement.moneyReward > 0)
            {
                gameModel.AddMoney(achievement.moneyReward);
            }
            
            // Apply experience reward
            if (achievement.expReward > 0)
            {
                gameModel.AddExperience(achievement.expReward);
            }
            
            // Apply multiplier reward
            if (achievement.multiplierReward > 0)
            {
                // This would need to be implemented in GameModel
                // For now, we'll just log it
                Debug.Log($"[AchievementSystem] Multiplier reward: {achievement.rewardDescription}");
            }
        }
        
        private void ShowAchievementNotification(Achievement achievement)
        {
            string title = "Achievement Unlocked!";
            string message = $"{achievement.name}\n{achievement.description}";
            
            if (achievement.moneyReward > 0 || achievement.expReward > 0 || achievement.multiplierReward > 0)
            {
                message += "\n\nRewards:";
                
                if (achievement.moneyReward > 0)
                    message += $"\n💰 {NumberFormatter.FormatCurrency(achievement.moneyReward)}";
                    
                if (achievement.expReward > 0)
                    message += $"\n⭐ {NumberFormatter.Format(achievement.expReward)}";
                    
                if (!string.IsNullOrEmpty(achievement.rewardDescription))
                    message += $"\n🎯 {achievement.rewardDescription}";
            }
            
            GameEvents.InvokeNotificationShown(title, message);
        }
        
        #endregion
        
        #region Event Handlers
        
        private void OnClickPerformed(float moneyGained, float expGained)
        {
            CheckAchievementsByType(AchievementType.Click);
        }
        
        private void OnMoneyChanged(long money)
        {
            CheckAchievementsByType(AchievementType.Money);
        }
        
        private void OnExperienceChanged(long experience)
        {
            CheckAchievementsByType(AchievementType.Experience);
        }
        
        private void OnLevelUp(int level)
        {
            CheckAchievementsByType(AchievementType.Level);
        }
        
        private void OnStageUnlocked(int stage)
        {
            CheckAchievementsByType(AchievementType.Stage);
            CheckAchievementsByType(AchievementType.Special);
        }
        
        private void OnUpgradePurchased(Data.ScriptableObjects.UpgradeData upgrade)
        {
            CheckAchievementsByType(AchievementType.Upgrade);
        }
        
        private void OnProjectCompleted(long reward)
        {
            CheckAchievementsByType(AchievementType.Project);
        }
        
        private void OnMilestoneReached(string milestoneName, long value)
        {
            // Check all achievements when milestones are reached
            CheckAllAchievements();
        }
        
        private void CheckAchievementsByType(AchievementType type)
        {
            var achievementsToCheck = allAchievements.Where(a => !a.isUnlocked && a.type == type).ToList();
            foreach (var achievement in achievementsToCheck)
            {
                CheckAchievement(achievement);
            }
        }
        
        #endregion
        
        #region Public Methods
        
        public List<Achievement> GetAllAchievements()
        {
            return new List<Achievement>(allAchievements);
        }
        
        public List<Achievement> GetUnlockedAchievements()
        {
            return new List<Achievement>(unlockedAchievements);
        }
        
        public List<Achievement> GetLockedAchievements()
        {
            return allAchievements.Where(a => !a.isUnlocked).ToList();
        }
        
        public List<Achievement> GetAchievementsByType(AchievementType type)
        {
            return allAchievements.Where(a => a.type == type).ToList();
        }
        
        public List<Achievement> GetAchievementsByRarity(AchievementRarity rarity)
        {
            return allAchievements.Where(a => a.rarity == rarity).ToList();
        }
        
        public Achievement GetAchievement(string id)
        {
            achievementDict.TryGetValue(id, out Achievement achievement);
            return achievement;
        }
        
        public float GetCompletionPercentage()
        {
            if (allAchievements.Count == 0) return 0f;
            return (float)unlockedAchievements.Count / allAchievements.Count * 100f;
        }
        
        public int GetTotalAchievementPoints()
        {
            int points = 0;
            foreach (var achievement in unlockedAchievements)
            {
                switch (achievement.rarity)
                {
                    case AchievementRarity.Common: points += 10; break;
                    case AchievementRarity.Uncommon: points += 25; break;
                    case AchievementRarity.Rare: points += 50; break;
                    case AchievementRarity.Epic: points += 100; break;
                    case AchievementRarity.Legendary: points += 200; break;
                }
            }
            return points;
        }
        
        #endregion
        
        #region Save/Load
        
        private void SaveAchievementProgress()
        {
            var saveData = new AchievementSaveData
            {
                unlockedAchievementIds = unlockedAchievements.Select(a => a.id).ToList(),
                achievementProgress = new Dictionary<string, long>()
            };
            
            foreach (var achievement in allAchievements)
            {
                if (achievement.isProgressBased && !achievement.isUnlocked)
                {
                    saveData.achievementProgress[achievement.id] = achievement.currentValue;
                }
            }
            
            string json = JsonUtility.ToJson(saveData, true);
            PlayerPrefs.SetString("AchievementProgress", json);
            PlayerPrefs.Save();
        }
        
        private void LoadAchievementProgress()
        {
            if (!PlayerPrefs.HasKey("AchievementProgress")) return;
            
            string json = PlayerPrefs.GetString("AchievementProgress");
            var saveData = JsonUtility.FromJson<AchievementSaveData>(json);
            
            if (saveData != null)
            {
                // Restore unlocked achievements
                if (saveData.unlockedAchievementIds != null)
                {
                    foreach (string id in saveData.unlockedAchievementIds)
                    {
                        var achievement = GetAchievement(id);
                        if (achievement != null)
                        {
                            achievement.isUnlocked = true;
                            if (!unlockedAchievements.Contains(achievement))
                            {
                                unlockedAchievements.Add(achievement);
                            }
                        }
                    }
                }
                
                // Restore progress
                if (saveData.achievementProgress != null)
                {
                    foreach (var kvp in saveData.achievementProgress)
                    {
                        var achievement = GetAchievement(kvp.Key);
                        if (achievement != null && !achievement.isUnlocked)
                        {
                            achievement.currentValue = kvp.Value;
                        }
                    }
                }
            }
            
            OnAchievementsLoaded?.Invoke(allAchievements);
        }
        
        [System.Serializable]
        private class AchievementSaveData
        {
            public List<string> unlockedAchievementIds;
            public Dictionary<string, long> achievementProgress;
        }
        
        #endregion
        
        private void OnDestroy()
        {
            // Unsubscribe from events
            GameEvents.OnClickPerformed -= OnClickPerformed;
            GameEvents.OnMoneyChanged -= OnMoneyChanged;
            GameEvents.OnExperienceChanged -= OnExperienceChanged;
            GameEvents.OnLevelUp -= OnLevelUp;
            GameEvents.OnStageUnlocked -= OnStageUnlocked;
            GameEvents.OnUpgradePurchased -= OnUpgradePurchased;
            GameEvents.OnProjectCompleted -= OnProjectCompleted;
            
            var stats = StatisticsSystem.Instance;
            if (stats != null)
            {
                stats.OnMilestoneReached -= OnMilestoneReached;
            }
            
            SaveAchievementProgress();
        }
    }
}