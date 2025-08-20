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
        
        // Click zone protection variables
        private Vector2 _originalClickZoneSize;
        private Vector3 _originalClickZoneScale;

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
            StartCoroutine(FixScrollViewDelayed());
        }

        private void InitializeUI()
        {
            // Auto-find components if not assigned
            if (mainCanvas == null)
                mainCanvas = GetComponentInParent<Canvas>();
            
            // Ensure correct parent for upgrade items early
            EnsureCorrectUpgradeParent();

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
            {
                clickZoneButton.onClick.AddListener(OnClickZoneClick);
                
                // Store initial size to protect against external modifications
                var rectTransform = clickZoneButton.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    _originalClickZoneSize = rectTransform.sizeDelta;
                    _originalClickZoneScale = clickZoneButton.transform.localScale;
                }
            }

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
            // Validate inputs
            if (upgrades == null)
            {
                Debug.LogError($"[GameViewUI] PopulateUpgradeList called with null upgrades list for category {category}");
                return;
            }
            
            // Remove any null entries from the upgrades list
            var validUpgrades = new List<UpgradeData>();
            for (int i = 0; i < upgrades.Count; i++)
            {
                if (upgrades[i] != null)
                {
                    validUpgrades.Add(upgrades[i]);
                }
                else
                {
                    Debug.LogWarning($"[GameViewUI] Null upgrade found at index {i} in category {category}");
                }
            }
            
            // Check if we already have cached elements for this category
            if (_cachedUpgradeElements.ContainsKey(category))
            {
                // Update existing elements instead of recreating
                UpdateCachedElements(category, validUpgrades);
                
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
            
            foreach (var upgrade in validUpgrades)
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
                    // Capture the upgrade in a local variable to avoid closure issues
                    var currentUpgrade = upgrades[i];
                    upgradeItem.Setup(currentUpgrade, () => 
                    {
                        if (currentUpgrade != null)
                        {
                            OnUpgradePurchaseRequested?.Invoke(currentUpgrade);
                        }
                        else
                        {
                            Debug.LogError("[GameViewUI] Upgrade is null in button click handler");
                        }
                    });
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
            // Ensure we have the correct parent (Content, not Viewport)
            Transform correctParent = EnsureCorrectUpgradeParent();
            
            if (upgradeItemPrefab == null || correctParent == null) 
            {
                Debug.LogError($"[GameViewUI] Cannot create upgrade element: prefab={upgradeItemPrefab?.name ?? "null"}, parent={correctParent?.name ?? "null"}");
                return;
            }

            if (upgrade == null)
            {
                Debug.LogError("[GameViewUI] Cannot create upgrade element for null upgrade");
                return;
            }

            Debug.Log($"[GameViewUI] Creating upgrade element for {upgrade.upgradeId} in parent {correctParent.name}");
            
            var upgradeObject = Instantiate(upgradeItemPrefab, correctParent);
            upgradeObject.name = $"UpgradeItem_{upgrade.upgradeId}"; // Better naming
            
            // Ensure upgrade item has proper RectTransform settings
            var itemRect = upgradeObject.GetComponent<RectTransform>();
            if (itemRect != null)
            {
                // Make item stretch to full width of content
                itemRect.anchorMin = new Vector2(0, 1);  // Left-Top
                itemRect.anchorMax = new Vector2(1, 1);  // Right-Top
                itemRect.pivot = new Vector2(0.5f, 0.5f);
                itemRect.sizeDelta = new Vector2(0, 100); // 0 width = stretch, 100 height (or your preferred height)
                itemRect.anchoredPosition = Vector2.zero;
            }
            
            var upgradeItem = upgradeObject.GetComponent<UpgradeItemUI>();
            
            if (upgradeItem != null)
            {
                // Capture the upgrade in a local variable to avoid closure issues
                var capturedUpgrade = upgrade;
                upgradeItem.Setup(capturedUpgrade, () => 
                {
                    if (capturedUpgrade != null)
                    {
                        OnUpgradePurchaseRequested?.Invoke(capturedUpgrade);
                    }
                    else
                    {
                        Debug.LogError("[GameViewUI] Captured upgrade is null in button click handler");
                    }
                });
                _upgradeElements[upgrade.upgradeId] = upgradeObject;
                
                // Ensure the category list exists
                if (!_cachedUpgradeElements.ContainsKey(category))
                {
                    _cachedUpgradeElements[category] = new List<GameObject>();
                }
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
            
            // Trigger character animations through CharacterManager
            var characterManager = Character.CharacterManager.Instance;
            if (characterManager != null)
            {
                characterManager.OnClickZoneClicked();
            }
            
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
            // Temporarily disable animation to test if it's causing the shrinking
            // if (clickZoneButton != null)
            // {
            //     StartCoroutine(ClickZoneAnimation());
            // }
            
            // Just log the click for now
            Debug.Log("[ClickZone] Click animation disabled to prevent shrinking");
        }

        private IEnumerator ClickZoneAnimation()
        {
            // Store the original scale and size to restore later
            var originalScale = clickZoneButton.transform.localScale;
            var originalSize = clickZoneButton.GetComponent<RectTransform>().sizeDelta;
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
            
            // Ensure we restore both scale and size to prevent any shrinking
            clickZoneButton.transform.localScale = originalScale;
            clickZoneButton.GetComponent<RectTransform>().sizeDelta = originalSize;
            
            // Also restore to our stored original values as a safety measure
            RestoreClickZoneSize();
        }
        
        /// <summary>
        /// Restores the click zone to its original size and scale
        /// Call this if external systems are modifying the click zone
        /// </summary>
        [ContextMenu("Restore Click Zone Size")]
        public void RestoreClickZoneSize()
        {
            if (clickZoneButton != null)
            {
                var rectTransform = clickZoneButton.GetComponent<RectTransform>();
                if (rectTransform != null && _originalClickZoneSize != Vector2.zero)
                {
                    Debug.Log($"[ClickZone] Restoring size from {rectTransform.sizeDelta} to {_originalClickZoneSize}");
                    Debug.Log($"[ClickZone] Restoring scale from {clickZoneButton.transform.localScale} to {_originalClickZoneScale}");
                    clickZoneButton.transform.localScale = _originalClickZoneScale;
                    rectTransform.sizeDelta = _originalClickZoneSize;
                }
            }
        }
        
        private void Update()
        {
            // Monitor click zone size changes to catch what's modifying it
            if (clickZoneButton != null && _originalClickZoneSize != Vector2.zero)
            {
                var rectTransform = clickZoneButton.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    var currentSize = rectTransform.sizeDelta;
                    var currentScale = clickZoneButton.transform.localScale;
                    
                    // Check if size or scale has changed unexpectedly
                    if (Vector2.Distance(currentSize, _originalClickZoneSize) > 1f || 
                        Vector3.Distance(currentScale, _originalClickZoneScale) > 0.01f)
                    {
                        Debug.LogWarning($"[ClickZone] Size changed! Current: {currentSize}, Original: {_originalClickZoneSize}");
                        Debug.LogWarning($"[ClickZone] Scale changed! Current: {currentScale}, Original: {_originalClickZoneScale}");
                        Debug.LogWarning($"[ClickZone] Stack trace: {System.Environment.StackTrace}");
                        
                        // Auto-restore to prevent shrinking
                        clickZoneButton.transform.localScale = _originalClickZoneScale;
                        rectTransform.sizeDelta = _originalClickZoneSize;
                    }
                }
            }
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

        #region ScrollView Fix Methods
        
        /// <summary>
        /// Ensures upgrade items are placed in the correct Content transform, not Viewport
        /// </summary>
        private Transform EnsureCorrectUpgradeParent()
        {
            // First try to use the assigned upgradeListParent if it's the Content
            if (upgradeListParent != null && upgradeListParent.name == "Content")
            {
                return upgradeListParent;
            }
            
            // Find the ScrollRect
            if (upgradeScrollRect == null)
            {
                upgradeScrollRect = GetComponentInChildren<ScrollRect>(true);
            }
            
            if (upgradeScrollRect != null && upgradeScrollRect.content != null)
            {
                // Update the reference for future use
                upgradeListParent = upgradeScrollRect.content;
                Debug.Log($"[GameViewUI] Set upgrade parent to ScrollRect Content: {upgradeListParent.name}");
                return upgradeListParent;
            }
            
            // Fallback: Try to find Content by hierarchy
            Transform scrollView = transform.Find("Upgrades Panel/Upgrade Content/Upgrade List/Scroll View");
            if (scrollView != null)
            {
                Transform viewport = scrollView.Find("Viewport");
                if (viewport != null)
                {
                    Transform content = viewport.Find("Content");
                    if (content != null)
                    {
                        upgradeListParent = content;
                        Debug.Log($"[GameViewUI] Found Content by hierarchy search: {upgradeListParent.name}");
                        return content;
                    }
                }
            }
            
            // Last resort: if we have a parent named "Viewport", get its child "Content"
            if (upgradeListParent != null && upgradeListParent.name == "Viewport")
            {
                Transform content = upgradeListParent.Find("Content");
                if (content != null)
                {
                    Debug.LogWarning($"[GameViewUI] Parent was set to Viewport, switching to Content");
                    upgradeListParent = content;
                    return content;
                }
            }
            
            Debug.LogError("[GameViewUI] Could not find proper Content transform for upgrade items!");
            return upgradeListParent; // Return whatever we have
        }
        
        private IEnumerator FixScrollViewDelayed()
        {
            // Wait for UI to be fully initialized
            yield return new WaitForSeconds(0.1f);
            
            FixScrollView();
            
            // Fix again after a short delay to ensure everything is loaded
            yield return new WaitForSeconds(0.5f);
            FixScrollView();
            
            // Start continuous monitoring
            StartCoroutine(MonitorScrollView());
        }
        
        private void FixScrollView()
        {
            // Ensure we have the correct parent first
            Transform correctParent = EnsureCorrectUpgradeParent();
            
            var scrollRect = GetComponentInChildren<ScrollRect>(true);
            if (scrollRect == null)
            {
                Debug.LogWarning("[GameViewUI] No ScrollRect found in children");
                return;
            }
            
            // Move any misplaced upgrade items from Viewport to Content
            Transform viewport = scrollRect.viewport;
            if (viewport != null && correctParent != null && viewport != correctParent)
            {
                // Get all upgrade items that are wrongly placed in Viewport
                List<Transform> misplacedItems = new List<Transform>();
                foreach (Transform child in viewport)
                {
                    if (child.name != "Content" && child.name.Contains("Upgrade"))
                    {
                        misplacedItems.Add(child);
                    }
                }
                
                // Move them to Content
                foreach (Transform item in misplacedItems)
                {
                    Debug.Log($"[GameViewUI] Moving {item.name} from Viewport to Content");
                    item.SetParent(correctParent, false);
                }
            }
            
            // Fix content
            if (scrollRect.content != null)
            {
                var content = scrollRect.content;
                
                // Fix anchoring - Content should stretch horizontally and anchor to top
                content.anchorMin = new Vector2(0, 1);  // Left-Top
                content.anchorMax = new Vector2(1, 1);  // Right-Top  
                content.pivot = new Vector2(0.5f, 1);   // Center-Top pivot
                
                // Reset position to top-left corner
                content.anchoredPosition = new Vector2(0, 0);
                
                // Make content full width of viewport
                content.sizeDelta = new Vector2(0, content.sizeDelta.y); // 0 width means stretch to anchors
                
                // Ensure proper offset to prevent cutting off
                content.offsetMin = new Vector2(0, content.offsetMin.y);  // Left offset = 0
                content.offsetMax = new Vector2(0, content.offsetMax.y);  // Right offset = 0
                
                // Ensure VerticalLayoutGroup
                var layoutGroup = content.GetComponent<VerticalLayoutGroup>();
                if (layoutGroup == null)
                {
                    layoutGroup = content.gameObject.AddComponent<VerticalLayoutGroup>();
                }
                layoutGroup.childForceExpandWidth = true;
                layoutGroup.childForceExpandHeight = false;
                layoutGroup.childControlHeight = false;
                layoutGroup.childControlWidth = true;
                layoutGroup.spacing = 5f;
                layoutGroup.padding = new RectOffset(10, 10, 10, 10);
                layoutGroup.childAlignment = TextAnchor.UpperCenter; // Align children to top-center
                
                // Ensure ContentSizeFitter
                var sizeFitter = content.GetComponent<ContentSizeFitter>();
                if (sizeFitter == null)
                {
                    sizeFitter = content.gameObject.AddComponent<ContentSizeFitter>();
                }
                sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            }
            
            // Configure ScrollRect
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            scrollRect.elasticity = 0.1f;
            scrollRect.inertia = true;
            scrollRect.decelerationRate = 0.135f;
            scrollRect.scrollSensitivity = 15f;
            
            // Fix scrollbar
            if (scrollRect.verticalScrollbar != null)
            {
                scrollRect.verticalScrollbar.gameObject.SetActive(true);
                scrollRect.verticalScrollbar.direction = Scrollbar.Direction.TopToBottom;
                scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
                scrollRect.verticalScrollbarSpacing = -3f;
            }
            
            // Force layout rebuild
            if (scrollRect.content != null)
            {
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
            }
            
            Debug.Log("[GameViewUI] ScrollView fixed");
        }
        
        private IEnumerator MonitorScrollView()
        {
            while (true)
            {
                yield return new WaitForSeconds(1f);
                
                // Check if scrollbar became inactive
                var scrollRect = GetComponentInChildren<ScrollRect>(true);
                if (scrollRect != null && scrollRect.verticalScrollbar != null)
                {
                    if (!scrollRect.verticalScrollbar.gameObject.activeSelf && scrollRect.content != null && scrollRect.content.childCount > 0)
                    {
                        // Reactivate scrollbar if it has content
                        scrollRect.verticalScrollbar.gameObject.SetActive(true);
                        Debug.Log("[GameViewUI] Reactivated inactive scrollbar");
                    }
                }
            }
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