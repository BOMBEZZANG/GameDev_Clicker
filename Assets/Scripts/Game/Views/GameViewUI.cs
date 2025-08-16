using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GameDevClicker.Core.Patterns;
using GameDevClicker.Core.Utilities;
using GameDevClicker.Data.ScriptableObjects;
using GameDevClicker.Game.Models;

namespace GameDevClicker.Game.Views
{
    public class GameViewUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Canvas mainCanvas;

        [Header("Header UI")]
        [SerializeField] private TextMeshProUGUI stageNameText;
        [SerializeField] private TextMeshProUGUI stageTitleText;

        [Header("Currency Display")]
        [SerializeField] private GameObject moneyContainer;
        [SerializeField] private TextMeshProUGUI moneyValueText;
        [SerializeField] private TextMeshProUGUI moneyIncomeText;
        [SerializeField] private TextMeshProUGUI moneyLockText;
        [SerializeField] private TextMeshProUGUI expValueText;
        [SerializeField] private TextMeshProUGUI expIncomeText;
        [SerializeField] private TextMeshProUGUI stageValueText;
        [SerializeField] private TextMeshProUGUI playerLevelText;

        [Header("Progress Bar")]
        [SerializeField] private TextMeshProUGUI progressNextStageText;
        [SerializeField] private TextMeshProUGUI progressPercentText;
        [SerializeField] private Image progressFillImage;

        [Header("Click Zone")]
        [SerializeField] private Button clickZoneButton;
        [SerializeField] private TextMeshProUGUI clickZoneIcon;
        [SerializeField] private TextMeshProUGUI clickZoneText;

        [Header("Auto Income")]
        [SerializeField] private TextMeshProUGUI autoIncomeText;

        [Header("Project System")]
        [SerializeField] private GameObject projectPanel;
        [SerializeField] private TextMeshProUGUI projectTitleText;
        [SerializeField] private TextMeshProUGUI projectRewardText;
        [SerializeField] private Image projectProgressFill;
        [SerializeField] private TextMeshProUGUI projectProgressText;

        [Header("Upgrade System")]
        [SerializeField] private Button skillsTabButton;
        [SerializeField] private Button equipmentTabButton;
        [SerializeField] private Button teamTabButton;
        [SerializeField] private ScrollRect upgradeScrollRect;
        [SerializeField] private Transform upgradeListParent;
        [SerializeField] private GameObject upgradeItemPrefab;

        [Header("Popup System")]
        [SerializeField] private GameObject popupOverlay;
        [SerializeField] private GameObject unlockPopup;
        [SerializeField] private TextMeshProUGUI popupTitleText;
        [SerializeField] private TextMeshProUGUI popupMessageText;
        [SerializeField] private Button popupOkButton;

        [Header("Click Effects")]
        [SerializeField] private Transform clickEffectParent;
        public Transform ClickEffectParent => clickEffectParent;
        [SerializeField] private GameObject clickEffectPrefab;
        [SerializeField] private float clickEffectDuration = 1f;
        [SerializeField] private float clickEffectDistance = 70f;

        [Header("Configuration")]
        [SerializeField] private float popupDisplayDuration = 3f;

        // State
        private UpgradeData.UpgradeCategory _currentUpgradeTab = UpgradeData.UpgradeCategory.Skills;
        private Dictionary<string, GameObject> _upgradeElements = new Dictionary<string, GameObject>();
        private Dictionary<UpgradeData.UpgradeCategory, List<GameObject>> _cachedUpgradeElements = new Dictionary<UpgradeData.UpgradeCategory, List<GameObject>>();
        private List<Coroutine> _activeClickEffects = new List<Coroutine>();

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
            SetupInitialState();
        }

        private void InitializeUI()
        {
            // Auto-find components if not assigned
            if (mainCanvas == null)
                mainCanvas = GetComponentInParent<Canvas>();

            SetupTabButtonTexts();
            SetupUICallbacks();
        }

        private void SetupTabButtonTexts()
        {
            // Set tab button texts
            if (skillsTabButton != null)
            {
                var skillsText = skillsTabButton.GetComponentInChildren<TextMeshProUGUI>();
                if (skillsText != null) skillsText.text = "Skills";
            }
            
            if (equipmentTabButton != null)
            {
                var equipmentText = equipmentTabButton.GetComponentInChildren<TextMeshProUGUI>();
                if (equipmentText != null) equipmentText.text = "Equipment";
            }
            
            if (teamTabButton != null)
            {
                var teamText = teamTabButton.GetComponentInChildren<TextMeshProUGUI>();
                if (teamText != null) teamText.text = "Team";
            }
        }

        private void SetupUICallbacks()
        {
            // Click zone
            if (clickZoneButton != null)
                clickZoneButton.onClick.AddListener(OnClickZoneClick);

            // Upgrade tabs
            if (skillsTabButton != null)
                skillsTabButton.onClick.AddListener(() => SwitchUpgradeTab(UpgradeData.UpgradeCategory.Skills));
            if (equipmentTabButton != null)
                equipmentTabButton.onClick.AddListener(() => SwitchUpgradeTab(UpgradeData.UpgradeCategory.Equipment));
            if (teamTabButton != null)
                teamTabButton.onClick.AddListener(() => SwitchUpgradeTab(UpgradeData.UpgradeCategory.Team));

            // Popup button
            if (popupOkButton != null)
                popupOkButton.onClick.AddListener(HidePopup);
        }

        private void SetupInitialState()
        {
            // Hide popup overlay
            if (popupOverlay != null)
                popupOverlay.SetActive(false);

            // Hide project panel initially
            if (projectPanel != null)
                projectPanel.SetActive(false);

            // Set initial tab
            SwitchUpgradeTab(UpgradeData.UpgradeCategory.Skills);

            // Setup click effect parent
            if (clickEffectParent == null && clickZoneButton != null)
                clickEffectParent = clickZoneButton.transform;
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
            GameEvents.OnClickPerformed += OnClickPerformed;
        }

        #region UI Update Methods

        public void UpdateMoneyDisplay(long money)
        {
            if (GameModel.Instance.IsMoneyUnlocked)
            {
                if (moneyValueText != null) moneyValueText.text = NumberFormatter.FormatCurrency(money);
                if (moneyContainer != null)
                {
                    var containerImage = moneyContainer.GetComponent<Image>();
                    if (containerImage != null) containerImage.color = new Color(1, 1, 1, 0.1f);
                }
                if (moneyLockText != null) moneyLockText.gameObject.SetActive(false);
                if (moneyIncomeText != null) moneyIncomeText.gameObject.SetActive(true);
            }
            else
            {
                if (moneyValueText != null) moneyValueText.text = "---";
                if (moneyContainer != null)
                {
                    var containerImage = moneyContainer.GetComponent<Image>();
                    if (containerImage != null) containerImage.color = new Color(1, 1, 1, 0.05f);
                }
                if (moneyLockText != null) moneyLockText.gameObject.SetActive(true);
                if (moneyIncomeText != null) moneyIncomeText.gameObject.SetActive(false);
            }
        }

        public void UpdateExperienceDisplay(long experience)
        {
            if (expValueText != null) expValueText.text = NumberFormatter.Format(experience);
        }

        public void UpdateClickValues(float moneyPerClick, float expPerClick)
        {
            if (GameModel.Instance.IsMoneyUnlocked && moneyIncomeText != null)
            {
                moneyIncomeText.text = $"+{NumberFormatter.Format((long)moneyPerClick)}/click";
            }
            if (expIncomeText != null) expIncomeText.text = $"+{NumberFormatter.Format((long)expPerClick)}/click";
        }

        public void UpdateAutoIncome(float autoMoney, float autoExp)
        {
            if (autoIncomeText != null)
            {
                autoIncomeText.text = $"Auto Income: 💰 {NumberFormatter.Format((long)autoMoney)}/s | ⭐ {NumberFormatter.Format((long)autoExp)}/s";
            }
        }

        public void UpdatePlayerLevel(int level)
        {
            if (playerLevelText != null) playerLevelText.text = $"Level {level}";
        }

        public void UpdateStageDisplay(int stage)
        {
            if (stageValueText != null) stageValueText.text = $"{stage}/10";
            
            // Update stage name
            string stageName = GetStageName(stage);
            if (stageNameText != null) stageNameText.text = stageName;
            
            // Update next stage progress label
            string nextStageName = GetStageName(stage + 1);
            if (progressNextStageText != null) progressNextStageText.text = $"Next Stage: {nextStageName}";
        }

        public void UpdateStageProgress(float progress)
        {
            progress = Mathf.Clamp01(progress);
            if (progressFillImage != null) progressFillImage.fillAmount = progress;
            if (progressPercentText != null) progressPercentText.text = $"{Mathf.RoundToInt(progress * 100)}%";
        }

        public void UpdateProjectProgress(float progress)
        {
            if (projectProgressFill != null) projectProgressFill.fillAmount = Mathf.Clamp01(progress);
        }

        public void UpdateProjectInfo(string projectName, long reward, float progress, float required)
        {
            if (projectTitleText != null) projectTitleText.text = $"🎮 Project: {projectName}";
            if (projectRewardText != null) projectRewardText.text = $"Complete Reward: 💰 {NumberFormatter.Format(reward)}";
            if (projectProgressText != null) projectProgressText.text = $"{NumberFormatter.Format((long)progress)} / {NumberFormatter.Format((long)required)} EXP";
            UpdateProjectProgress(progress / required);
        }

        #endregion

        #region Upgrade System

        private void SwitchUpgradeTab(UpgradeData.UpgradeCategory category)
        {
            // Hide current tab elements
            HideCurrentTabElements();
            
            // Update tab visual state
            UpdateTabVisuals(category);
            
            _currentUpgradeTab = category;
            
            // Show new tab elements
            ShowTabElements(category);
            
            OnUpgradeTabChanged?.Invoke(category);
        }
        
        private void HideCurrentTabElements()
        {
            if (_cachedUpgradeElements.TryGetValue(_currentUpgradeTab, out var elements))
            {
                foreach (var element in elements)
                {
                    if (element != null) element.SetActive(false);
                }
            }
        }
        
        private void ShowTabElements(UpgradeData.UpgradeCategory category)
        {
            if (_cachedUpgradeElements.TryGetValue(category, out var elements))
            {
                foreach (var element in elements)
                {
                    if (element != null) element.SetActive(true);
                }
            }
        }

        private void UpdateTabVisuals(UpgradeData.UpgradeCategory activeCategory)
        {
            // Reset all tab colors
            if (skillsTabButton != null)
                SetTabActive(skillsTabButton, activeCategory == UpgradeData.UpgradeCategory.Skills);
            if (equipmentTabButton != null)
                SetTabActive(equipmentTabButton, activeCategory == UpgradeData.UpgradeCategory.Equipment);
            if (teamTabButton != null)
                SetTabActive(teamTabButton, activeCategory == UpgradeData.UpgradeCategory.Team);
        }

        private void SetTabActive(Button tab, bool isActive)
        {
            var colors = tab.colors;
            if (isActive)
            {
                colors.normalColor = new Color(1f, 0.84f, 0f, 0.3f); // Gold with alpha
                colors.highlightedColor = new Color(1f, 0.84f, 0f, 0.4f);
            }
            else
            {
                colors.normalColor = new Color(1f, 1f, 1f, 0.1f);
                colors.highlightedColor = new Color(1f, 1f, 1f, 0.2f);
            }
            tab.colors = colors;
        }

        public void PopulateUpgradeList(UpgradeData.UpgradeCategory category, List<UpgradeData> upgrades)
        {
            // Check if we already have cached elements for this category
            if (_cachedUpgradeElements.ContainsKey(category))
            {
                // Update existing elements instead of recreating
                UpdateCachedElements(category, upgrades);
                
                // If this is the current tab, show the elements
                if (category == _currentUpgradeTab)
                {
                    ShowTabElements(category);
                }
                return;
            }
            
            // First time creating elements for this category
            if (!_cachedUpgradeElements.ContainsKey(category))
            {
                _cachedUpgradeElements[category] = new List<GameObject>();
            }
            
            foreach (var upgrade in upgrades)
            {
                CreateUpgradeElement(upgrade, category);
            }
            
            // Only show if this is the current tab
            if (category == _currentUpgradeTab)
            {
                ShowTabElements(category);
            }
            else
            {
                HideTabElementsForCategory(category);
            }
        }
        
        private void UpdateCachedElements(UpgradeData.UpgradeCategory category, List<UpgradeData> upgrades)
        {
            var cachedElements = _cachedUpgradeElements[category];
            
            // Update existing elements
            for (int i = 0; i < upgrades.Count && i < cachedElements.Count; i++)
            {
                var upgradeItem = cachedElements[i].GetComponent<UpgradeItemUI>();
                if (upgradeItem != null)
                {
                    upgradeItem.Setup(upgrades[i], () => OnUpgradePurchaseRequested?.Invoke(upgrades[i]));
                }
            }
            
            // Create any additional elements if needed
            for (int i = cachedElements.Count; i < upgrades.Count; i++)
            {
                CreateUpgradeElement(upgrades[i], category);
            }
        }
        
        private void HideTabElementsForCategory(UpgradeData.UpgradeCategory category)
        {
            if (_cachedUpgradeElements.TryGetValue(category, out var elements))
            {
                foreach (var element in elements)
                {
                    if (element != null) element.SetActive(false);
                }
            }
        }

        private void ClearUpgradeList()
        {
            // Clear all cached elements for all categories
            foreach (var kvp in _cachedUpgradeElements)
            {
                foreach (var element in kvp.Value)
                {
                    if (element != null) Destroy(element);
                }
            }
            _cachedUpgradeElements.Clear();
            _upgradeElements.Clear();
        }

        private void CreateUpgradeElement(UpgradeData upgrade, UpgradeData.UpgradeCategory category)
        {
            if (upgradeItemPrefab == null || upgradeListParent == null) 
            {
                Debug.LogError($"[GameViewUI] Cannot create upgrade element: prefab={upgradeItemPrefab?.name ?? "null"}, parent={upgradeListParent?.name ?? "null"}");
                return;
            }

            Debug.Log($"[GameViewUI] Creating upgrade element for {upgrade.upgradeId} in parent {upgradeListParent.name}");
            
            var upgradeObject = Instantiate(upgradeItemPrefab, upgradeListParent);
            upgradeObject.name = $"UpgradeItem_{upgrade.upgradeId}"; // Better naming
            
            var upgradeItem = upgradeObject.GetComponent<UpgradeItemUI>();
            
            if (upgradeItem != null)
            {
                upgradeItem.Setup(upgrade, () => OnUpgradePurchaseRequested?.Invoke(upgrade));
                _upgradeElements[upgrade.upgradeId] = upgradeObject;
                _cachedUpgradeElements[category].Add(upgradeObject);
                Debug.Log($"[GameViewUI] Successfully created upgrade UI for {upgrade.upgradeName}");
            }
            else
            {
                Debug.LogError($"[GameViewUI] UpgradeItemUI component not found on prefab {upgradeItemPrefab.name}");
                Destroy(upgradeObject);
            }
        }
        
        // Overload for backward compatibility
        private void CreateUpgradeElement(UpgradeData upgrade)
        {
            CreateUpgradeElement(upgrade, _currentUpgradeTab);
        }

        public void UpdateUpgradeElement(UpgradeData upgrade)
        {
            if (_upgradeElements.TryGetValue(upgrade.upgradeId, out var element))
            {
                var upgradeItem = element.GetComponent<UpgradeItemUI>();
                upgradeItem?.UpdateDisplay(upgrade);
            }
        }

        #endregion

        #region Click Effects

        private void OnClickZoneClick()
        {
            OnClickZoneClicked?.Invoke();
            Vector2 clickPosition = GetRandomClickPosition();
            CreateClickEffect(clickPosition);
        }

        private void OnClickPerformed(float moneyGained, float expGained)
        {
            // Visual feedback for the click
            AnimateClickZone();
        }

        private void AnimateClickZone()
        {
            if (clickZoneButton != null)
            {
                StartCoroutine(ClickZoneAnimation());
            }
        }

        private IEnumerator ClickZoneAnimation()
        {
            var originalScale = clickZoneButton.transform.localScale;
            var pressedScale = originalScale * 0.95f;
            
            // Scale down
            float time = 0;
            while (time < 0.1f)
            {
                time += Time.deltaTime;
                float t = time / 0.1f;
                clickZoneButton.transform.localScale = Vector3.Lerp(originalScale, pressedScale, t);
                yield return null;
            }
            
            // Scale back up
            time = 0;
            while (time < 0.1f)
            {
                time += Time.deltaTime;
                float t = time / 0.1f;
                clickZoneButton.transform.localScale = Vector3.Lerp(pressedScale, originalScale, t);
                yield return null;
            }
            
            clickZoneButton.transform.localScale = originalScale;
        }

        private void CreateClickEffect(Vector2 position)
        {
            if (clickEffectPrefab == null || clickEffectParent == null) return;

            var effectObject = Instantiate(clickEffectPrefab, clickEffectParent);
            var effectText = effectObject.GetComponent<TextMeshProUGUI>();
            
            if (effectText != null)
            {
                // Set effect text
                float expGain = GameModel.Instance.ExpPerClick;
                float moneyGain = GameModel.Instance.IsMoneyUnlocked ? GameModel.Instance.MoneyPerClick : 0f;
                
                if (moneyGain > 0)
                {
                    effectText.text = $"+{NumberFormatter.Format((long)expGain)} ⭐\n+{NumberFormatter.Format((long)moneyGain)} 💰";
                }
                else
                {
                    effectText.text = $"+{NumberFormatter.Format((long)expGain)} ⭐";
                }
                
                // Position the effect
                var rectTransform = effectObject.GetComponent<RectTransform>();
                rectTransform.anchoredPosition = position;
                
                // Start animation
                var coroutine = StartCoroutine(AnimateClickEffect(effectObject, rectTransform));
                _activeClickEffects.Add(coroutine);
            }
        }

        private IEnumerator AnimateClickEffect(GameObject effectObject, RectTransform rectTransform)
        {
            var canvasGroup = effectObject.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = effectObject.AddComponent<CanvasGroup>();
            
            var startPos = rectTransform.anchoredPosition;
            var endPos = startPos + Vector2.up * clickEffectDistance;
            
            float time = 0;
            while (time < clickEffectDuration)
            {
                time += Time.deltaTime;
                float t = time / clickEffectDuration;
                
                // Move up and fade out
                rectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
                canvasGroup.alpha = 1f - t;
                
                yield return null;
            }
            
            Destroy(effectObject);
        }

        private Vector2 GetRandomClickPosition()
        {
            return new Vector2(
                UnityEngine.Random.Range(-50f, 50f),
                UnityEngine.Random.Range(-50f, 50f)
            );
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
                    if (projectPanel != null) projectPanel.SetActive(true);
                    break;
            }
        }

        #endregion

        #region Popup System

        public void ShowNotificationPopup(string title, string message)
        {
            if (popupTitleText != null) popupTitleText.text = title;
            if (popupMessageText != null) popupMessageText.text = message;
            if (popupOverlay != null) popupOverlay.SetActive(true);
            
            // Auto-hide after duration
            StartCoroutine(AutoHidePopup());
        }

        private IEnumerator AutoHidePopup()
        {
            yield return new WaitForSeconds(popupDisplayDuration);
            HidePopup();
        }

        private void HidePopup()
        {
            if (popupOverlay != null) popupOverlay.SetActive(false);
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
            // Stop all active click effects
            foreach (var coroutine in _activeClickEffects)
            {
                if (coroutine != null) StopCoroutine(coroutine);
            }
            _activeClickEffects.Clear();

            // Unsubscribe from events
            GameEvents.OnMoneyChanged -= UpdateMoneyDisplay;
            GameEvents.OnExperienceChanged -= UpdateExperienceDisplay;
            GameEvents.OnClickValueChanged -= UpdateClickValues;
            GameEvents.OnAutoIncomeChanged -= UpdateAutoIncome;
            GameEvents.OnLevelUp -= UpdatePlayerLevel;
            GameEvents.OnStageUnlocked -= UpdateStageDisplay;
            GameEvents.OnFeatureUnlocked -= OnFeatureUnlocked;
            GameEvents.OnProjectProgressChanged -= UpdateProjectProgress;
            GameEvents.OnNotificationShown -= ShowNotificationPopup;
            GameEvents.OnClickPerformed -= OnClickPerformed;
        }
    }
}