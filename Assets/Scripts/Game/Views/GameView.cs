using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using GameDevClicker.Core.Patterns;
using GameDevClicker.Core.Utilities;
using GameDevClicker.Data.ScriptableObjects;
using GameDevClicker.Game.Models;

namespace GameDevClicker.Game.Views
{
    public class GameView : MonoBehaviour
    {
        [Header("UI Configuration")]
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private VisualTreeAsset upgradeItemTemplate;
        [SerializeField] private float clickEffectDuration = 1f;
        [SerializeField] private float popupDisplayDuration = 3f;

        // UI Element References
        private VisualElement _root;
        
        // Header Elements
        private Label _stageNameLabel;
        private Label _moneyValueLabel;
        private Label _expValueLabel;
        private Label _stageValueLabel;
        private Label _playerLevelLabel;
        private Label _moneyIncomeLabel;
        private Label _expIncomeLabel;
        private Label _moneyLockText;
        private VisualElement _moneyContainer;
        
        // Progress Elements
        private Label _progressNextStageLabel;
        private Label _progressPercentLabel;
        private VisualElement _progressFill;
        
        // Click Zone
        private Button _clickZone;
        private Label _clickZoneIcon;
        private Label _clickZoneText;
        
        // Auto Income
        private Label _autoMoneyValue;
        private Label _autoExpValue;
        
        // Project Section
        private VisualElement _projectSection;
        private Label _projectName;
        private Label _projectRewardValue;
        private VisualElement _projectProgressFill;
        private Label _projectProgressText;
        
        // Upgrade System
        private Button _skillsTab;
        private Button _equipmentTab;
        private Button _teamTab;
        private VisualElement _skillsUpgrades;
        private VisualElement _equipmentUpgrades;
        private VisualElement _teamUpgrades;
        
        // Popup System
        private VisualElement _popupOverlay;
        private VisualElement _unlockPopup;
        private Label _popupTitle;
        private Label _popupMessage;
        private Button _popupButton;

        // State
        private UpgradeData.UpgradeCategory _currentUpgradeTab = UpgradeData.UpgradeCategory.Skills;
        private Dictionary<string, VisualElement> _upgradeElements = new Dictionary<string, VisualElement>();

        // Events
        public event Action OnClickZoneClicked;
        public event Action<UpgradeData> OnUpgradePurchaseRequested;
        public event Action<UpgradeData.UpgradeCategory> OnUpgradeTabChanged;

        private void Awake()
        {
            InitializeUI();
        }

        private void Start()
        {
            SubscribeToEvents();
        }

        private void InitializeUI()
        {
            if (uiDocument == null)
            {
                uiDocument = GetComponent<UIDocument>();
            }

            if (uiDocument == null)
            {
                Debug.LogError("[GameView] UIDocument not found!");
                return;
            }

            _root = uiDocument.rootVisualElement;
            CacheUIElements();
            SetupUICallbacks();
        }

        private void CacheUIElements()
        {
            // Header elements
            _stageNameLabel = _root.Q<Label>("stage-name");
            _moneyValueLabel = _root.Q<Label>("money-value");
            _expValueLabel = _root.Q<Label>("exp-value");
            _stageValueLabel = _root.Q<Label>("stage-value");
            _playerLevelLabel = _root.Q<Label>("player-level");
            _moneyIncomeLabel = _root.Q<Label>("money-income");
            _expIncomeLabel = _root.Q<Label>("exp-income");
            _moneyLockText = _root.Q<Label>("money-lock-text");
            _moneyContainer = _root.Q<VisualElement>("money-container");
            
            // Progress elements
            _progressNextStageLabel = _root.Q<Label>("progress-next-stage");
            _progressPercentLabel = _root.Q<Label>("progress-percent");
            _progressFill = _root.Q<VisualElement>("progress-fill");
            
            // Click zone
            _clickZone = _root.Q<Button>("click-zone");
            _clickZoneIcon = _root.Q<Label>("click-zone-icon");
            _clickZoneText = _root.Q<Label>("click-zone-text");
            
            // Auto income
            _autoMoneyValue = _root.Q<Label>("auto-money-value");
            _autoExpValue = _root.Q<Label>("auto-exp-value");
            
            // Project section
            _projectSection = _root.Q<VisualElement>("project-section");
            _projectName = _root.Q<Label>("project-name");
            _projectRewardValue = _root.Q<Label>("project-reward-value");
            _projectProgressFill = _root.Q<VisualElement>("project-progress-fill");
            _projectProgressText = _root.Q<Label>("project-progress-text");
            
            // Upgrade tabs
            _skillsTab = _root.Q<Button>("skills-tab");
            _equipmentTab = _root.Q<Button>("equipment-tab");
            _teamTab = _root.Q<Button>("team-tab");
            _skillsUpgrades = _root.Q<VisualElement>("skills-upgrades");
            _equipmentUpgrades = _root.Q<VisualElement>("equipment-upgrades");
            _teamUpgrades = _root.Q<VisualElement>("team-upgrades");
            
            // Popup system
            _popupOverlay = _root.Q<VisualElement>("popup-overlay");
            _unlockPopup = _root.Q<VisualElement>("unlock-popup");
            _popupTitle = _root.Q<Label>("popup-title");
            _popupMessage = _root.Q<Label>("popup-message");
            _popupButton = _root.Q<Button>("popup-button");
        }

        private void SetupUICallbacks()
        {
            // Click zone
            _clickZone?.RegisterCallback<ClickEvent>(OnClickZoneClick);
            
            // Upgrade tabs
            _skillsTab?.RegisterCallback<ClickEvent>(evt => SwitchUpgradeTab(UpgradeData.UpgradeCategory.Skills));
            _equipmentTab?.RegisterCallback<ClickEvent>(evt => SwitchUpgradeTab(UpgradeData.UpgradeCategory.Equipment));
            _teamTab?.RegisterCallback<ClickEvent>(evt => SwitchUpgradeTab(UpgradeData.UpgradeCategory.Team));
            
            // Popup button
            _popupButton?.RegisterCallback<ClickEvent>(evt => HidePopup());
        }

        private void SubscribeToEvents()
        {
            GameEvents.OnMoneyChanged += UpdateMoneyDisplay;
            GameEvents.OnExperienceChanged += UpdateExperienceDisplay;
            GameEvents.OnClickValueChanged += UpdateClickValues;
            GameEvents.OnAutoIncomeChanged += UpdateAutoIncome;
            GameEvents.OnLevelUp += UpdatePlayerLevel;
            GameEvents.OnStageUnlocked += UpdateStageDisplay;
            GameEvents.OnFeatureUnlocked += OnFeatureUnlocked;
            GameEvents.OnProjectProgressChanged += UpdateProjectProgress;
            GameEvents.OnNotificationShown += ShowNotificationPopup;
        }

        #region UI Update Methods

        public void UpdateMoneyDisplay(long money)
        {
            if (GameModel.Instance.IsMoneyUnlocked)
            {
                if (_moneyValueLabel != null) _moneyValueLabel.text = NumberFormatter.FormatCurrency(money);
                _moneyContainer?.RemoveFromClassList("locked");
                _moneyLockText?.AddToClassList("hidden");
                _moneyIncomeLabel?.RemoveFromClassList("hidden");
            }
            else
            {
                if (_moneyValueLabel != null) _moneyValueLabel.text = "---";
                _moneyContainer?.AddToClassList("locked");
                _moneyLockText?.RemoveFromClassList("hidden");
                _moneyIncomeLabel?.AddToClassList("hidden");
            }
        }

        public void UpdateExperienceDisplay(long experience)
        {
            if (_expValueLabel != null) _expValueLabel.text = NumberFormatter.Format(experience);
        }

        public void UpdateClickValues(float moneyPerClick, float expPerClick)
        {
            if (GameModel.Instance.IsMoneyUnlocked)
            {
                if (_moneyIncomeLabel != null) _moneyIncomeLabel.text = $"+{NumberFormatter.Format((long)moneyPerClick)}/click";
            }
            if (_expIncomeLabel != null) _expIncomeLabel.text = $"+{NumberFormatter.Format((long)expPerClick)}/click";
        }

        public void UpdateAutoIncome(float autoMoney, float autoExp)
        {
            if (_autoMoneyValue != null) _autoMoneyValue.text = NumberFormatter.Format((long)autoMoney);
            if (_autoExpValue != null) _autoExpValue.text = NumberFormatter.Format((long)autoExp);
        }

        public void UpdatePlayerLevel(int level)
        {
            if (_playerLevelLabel != null) _playerLevelLabel.text = $"Level {level}";
        }

        public void UpdateStageDisplay(int stage)
        {
            if (_stageValueLabel != null) _stageValueLabel.text = $"{stage}/10";
            
            // Update stage name based on stage number
            string stageName = GetStageName(stage);
            if (_stageNameLabel != null) _stageNameLabel.text = stageName;
            
            // Update next stage progress label
            string nextStageName = GetStageName(stage + 1);
            if (_progressNextStageLabel != null) _progressNextStageLabel.text = $"Next Stage: {nextStageName}";
        }

        public void UpdateStageProgress(float progress)
        {
            progress = Mathf.Clamp01(progress);
            if (_progressFill != null) _progressFill.style.width = Length.Percent(progress * 100f);
            if (_progressPercentLabel != null) _progressPercentLabel.text = $"{Mathf.RoundToInt(progress * 100)}%";
        }

        public void UpdateProjectProgress(float progress)
        {
            if (_projectSection == null) return;
            
            progress = Mathf.Clamp01(progress);
            if (_projectProgressFill != null) _projectProgressFill.style.width = Length.Percent(progress * 100f);
        }

        public void UpdateProjectInfo(string projectName, long reward, float progress, float required)
        {
            if (_projectName != null) _projectName.text = projectName;
            if (_projectRewardValue != null) _projectRewardValue.text = NumberFormatter.Format(reward);
            if (_projectProgressText != null) _projectProgressText.text = $"{NumberFormatter.Format((long)(progress))} / {NumberFormatter.Format((long)required)} EXP";
            UpdateProjectProgress(progress / required);
        }

        #endregion

        #region Upgrade System

        private void SwitchUpgradeTab(UpgradeData.UpgradeCategory category)
        {
            // Update tab visual state
            _skillsTab?.RemoveFromClassList("active");
            _equipmentTab?.RemoveFromClassList("active");
            _teamTab?.RemoveFromClassList("active");
            
            // Hide all upgrade lists
            _skillsUpgrades?.AddToClassList("hidden");
            _equipmentUpgrades?.AddToClassList("hidden");
            _teamUpgrades?.AddToClassList("hidden");
            
            // Show selected tab and list
            switch (category)
            {
                case UpgradeData.UpgradeCategory.Skills:
                    _skillsTab?.AddToClassList("active");
                    _skillsUpgrades?.RemoveFromClassList("hidden");
                    break;
                case UpgradeData.UpgradeCategory.Equipment:
                    _equipmentTab?.AddToClassList("active");
                    _equipmentUpgrades?.RemoveFromClassList("hidden");
                    break;
                case UpgradeData.UpgradeCategory.Team:
                    _teamTab?.AddToClassList("active");
                    _teamUpgrades?.RemoveFromClassList("hidden");
                    break;
            }
            
            _currentUpgradeTab = category;
            OnUpgradeTabChanged?.Invoke(category);
        }

        public void PopulateUpgradeList(UpgradeData.UpgradeCategory category, List<UpgradeData> upgrades)
        {
            VisualElement container = GetUpgradeContainer(category);
            if (container == null) return;
            
            container.Clear();
            
            foreach (var upgrade in upgrades)
            {
                CreateUpgradeElement(upgrade, container);
            }
        }

        private VisualElement GetUpgradeContainer(UpgradeData.UpgradeCategory category)
        {
            return category switch
            {
                UpgradeData.UpgradeCategory.Skills => _skillsUpgrades,
                UpgradeData.UpgradeCategory.Equipment => _equipmentUpgrades,
                UpgradeData.UpgradeCategory.Team => _teamUpgrades,
                _ => null
            };
        }

        private void CreateUpgradeElement(UpgradeData upgrade, VisualElement container)
        {
            var upgradeElement = new VisualElement();
            upgradeElement.AddToClassList("upgrade-item");
            
            // Upgrade info
            var infoElement = new VisualElement();
            infoElement.AddToClassList("upgrade-info");
            
            var nameLabel = new Label(upgrade.upgradeName);
            nameLabel.AddToClassList("upgrade-name");
            infoElement.Add(nameLabel);
            
            var descLabel = new Label(upgrade.description);
            descLabel.AddToClassList("upgrade-desc");
            infoElement.Add(descLabel);
            
            // Upgrade button
            var button = new Button();
            button.AddToClassList("upgrade-button");
            
            // Add currency-specific styling
            if (upgrade.currencyType == UpgradeData.CurrencyType.Experience)
            {
                button.AddToClassList("exp-button");
            }
            else
            {
                button.AddToClassList("money-button");
            }
            
            button.clicked += () => OnUpgradePurchaseRequested?.Invoke(upgrade);
            
            upgradeElement.Add(infoElement);
            upgradeElement.Add(button);
            container.Add(upgradeElement);
            
            _upgradeElements[upgrade.upgradeId] = upgradeElement;
            
            UpdateUpgradeElement(upgrade);
        }

        public void UpdateUpgradeElement(UpgradeData upgrade)
        {
            if (!_upgradeElements.TryGetValue(upgrade.upgradeId, out var element)) return;
            
            var button = element.Q<Button>();
            if (button == null) return;
            
            // Update button text with cost
            long cost = upgrade.CalculatePrice(0); // TODO: Get actual level from UpgradeManager
            string currencySymbol = upgrade.currencyType == UpgradeData.CurrencyType.Money ? "💰" : "⭐";
            button.text = $"{currencySymbol} {NumberFormatter.Format(cost)}";
            
            // Update button enabled state based on affordability
            bool canAfford = CanAffordUpgrade(upgrade, cost);
            button.SetEnabled(canAfford);
        }

        private bool CanAffordUpgrade(UpgradeData upgrade, long cost)
        {
            return upgrade.currencyType switch
            {
                UpgradeData.CurrencyType.Money => GameModel.Instance.Money >= cost && GameModel.Instance.IsMoneyUnlocked,
                UpgradeData.CurrencyType.Experience => GameModel.Instance.Experience >= cost,
                _ => false
            };
        }

        #endregion

        #region Click Effects

        private void OnClickZoneClick(ClickEvent evt)
        {
            OnClickZoneClicked?.Invoke();
            CreateClickEffect(evt.localPosition);
        }

        private void CreateClickEffect(Vector2 position)
        {
            var effect = new Label();
            effect.AddToClassList("click-effect");
            
            // Get current click values for display
            float expGain = GameModel.Instance.ExpPerClick;
            float moneyGain = GameModel.Instance.IsMoneyUnlocked ? GameModel.Instance.MoneyPerClick : 0f;
            
            if (moneyGain > 0)
            {
                effect.text = $"+{NumberFormatter.Format((long)expGain)} ⭐\n+{NumberFormatter.Format((long)moneyGain)} 💰";
            }
            else
            {
                effect.text = $"+{NumberFormatter.Format((long)expGain)} ⭐";
            }
            
            // Position the effect
            effect.style.position = Position.Absolute;
            effect.style.left = position.x - 30;
            effect.style.top = position.y - 20;
            
            _clickZone.Add(effect);
            
            // Remove after animation
            this.Wait(clickEffectDuration, () => effect?.RemoveFromHierarchy());
        }

        #endregion

        #region Feature Unlocks

        private void OnFeatureUnlocked(string featureName)
        {
            switch (featureName)
            {
                case "money":
                    UpdateMoneyDisplay(GameModel.Instance.Money);
                    break;
                case "project_system":
                    _projectSection?.RemoveFromClassList("hidden");
                    _projectSection?.AddToClassList("unlocked");
                    break;
            }
        }

        #endregion

        #region Popup System

        public void ShowNotificationPopup(string title, string message)
        {
            if (_popupTitle != null) _popupTitle.text = title;
            if (_popupMessage != null) _popupMessage.text = message;
            _popupOverlay?.RemoveFromClassList("hidden");
            
            // Auto-hide after duration
            this.Wait(popupDisplayDuration, () => HidePopup());
        }

        private void HidePopup()
        {
            _popupOverlay?.AddToClassList("hidden");
        }

        #endregion

        #region Utility Methods

        private string GetStageName(int stage)
        {
            return stage switch
            {
                1 => "Indie Game Developer",
                2 => "Mobile Game Development",
                3 => "PC Game Development", 
                4 => "VR Game Development",
                5 => "AI Development",
                6 => "Robot Development",
                7 => "Rocket Development",
                8 => "Spaceship Development",
                9 => "Black Hole Explorer Development",
                10 => "Time Machine & Space-Time Conquest",
                _ => "Unknown Stage"
            };
        }

        #endregion

        private void OnDestroy()
        {
            GameEvents.OnMoneyChanged -= UpdateMoneyDisplay;
            GameEvents.OnExperienceChanged -= UpdateExperienceDisplay;
            GameEvents.OnClickValueChanged -= UpdateClickValues;
            GameEvents.OnAutoIncomeChanged -= UpdateAutoIncome;
            GameEvents.OnLevelUp -= UpdatePlayerLevel;
            GameEvents.OnStageUnlocked -= UpdateStageDisplay;
            GameEvents.OnFeatureUnlocked -= OnFeatureUnlocked;
            GameEvents.OnProjectProgressChanged -= UpdateProjectProgress;
            GameEvents.OnNotificationShown -= ShowNotificationPopup;
        }
    }

    // Extension method for delayed actions
    public static class MonoBehaviourExtensions
    {
        public static void Wait(this MonoBehaviour mono, float delay, Action callback)
        {
            mono.StartCoroutine(WaitCoroutine(delay, callback));
        }

        private static System.Collections.IEnumerator WaitCoroutine(float delay, Action callback)
        {
            yield return new WaitForSeconds(delay);
            callback?.Invoke();
        }
    }
}