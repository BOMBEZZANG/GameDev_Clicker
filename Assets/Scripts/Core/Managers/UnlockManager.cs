using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GameDevClicker.Core.Patterns;
using GameDevClicker.Game.Models;

namespace GameDevClicker.Core.Managers
{
    public class UnlockManager : Singleton<UnlockManager>
    {
        [Header("Unlock Configuration")]
        [SerializeField] private UnlockMilestone[] allUnlockMilestones;
        [SerializeField] private bool showUnlockPopups = true;
        [SerializeField] private float popupDisplayTime = 3f;
        
        private HashSet<string> _unlockedFeatures;
        private Dictionary<UnlockType, List<UnlockMilestone>> _milestonesByType;

        public event Action<UnlockMilestone> OnMilestoneUnlocked;
        public event Action<UnlockType> OnUnlockTypeAvailable;

        protected override void Awake()
        {
            base.Awake();
            InitializeUnlockSystem();
        }

        private void Start()
        {
            SubscribeToEvents();
            LoadUnlockedFeatures();
            CheckAllUnlocks();
        }

        private void InitializeUnlockSystem()
        {
            _unlockedFeatures = new HashSet<string>();
            _milestonesByType = new Dictionary<UnlockType, List<UnlockMilestone>>();

            if (allUnlockMilestones == null)
            {
                CreateDefaultUnlockMilestones();
            }

            ProcessUnlockMilestones();
            Debug.Log($"[UnlockManager] Initialized with {allUnlockMilestones.Length} unlock milestones");
        }

        private void CreateDefaultUnlockMilestones()
        {
            allUnlockMilestones = new UnlockMilestone[]
            {
                new UnlockMilestone
                {
                    unlockId = "money_generation",
                    unlockName = "Money Generation",
                    unlockDescription = "Start earning money from your development work!",
                    type = UnlockType.MoneyGeneration,
                    requiredLevel = 10,
                    requiredStage = 1
                },
                new UnlockMilestone
                {
                    unlockId = "project_system",
                    unlockName = "Project System",
                    unlockDescription = "Complete projects for big money rewards!",
                    type = UnlockType.ProjectSystem,
                    requiredLevel = 1,
                    requiredStage = 2
                },
                new UnlockMilestone
                {
                    unlockId = "ad_revenue",
                    unlockName = "Advertisement Revenue",
                    unlockDescription = "Earn passive income from ads in your games!",
                    type = UnlockType.AdRevenue,
                    requiredLevel = 15,
                    requiredStage = 2
                },
                new UnlockMilestone
                {
                    unlockId = "investment_events",
                    unlockName = "Investment Opportunities",
                    unlockDescription = "Investors are interested in funding your projects!",
                    type = UnlockType.InvestmentEvents,
                    requiredLevel = 25,
                    requiredStage = 3
                },
                new UnlockMilestone
                {
                    unlockId = "team_management",
                    unlockName = "Team Management",
                    unlockDescription = "Hire and manage a team of developers!",
                    type = UnlockType.TeamManagement,
                    requiredLevel = 30,
                    requiredStage = 3
                }
            };
        }

        private void ProcessUnlockMilestones()
        {
            foreach (var milestone in allUnlockMilestones)
            {
                if (!_milestonesByType.ContainsKey(milestone.type))
                {
                    _milestonesByType[milestone.type] = new List<UnlockMilestone>();
                }
                _milestonesByType[milestone.type].Add(milestone);
            }
        }

        private void SubscribeToEvents()
        {
            GameEvents.OnLevelUp += OnPlayerLevelUp;
            GameEvents.OnStageUnlocked += OnStageUnlocked;
            GameEvents.OnGameLoaded += LoadUnlockedFeatures;
        }

        private void LoadUnlockedFeatures()
        {
            var gameData = SaveManager.Instance.CurrentGameData;
            if (gameData?.unlockedFeatures != null)
            {
                _unlockedFeatures = new HashSet<string>(gameData.unlockedFeatures);
            }
        }

        private void SaveUnlockedFeatures()
        {
            var gameData = SaveManager.Instance.CurrentGameData;
            if (gameData != null)
            {
                gameData.unlockedFeatures = new HashSet<string>(_unlockedFeatures);
                SaveManager.Instance.MarkDirty();
            }
        }

        #region Unlock Checking

        public void CheckAllUnlocks()
        {
            var gameData = SaveManager.Instance.CurrentGameData;
            if (gameData == null) return;

            foreach (var milestone in allUnlockMilestones)
            {
                if (!IsUnlocked(milestone.unlockId) && 
                    ShouldUnlockMilestone(milestone, gameData.playerLevel, gameData.currentStage))
                {
                    UnlockMilestone(milestone);
                }
            }
        }

        private bool ShouldUnlockMilestone(UnlockMilestone milestone, int playerLevel, int currentStage)
        {
            if (playerLevel < milestone.requiredLevel) return false;
            if (currentStage < milestone.requiredStage) return false;

            // Check additional requirements
            if (milestone.requiredUnlocks != null && milestone.requiredUnlocks.Length > 0)
            {
                foreach (string requiredUnlock in milestone.requiredUnlocks)
                {
                    if (!IsUnlocked(requiredUnlock)) return false;
                }
            }

            return true;
        }

        private void UnlockMilestone(UnlockMilestone milestone)
        {
            if (IsUnlocked(milestone.unlockId)) return;

            _unlockedFeatures.Add(milestone.unlockId);
            SaveUnlockedFeatures();

            // Execute custom unlock action
            milestone.onUnlock?.Invoke();

            // Trigger events
            OnMilestoneUnlocked?.Invoke(milestone);
            GameEvents.InvokeFeatureUnlocked(milestone.unlockId);

            // Show popup notification
            if (showUnlockPopups)
            {
                ShowUnlockPopup(milestone);
            }

            Debug.Log($"[UnlockManager] Unlocked: {milestone.unlockName}");

            // Check if this unlock type is now available
            OnUnlockTypeAvailable?.Invoke(milestone.type);
        }

        #endregion

        #region Event Handlers

        private void OnPlayerLevelUp(int newLevel)
        {
            CheckAllUnlocks();

            // Special handling for money unlock at level 10
            if (newLevel == 10 && !IsUnlocked("money_generation"))
            {
                var moneyUnlock = GetUnlockMilestone("money_generation");
                if (moneyUnlock != null)
                {
                    UnlockMilestone(moneyUnlock);
                }
            }
        }

        private void OnStageUnlocked(int stage)
        {
            CheckAllUnlocks();
        }

        #endregion

        #region Query Methods

        public bool IsUnlocked(string unlockId)
        {
            return _unlockedFeatures.Contains(unlockId);
        }

        public bool IsUnlocked(UnlockType unlockType)
        {
            if (!_milestonesByType.ContainsKey(unlockType)) return false;
            
            return _milestonesByType[unlockType].Any(milestone => IsUnlocked(milestone.unlockId));
        }

        public UnlockMilestone GetUnlockMilestone(string unlockId)
        {
            return allUnlockMilestones?.FirstOrDefault(m => m.unlockId == unlockId);
        }

        public List<UnlockMilestone> GetUnlockedMilestones()
        {
            return allUnlockMilestones?.Where(m => IsUnlocked(m.unlockId)).ToList() ?? new List<UnlockMilestone>();
        }

        public List<UnlockMilestone> GetPendingMilestones()
        {
            var gameData = SaveManager.Instance.CurrentGameData;
            if (gameData == null) return new List<UnlockMilestone>();

            return allUnlockMilestones?.Where(m => 
                !IsUnlocked(m.unlockId) && 
                CanUnlockSoon(m, gameData.playerLevel, gameData.currentStage)
            ).ToList() ?? new List<UnlockMilestone>();
        }

        private bool CanUnlockSoon(UnlockMilestone milestone, int playerLevel, int currentStage)
        {
            // Consider "soon" as within 5 levels or 1 stage
            return (playerLevel >= milestone.requiredLevel - 5) || 
                   (currentStage >= milestone.requiredStage - 1);
        }

        public List<UnlockMilestone> GetMilestonesByType(UnlockType type)
        {
            return _milestonesByType.ContainsKey(type) 
                ? new List<UnlockMilestone>(_milestonesByType[type])
                : new List<UnlockMilestone>();
        }

        #endregion

        #region UI Integration

        private void ShowUnlockPopup(UnlockMilestone milestone)
        {
            GameEvents.InvokeNotificationShown(
                $"🎉 {milestone.unlockName} Unlocked!", 
                milestone.unlockDescription
            );
        }

        public string GetUnlockDescription(string unlockId)
        {
            var milestone = GetUnlockMilestone(unlockId);
            return milestone?.unlockDescription ?? "Unknown unlock";
        }

        public string GetUnlockProgress(string unlockId)
        {
            var milestone = GetUnlockMilestone(unlockId);
            if (milestone == null) return "";

            if (IsUnlocked(unlockId)) return "✅ Unlocked";

            var gameData = SaveManager.Instance.CurrentGameData;
            if (gameData == null) return "";

            List<string> requirements = new List<string>();

            if (gameData.playerLevel < milestone.requiredLevel)
                requirements.Add($"Level {milestone.requiredLevel} (currently {gameData.playerLevel})");

            if (gameData.currentStage < milestone.requiredStage)
                requirements.Add($"Stage {milestone.requiredStage} (currently {gameData.currentStage})");

            return requirements.Count > 0 ? $"Requires: {string.Join(", ", requirements)}" : "Ready to unlock!";
        }

        #endregion

        #region Debug Methods

        public void DebugUnlockFeature(string unlockId)
        {
            var milestone = GetUnlockMilestone(unlockId);
            if (milestone != null)
            {
                UnlockMilestone(milestone);
            }
            else
            {
                Debug.LogWarning($"[UnlockManager] Unknown unlock ID: {unlockId}");
            }
        }

        public void DebugPrintUnlockStatus()
        {
            Debug.Log($"[UnlockManager] Unlocked Features: {string.Join(", ", _unlockedFeatures)}");
            
            foreach (var milestone in allUnlockMilestones)
            {
                string status = IsUnlocked(milestone.unlockId) ? "UNLOCKED" : "LOCKED";
                Debug.Log($"[UnlockManager] {milestone.unlockName}: {status}");
            }
        }

        #endregion

        protected override void OnDestroy()
        {
            GameEvents.OnLevelUp -= OnPlayerLevelUp;
            GameEvents.OnStageUnlocked -= OnStageUnlocked;
            GameEvents.OnGameLoaded -= LoadUnlockedFeatures;
            base.OnDestroy();
        }
    }

    [System.Serializable]
    public class UnlockMilestone
    {
        [Header("Basic Information")]
        public string unlockId;
        public string unlockName;
        [TextArea(2, 4)]
        public string unlockDescription;
        public UnlockType type;

        [Header("Requirements")]
        public int requiredLevel = 1;
        public int requiredStage = 1;
        public string[] requiredUnlocks;

        [Header("Unlock Action")]
        public UnityEngine.Events.UnityEvent onUnlockEvent;
        
        [System.NonSerialized]
        public System.Action onUnlock;

        public void TriggerUnlock()
        {
            onUnlockEvent?.Invoke();
            onUnlock?.Invoke();
        }
    }

    public enum UnlockType
    {
        MoneyGeneration,    // Level 10
        ProjectSystem,      // Stage 2
        AdRevenue,         // Stage 2
        InvestmentEvents,  // Stage 3
        TeamManagement,    // Stage 3
        VirtualReality,    // Stage 4
        ArtificialIntelligence, // Stage 5
        Robotics,          // Stage 6
        SpaceTechnology,   // Stage 7
        Starships,         // Stage 8
        BlackHoles,        // Stage 9
        TimeMachine        // Stage 10
    }
}