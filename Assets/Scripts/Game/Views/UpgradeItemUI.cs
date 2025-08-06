using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GameDevClicker.Core.Utilities;
using GameDevClicker.Data.ScriptableObjects;
using GameDevClicker.Game.Models;
using GameDevClicker.Core.Managers;

namespace GameDevClicker.Game.Views
{
    public class UpgradeItemUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI upgradeNameText;
        [SerializeField] private TextMeshProUGUI upgradeDescText;
        [SerializeField] private TextMeshProUGUI upgradeLevelText;
        [SerializeField] private Button upgradeButton;
        [SerializeField] private TextMeshProUGUI buttonText;
        [SerializeField] private Image backgroundImage;

        [Header("Currency Colors")]
        [SerializeField] private Color expButtonColor = new Color(0.61f, 0.15f, 0.69f); // Purple
        [SerializeField] private Color moneyButtonColor = new Color(1f, 0.6f, 0f); // Orange
        [SerializeField] private Color disabledColor = new Color(0.4f, 0.4f, 0.4f); // Gray

        private UpgradeData _upgradeData;
        private Action _onPurchaseRequested;

        private void Awake()
        {
            if (upgradeButton != null)
            {
                upgradeButton.onClick.AddListener(OnUpgradeButtonClick);
            }
        }

        public void Setup(UpgradeData upgradeData, Action onPurchaseRequested)
        {
            _upgradeData = upgradeData;
            _onPurchaseRequested = onPurchaseRequested;
            
            SetupInitialDisplay();
            UpdateDisplay(upgradeData);
        }

        private void SetupInitialDisplay()
        {
            if (_upgradeData == null) return;

            // Set name and description
            if (upgradeNameText != null)
                upgradeNameText.text = _upgradeData.upgradeName;

            if (upgradeDescText != null)
                upgradeDescText.text = _upgradeData.description;

            // Set currency-specific button color
            SetButtonCurrencyStyle();
        }

        public void UpdateDisplay(UpgradeData upgradeData)
        {
            if (upgradeData != _upgradeData) return;

            UpdateButtonText();
            UpdateButtonState();
            UpdateLevelDisplay();
        }

        private void SetButtonCurrencyStyle()
        {
            if (upgradeButton == null || _upgradeData == null) return;

            var colors = upgradeButton.colors;
            
            switch (_upgradeData.currencyType)
            {
                case UpgradeData.CurrencyType.Experience:
                    colors.normalColor = expButtonColor;
                    colors.highlightedColor = expButtonColor * 0.8f;
                    break;
                    
                case UpgradeData.CurrencyType.Money:
                    colors.normalColor = moneyButtonColor;
                    colors.highlightedColor = moneyButtonColor * 0.8f;
                    break;
            }
            
            colors.disabledColor = disabledColor;
            upgradeButton.colors = colors;
        }

        private void UpdateButtonText()
        {
            if (buttonText == null || _upgradeData == null) return;

            int currentLevel = GetCurrentUpgradeLevel();
            long cost = _upgradeData.CalculatePrice(currentLevel);
            
            string currencySymbol = _upgradeData.currencyType == UpgradeData.CurrencyType.Money ? "💰" : "⭐";
            buttonText.text = $"{currencySymbol} {NumberFormatter.Format(cost)}";
        }

        private void UpdateButtonState()
        {
            if (upgradeButton == null || _upgradeData == null) return;

            bool canAfford = CanAffordUpgrade();
            bool isMaxLevel = IsMaxLevel();
            
            upgradeButton.interactable = canAfford && !isMaxLevel;
            
            if (isMaxLevel && buttonText != null)
            {
                buttonText.text = "MAX";
            }
        }

        private void UpdateLevelDisplay()
        {
            if (upgradeLevelText == null || _upgradeData == null) return;

            int currentLevel = GetCurrentUpgradeLevel();
            
            if (currentLevel > 0)
            {
                if (_upgradeData.maxLevel > 0)
                {
                    upgradeLevelText.text = $"Level {currentLevel}/{_upgradeData.maxLevel}";
                }
                else
                {
                    upgradeLevelText.text = $"Level {currentLevel}";
                }
                upgradeLevelText.gameObject.SetActive(true);
            }
            else
            {
                upgradeLevelText.gameObject.SetActive(false);
            }
        }

        private bool CanAffordUpgrade()
        {
            if (_upgradeData == null) return false;

            int currentLevel = GetCurrentUpgradeLevel();
            long cost = _upgradeData.CalculatePrice(currentLevel);

            switch (_upgradeData.currencyType)
            {
                case UpgradeData.CurrencyType.Money:
                    return GameModel.Instance.Money >= cost && GameModel.Instance.IsMoneyUnlocked;
                    
                case UpgradeData.CurrencyType.Experience:
                    return GameModel.Instance.Experience >= cost;
                    
                default:
                    return false;
            }
        }

        private bool IsMaxLevel()
        {
            if (_upgradeData == null || _upgradeData.maxLevel <= 0) return false;

            int currentLevel = GetCurrentUpgradeLevel();
            return currentLevel >= _upgradeData.maxLevel;
        }

        private int GetCurrentUpgradeLevel()
        {
            if (_upgradeData == null || UpgradeManager.Instance == null) return 0;
            
            return UpgradeManager.Instance.GetUpgradeLevel(_upgradeData.upgradeId);
        }

        private void OnUpgradeButtonClick()
        {
            if (CanAffordUpgrade() && !IsMaxLevel())
            {
                _onPurchaseRequested?.Invoke();
            }
        }

        public void SetHighlighted(bool highlighted)
        {
            if (backgroundImage != null)
            {
                var color = backgroundImage.color;
                color.a = highlighted ? 0.15f : 0.1f;
                backgroundImage.color = color;
            }
        }

        #region Effect Methods

        public void PlayPurchaseEffect()
        {
            // Scale effect when purchased
            StartCoroutine(PurchaseAnimation());
        }

        private System.Collections.IEnumerator PurchaseAnimation()
        {
            var originalScale = transform.localScale;
            var targetScale = originalScale * 1.1f;
            
            // Scale up
            float time = 0;
            while (time < 0.1f)
            {
                time += Time.deltaTime;
                float t = time / 0.1f;
                transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
                yield return null;
            }
            
            // Scale back down
            time = 0;
            while (time < 0.2f)
            {
                time += Time.deltaTime;
                float t = time / 0.2f;
                transform.localScale = Vector3.Lerp(targetScale, originalScale, t);
                yield return null;
            }
            
            transform.localScale = originalScale;
        }

        #endregion

        private void OnDestroy()
        {
            if (upgradeButton != null)
            {
                upgradeButton.onClick.RemoveListener(OnUpgradeButtonClick);
            }
        }
    }
}