using System;
using UnityEngine;
using GameDevClicker.Core.Patterns;
using GameDevClicker.Core.Managers;
using GameDevClicker.Game.Models;
using GameDevClicker.Core.Utilities;

namespace GameDevClicker.Game.Systems
{
    [System.Serializable]
    public class OfflineProgressionData
    {
        public DateTime lastOnlineTime;
        public float offlineSeconds;
        public long offlineMoneyEarned;
        public long offlineExpEarned;
        public int offlineProjectsCompleted;
        public bool hasOfflineProgress;
    }

    public class OfflineProgressionSystem : Singleton<OfflineProgressionSystem>
    {
        [Header("Offline Settings")]
        [SerializeField] private bool enableOfflineProgression = true;
        [SerializeField] private float offlineEfficiency = 0.5f; // 50% efficiency when offline
        [SerializeField] private float maxOfflineHours = 24f; // Maximum 24 hours of offline progress
        [SerializeField] private float minOfflineMinutes = 1f; // Minimum 1 minute to count as offline
        
        [Header("UI Settings")]
        [SerializeField] private bool showOfflineReportOnReturn = true;
        [SerializeField] private float reportDisplayDuration = 5f;
        
        private OfflineProgressionData offlineData;
        
        // Events
        public event Action<OfflineProgressionData> OnOfflineProgressCalculated;
        public event Action<string, string> OnOfflineReportReady;
        
        protected override void Awake()
        {
            base.Awake();
            offlineData = new OfflineProgressionData();
        }
        
        private void Start()
        {
            // Check for offline progress when game starts
            if (enableOfflineProgression && SaveManager.Instance.HasSavedGame())
            {
                CalculateOfflineProgress();
            }
        }
        
        public void CalculateOfflineProgress()
        {
            if (!enableOfflineProgression) return;
            
            var saveData = SaveManager.Instance.CurrentGameData;
            if (saveData == null) return;
            
            DateTime lastSaveTime = saveData.lastSaveTime;
            DateTime currentTime = DateTime.Now;
            
            // Calculate offline duration
            TimeSpan offlineTime = currentTime - lastSaveTime;
            float offlineSeconds = (float)offlineTime.TotalSeconds;
            
            // Check minimum offline time
            if (offlineSeconds < minOfflineMinutes * 60f)
            {
                Debug.Log($"[OfflineProgression] Not enough offline time: {offlineSeconds} seconds");
                return;
            }
            
            // Cap offline time to maximum
            float maxOfflineSeconds = maxOfflineHours * 3600f;
            offlineSeconds = Mathf.Min(offlineSeconds, maxOfflineSeconds);
            
            // Store offline data
            offlineData.lastOnlineTime = lastSaveTime;
            offlineData.offlineSeconds = offlineSeconds;
            offlineData.hasOfflineProgress = true;
            
            // Calculate offline earnings based on auto income
            CalculateOfflineEarnings(offlineSeconds);
            
            // Calculate offline project completions
            CalculateOfflineProjects(offlineSeconds);
            
            // Apply offline progress
            ApplyOfflineProgress();
            
            // Trigger events
            OnOfflineProgressCalculated?.Invoke(offlineData);
            
            if (showOfflineReportOnReturn)
            {
                ShowOfflineReport();
            }
        }
        
        private void CalculateOfflineEarnings(float offlineSeconds)
        {
            var gameModel = GameModel.Instance;
            if (gameModel == null) return;
            
            // Calculate offline money (only if unlocked)
            if (gameModel.IsMoneyUnlocked && gameModel.AutoMoney > 0)
            {
                float offlineMoneyRate = gameModel.AutoMoney * offlineEfficiency;
                offlineData.offlineMoneyEarned = (long)(offlineMoneyRate * offlineSeconds);
            }
            else
            {
                offlineData.offlineMoneyEarned = 0;
            }
            
            // Calculate offline experience
            if (gameModel.AutoExp > 0)
            {
                float offlineExpRate = gameModel.AutoExp * offlineEfficiency;
                offlineData.offlineExpEarned = (long)(offlineExpRate * offlineSeconds);
            }
            else
            {
                offlineData.offlineExpEarned = 0;
            }
        }
        
        private void CalculateOfflineProjects(float offlineSeconds)
        {
            var projectSystem = ProjectSystem.Instance;
            if (projectSystem == null || !projectSystem.IsUnlocked) 
            {
                offlineData.offlineProjectsCompleted = 0;
                return;
            }
            
            // Calculate how much exp was generated offline
            long offlineExp = offlineData.offlineExpEarned;
            if (offlineExp <= 0) 
            {
                offlineData.offlineProjectsCompleted = 0;
                return;
            }
            
            // Calculate project completions
            float currentProgress = projectSystem.CurrentProgress;
            float requirement = projectSystem.CurrentRequirement;
            float totalProgress = currentProgress + offlineExp;
            
            int projectsCompleted = 0;
            while (totalProgress >= requirement)
            {
                projectsCompleted++;
                totalProgress -= requirement;
                requirement *= 1.5f; // Project requirements increase
            }
            
            offlineData.offlineProjectsCompleted = projectsCompleted;
        }
        
        private void ApplyOfflineProgress()
        {
            if (!offlineData.hasOfflineProgress) return;
            
            var gameModel = GameModel.Instance;
            if (gameModel == null) return;
            
            // Apply offline money
            if (offlineData.offlineMoneyEarned > 0)
            {
                gameModel.AddMoney(offlineData.offlineMoneyEarned);
                Debug.Log($"[OfflineProgression] Added offline money: {NumberFormatter.FormatCurrency(offlineData.offlineMoneyEarned)}");
            }
            
            // Apply offline experience
            if (offlineData.offlineExpEarned > 0)
            {
                gameModel.AddExperience(offlineData.offlineExpEarned);
                Debug.Log($"[OfflineProgression] Added offline experience: {NumberFormatter.Format(offlineData.offlineExpEarned)}");
            }
            
            // Apply project completions
            if (offlineData.offlineProjectsCompleted > 0)
            {
                var projectSystem = ProjectSystem.Instance;
                if (projectSystem != null && projectSystem.IsUnlocked)
                {
                    for (int i = 0; i < offlineData.offlineProjectsCompleted; i++)
                    {
                        long reward = projectSystem.CalculateProjectReward();
                        gameModel.AddMoney(reward);
                        
                        // Update project requirement
                        projectSystem.CompleteProjectOffline();
                    }
                }
                Debug.Log($"[OfflineProgression] Completed {offlineData.offlineProjectsCompleted} projects offline");
            }
            
            // Update statistics
            var stats = StatisticsSystem.Instance;
            if (stats != null)
            {
                stats.Statistics.totalMoneyEarned += offlineData.offlineMoneyEarned;
                stats.Statistics.totalExperienceEarned += offlineData.offlineExpEarned;
                stats.Statistics.totalProjectsCompleted += offlineData.offlineProjectsCompleted;
            }
        }
        
        private void ShowOfflineReport()
        {
            if (!offlineData.hasOfflineProgress) return;
            
            // Format offline time
            string timeString = FormatOfflineTime(offlineData.offlineSeconds);
            
            // Build report message
            string title = "Welcome Back!";
            string message = $"You were away for {timeString}\n\n";
            
            if (offlineData.offlineMoneyEarned > 0 || offlineData.offlineExpEarned > 0)
            {
                message += "Offline Earnings:\n";
                
                if (offlineData.offlineExpEarned > 0)
                {
                    message += $"⭐ {NumberFormatter.Format(offlineData.offlineExpEarned)} Experience\n";
                }
                
                if (offlineData.offlineMoneyEarned > 0)
                {
                    message += $"💰 {NumberFormatter.FormatCurrency(offlineData.offlineMoneyEarned)} Money\n";
                }
                
                if (offlineData.offlineProjectsCompleted > 0)
                {
                    message += $"\n🎮 {offlineData.offlineProjectsCompleted} Projects Completed";
                }
            }
            else
            {
                message += "Keep upgrading to earn offline income!";
            }
            
            OnOfflineReportReady?.Invoke(title, message);
            GameEvents.InvokeNotificationShown(title, message);
            
            // Clear offline data after showing report
            offlineData.hasOfflineProgress = false;
        }
        
        private string FormatOfflineTime(float seconds)
        {
            if (seconds < 60)
            {
                return $"{Mathf.RoundToInt(seconds)} seconds";
            }
            else if (seconds < 3600)
            {
                int minutes = Mathf.RoundToInt(seconds / 60);
                return $"{minutes} minute{(minutes != 1 ? "s" : "")}";
            }
            else if (seconds < 86400)
            {
                int hours = Mathf.RoundToInt(seconds / 3600);
                int minutes = Mathf.RoundToInt((seconds % 3600) / 60);
                
                if (minutes > 0)
                {
                    return $"{hours} hour{(hours != 1 ? "s" : "")} {minutes} minute{(minutes != 1 ? "s" : "")}";
                }
                else
                {
                    return $"{hours} hour{(hours != 1 ? "s" : "")}";
                }
            }
            else
            {
                int days = Mathf.RoundToInt(seconds / 86400);
                int hours = Mathf.RoundToInt((seconds % 86400) / 3600);
                
                if (hours > 0)
                {
                    return $"{days} day{(days != 1 ? "s" : "")} {hours} hour{(hours != 1 ? "s" : "")}";
                }
                else
                {
                    return $"{days} day{(days != 1 ? "s" : "")}";
                }
            }
        }
        
        public float GetOfflineEfficiencyPercentage()
        {
            return offlineEfficiency * 100f;
        }
        
        public void SetOfflineEfficiency(float efficiency)
        {
            offlineEfficiency = Mathf.Clamp01(efficiency);
        }
        
        public void SetMaxOfflineHours(float hours)
        {
            maxOfflineHours = Mathf.Clamp(hours, 1f, 168f); // Max 1 week
        }
        
        public bool HasPendingOfflineReport()
        {
            return offlineData.hasOfflineProgress;
        }
        
        public OfflineProgressionData GetOfflineData()
        {
            return offlineData;
        }
    }
}