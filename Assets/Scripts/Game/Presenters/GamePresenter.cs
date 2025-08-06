using UnityEngine;
using GameDevClicker.Core.Patterns;
using GameDevClicker.Core.Managers;
using GameDevClicker.Game.Models;
using GameDevClicker.Game.Views;
using GameDevClicker.Game.Systems;
using GameDevClicker.Data.ScriptableObjects;
using System.Collections.Generic;

namespace GameDevClicker.Game.Presenters
{
    public class GamePresenter : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameViewUI gameViewUI;
        
        [Header("Configuration")]
        [SerializeField] private float uiUpdateInterval = 0.1f;
        [SerializeField] private float progressUpdateInterval = 0.5f;

        private GameModel _model;
        private float _lastUIUpdate;
        private float _lastProgressUpdate;

        private void Awake()
        {
            InitializePresenter();
        }

        private void Start()
        {
            SetupConnections();
            InitializeUI();
        }

        private void Update()
        {
            UpdateUI();
            UpdateProgress();
        }

        private void InitializePresenter()
        {
            if (gameViewUI == null)
            {
                gameViewUI = FindObjectOfType<GameViewUI>();
                if (gameViewUI == null)
                {
                    Debug.LogError("[GamePresenter] GameViewUI not found!");
                    return;
                }
            }

            _model = GameModel.Instance;
            if (_model == null)
            {
                Debug.LogError("[GamePresenter] GameModel instance not found!");
                return;
            }
        }

        private void SetupConnections()
        {
            // Connect View events to Presenter methods
            gameViewUI.OnClickZoneClicked += HandleClickZoneClick;
            gameViewUI.OnUpgradePurchaseRequested += HandleUpgradePurchase;
            gameViewUI.OnUpgradeTabChanged += HandleUpgradeTabChanged;

            // Subscribe to game events for UI updates
            SubscribeToGameEvents();

            Debug.Log("[GamePresenter] Connections established successfully");
        }

        private void SubscribeToGameEvents()
        {
            GameEvents.OnMoneyChanged += OnMoneyChanged;
            GameEvents.OnExperienceChanged += OnExperienceChanged;
            GameEvents.OnClickValueChanged += OnClickValueChanged;
            GameEvents.OnAutoIncomeChanged += OnAutoIncomeChanged;
            GameEvents.OnLevelUp += OnLevelUp;
            GameEvents.OnStageUnlocked += OnStageUnlocked;
            GameEvents.OnFeatureUnlocked += OnFeatureUnlocked;
            GameEvents.OnUpgradePurchased += OnUpgradePurchased;
            GameEvents.OnProjectCompleted += OnProjectCompleted;
            GameEvents.OnProjectProgressChanged += OnProjectProgressChanged;
            GameEvents.OnNotificationShown += OnNotificationShown;
        }

        private void InitializeUI()
        {
            // Initialize all UI elements with current game state
            RefreshAllUI();
            PopulateUpgradeLists();
        }

        private void RefreshAllUI()
        {
            // Update currency displays
            gameViewUI.UpdateMoneyDisplay(_model.Money);
            gameViewUI.UpdateExperienceDisplay(_model.Experience);
            gameViewUI.UpdateClickValues(_model.MoneyPerClick, _model.ExpPerClick);
            gameViewUI.UpdateAutoIncome(_model.AutoMoney, _model.AutoExp);
            gameViewUI.UpdatePlayerLevel(_model.PlayerLevel);
            gameViewUI.UpdateStageDisplay(_model.CurrentStage);

            // Update project system if unlocked
            if (ProjectSystem.Instance.IsUnlocked)
            {
                UpdateProjectUI();
            }
        }

        private void UpdateUI()
        {
            if (Time.time - _lastUIUpdate < uiUpdateInterval) return;
            
            // Update frequently changing elements
            gameViewUI.UpdateMoneyDisplay(_model.Money);
            gameViewUI.UpdateExperienceDisplay(_model.Experience);
            
            // Update upgrade buttons affordability
            UpdateUpgradeButtonStates();
            
            _lastUIUpdate = Time.time;
        }

        private void UpdateProgress()
        {
            if (Time.time - _lastProgressUpdate < progressUpdateInterval) return;
            
            // Calculate stage progress
            long currentStageExp = GetStageExperienceRequirement(_model.CurrentStage);
            long nextStageExp = GetStageExperienceRequirement(_model.CurrentStage + 1);
            
            if (nextStageExp > currentStageExp)
            {
                long expInCurrentStage = _model.Experience - currentStageExp;
                long expRequiredForNext = nextStageExp - currentStageExp;
                float progress = (float)expInCurrentStage / expRequiredForNext;
                gameViewUI.UpdateStageProgress(progress);
            }
            
            _lastProgressUpdate = Time.time;
        }

        #region Event Handlers

        private void HandleClickZoneClick()
        {
            Vector2 clickPosition = Input.mousePosition; // Could be enhanced with actual click position
            _model.PerformClick(clickPosition);
        }

        private void HandleUpgradePurchase(UpgradeData upgrade)
        {
            if (UpgradeManager.Instance != null)
            {
                bool success = UpgradeManager.Instance.PurchaseUpgrade(upgrade);
                if (success)
                {
                    Debug.Log($"[GamePresenter] Successfully purchased upgrade: {upgrade.upgradeName}");
                }
                else
                {
                    Debug.Log($"[GamePresenter] Failed to purchase upgrade: {upgrade.upgradeName}");
                }
            }
        }

        private void HandleUpgradeTabChanged(UpgradeData.UpgradeCategory category)
        {
            PopulateUpgradeList(category);
        }

        #endregion

        #region Game Event Handlers

        private void OnMoneyChanged(long money)
        {
            // UI update is handled in UpdateUI() for performance
        }

        private void OnExperienceChanged(long experience)
        {
            // Check if we should unlock a new stage
            CheckStageProgression(experience);
        }

        private void OnClickValueChanged(float moneyPerClick, float expPerClick)
        {
            gameViewUI.UpdateClickValues(moneyPerClick, expPerClick);
        }

        private void OnAutoIncomeChanged(float autoMoney, float autoExp)
        {
            gameViewUI.UpdateAutoIncome(autoMoney, autoExp);
        }

        private void OnLevelUp(int level)
        {
            gameViewUI.UpdatePlayerLevel(level);
        }

        private void OnStageUnlocked(int stage)
        {
            gameViewUI.UpdateStageDisplay(stage);
            PopulateUpgradeLists(); // Refresh upgrade lists as new upgrades may be available
        }

        private void OnFeatureUnlocked(string featureName)
        {
            Debug.Log($"[GamePresenter] Feature unlocked: {featureName}");
            
            switch (featureName)
            {
                case "money":
                    PopulateUpgradeLists(); // Refresh to show money-based upgrades
                    break;
                case "project_system":
                    UpdateProjectUI();
                    break;
            }
        }

        private void OnUpgradePurchased(UpgradeData upgrade)
        {
            PopulateUpgradeList(upgrade.category); // Refresh the specific category
        }

        private void OnProjectCompleted(long reward)
        {
            UpdateProjectUI();
        }

        private void OnProjectProgressChanged(float progress)
        {
            UpdateProjectUI();
        }

        private void OnNotificationShown(string title, string message)
        {
            gameViewUI.ShowNotificationPopup(title, message);
        }

        #endregion

        #region Upgrade System

        private void PopulateUpgradeLists()
        {
            PopulateUpgradeList(UpgradeData.UpgradeCategory.Skills);
            PopulateUpgradeList(UpgradeData.UpgradeCategory.Equipment);
            PopulateUpgradeList(UpgradeData.UpgradeCategory.Team);
        }

        private void PopulateUpgradeList(UpgradeData.UpgradeCategory category)
        {
            if (UpgradeManager.Instance == null) return;

            var upgrades = UpgradeManager.Instance.GetUpgradesByCategory(category);
            var availableUpgrades = new List<UpgradeData>();

            foreach (var upgrade in upgrades)
            {
                if (UpgradeManager.Instance.IsUpgradeUnlocked(upgrade))
                {
                    availableUpgrades.Add(upgrade);
                }
            }

            gameViewUI.PopulateUpgradeList(category, availableUpgrades);
        }

        private void UpdateUpgradeButtonStates()
        {
            if (UpgradeManager.Instance == null) return;

            // Get all visible upgrades and update their button states
            var allUpgrades = new List<UpgradeData>();
            allUpgrades.AddRange(UpgradeManager.Instance.GetUpgradesByCategory(UpgradeData.UpgradeCategory.Skills));
            allUpgrades.AddRange(UpgradeManager.Instance.GetUpgradesByCategory(UpgradeData.UpgradeCategory.Equipment));
            allUpgrades.AddRange(UpgradeManager.Instance.GetUpgradesByCategory(UpgradeData.UpgradeCategory.Team));

            foreach (var upgrade in allUpgrades)
            {
                if (UpgradeManager.Instance.IsUpgradeUnlocked(upgrade))
                {
                    gameViewUI.UpdateUpgradeElement(upgrade);
                }
            }
        }

        #endregion

        #region Project System

        private void UpdateProjectUI()
        {
            if (ProjectSystem.Instance == null || !ProjectSystem.Instance.IsUnlocked) return;

            var currentProject = ProjectSystem.Instance.CurrentProjectType;
            if (currentProject != null)
            {
                long nextReward = CalculateNextProjectReward();
                gameViewUI.UpdateProjectInfo(
                    currentProject.projectName,
                    nextReward,
                    ProjectSystem.Instance.CurrentProgress,
                    ProjectSystem.Instance.CurrentRequirement
                );
            }
        }

        private long CalculateNextProjectReward()
        {
            // This should ideally be a method in ProjectSystem
            // For now, we'll calculate it here
            return 1000; // Placeholder
        }

        #endregion

        #region Stage Progression

        private void CheckStageProgression(long experience)
        {
            int currentStage = _model.CurrentStage;
            long requiredExp = GetStageExperienceRequirement(currentStage + 1);

            if (experience >= requiredExp && currentStage < 10)
            {
                // Trigger stage unlock
                GameEvents.InvokeStageUnlocked(currentStage + 1);
                
                // Update model stage (this should be handled by a stage manager ideally)
                var gameData = SaveManager.Instance.CurrentGameData;
                if (gameData != null)
                {
                    gameData.currentStage = currentStage + 1;
                    SaveManager.Instance.MarkDirty();
                }
            }
        }

        private long GetStageExperienceRequirement(int stage)
        {
            // This should match the progression defined in BalanceSettings
            long[] stageRequirements = { 0, 1000, 15000, 225000, 3375000, 50625000, 759375000 };
            
            if (stage <= 0) return 0;
            if (stage - 1 >= stageRequirements.Length)
                return stageRequirements[stageRequirements.Length - 1] * (long)Mathf.Pow(15f, stage - stageRequirements.Length);
            
            return stageRequirements[stage - 1];
        }

        #endregion

        #region Utility Methods

        public void RefreshUI()
        {
            RefreshAllUI();
            PopulateUpgradeLists();
        }

        public void SetUIUpdateInterval(float interval)
        {
            uiUpdateInterval = Mathf.Max(0.01f, interval);
        }

        public void SetProgressUpdateInterval(float interval)
        {
            progressUpdateInterval = Mathf.Max(0.01f, interval);
        }

        #endregion

        private void OnDestroy()
        {
            // Unsubscribe from game events
            GameEvents.OnMoneyChanged -= OnMoneyChanged;
            GameEvents.OnExperienceChanged -= OnExperienceChanged;
            GameEvents.OnClickValueChanged -= OnClickValueChanged;
            GameEvents.OnAutoIncomeChanged -= OnAutoIncomeChanged;
            GameEvents.OnLevelUp -= OnLevelUp;
            GameEvents.OnStageUnlocked -= OnStageUnlocked;
            GameEvents.OnFeatureUnlocked -= OnFeatureUnlocked;
            GameEvents.OnUpgradePurchased -= OnUpgradePurchased;
            GameEvents.OnProjectCompleted -= OnProjectCompleted;
            GameEvents.OnProjectProgressChanged -= OnProjectProgressChanged;
            GameEvents.OnNotificationShown -= OnNotificationShown;
        }
    }
}