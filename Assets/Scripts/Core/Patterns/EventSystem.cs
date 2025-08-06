using System;
using UnityEngine;
using GameDevClicker.Data.ScriptableObjects;

namespace GameDevClicker.Core.Patterns
{
    public static class GameEvents
    {
        // Currency Events
        public static event Action<long> OnMoneyChanged;
        public static event Action<long> OnExperienceChanged;
        public static event Action<float, float> OnClickValueChanged; // money per click, exp per click
        public static event Action<float, float> OnAutoIncomeChanged; // auto money, auto exp
        
        // Click Events
        public static event Action<float, float> OnClickPerformed; // money gained, exp gained
        public static event Action<Vector2> OnCriticalClick; // position for effect
        
        // Progression Events
        public static event Action<int> OnStageUnlocked;
        public static event Action<int> OnLevelUp;
        public static event Action<UpgradeData> OnUpgradePurchased;
        public static event Action<string> OnFeatureUnlocked; // feature name
        
        // Project System Events
        public static event Action<long> OnProjectCompleted; // reward amount
        public static event Action<float> OnProjectProgressChanged; // 0.0 to 1.0
        
        // UI Events
        public static event Action<UpgradeData.UpgradeCategory> OnUpgradeTabChanged;
        public static event Action<string, string> OnNotificationShown; // title, message
        
        // System Events
        public static event Action OnGameLoaded;
        public static event Action OnGameSaved;
        public static event Action<OfflineEarnings> OnOfflineEarningsCalculated;
        public static event Action<float> OnProgressChanged;

        // Currency Event Invokers
        public static void InvokeMoneyChanged(long newValue)
        {
            OnMoneyChanged?.Invoke(newValue);
        }

        public static void InvokeExperienceChanged(long newValue)
        {
            OnExperienceChanged?.Invoke(newValue);
        }

        public static void InvokeClickValueChanged(float moneyPerClick, float expPerClick)
        {
            OnClickValueChanged?.Invoke(moneyPerClick, expPerClick);
        }

        public static void InvokeAutoIncomeChanged(float autoMoney, float autoExp)
        {
            OnAutoIncomeChanged?.Invoke(autoMoney, autoExp);
        }

        // Click Event Invokers
        public static void InvokeClickPerformed(float moneyGained, float expGained)
        {
            OnClickPerformed?.Invoke(moneyGained, expGained);
        }

        public static void InvokeCriticalClick(Vector2 position)
        {
            OnCriticalClick?.Invoke(position);
        }

        // Progression Event Invokers
        public static void InvokeStageUnlocked(int stageId)
        {
            OnStageUnlocked?.Invoke(stageId);
        }

        public static void InvokeLevelUp(int newLevel)
        {
            OnLevelUp?.Invoke(newLevel);
        }

        public static void InvokeUpgradePurchased(UpgradeData upgrade)
        {
            OnUpgradePurchased?.Invoke(upgrade);
        }

        public static void InvokeFeatureUnlocked(string featureName)
        {
            OnFeatureUnlocked?.Invoke(featureName);
        }

        // Project System Event Invokers
        public static void InvokeProjectCompleted(long reward)
        {
            OnProjectCompleted?.Invoke(reward);
        }

        public static void InvokeProjectProgressChanged(float progress)
        {
            OnProjectProgressChanged?.Invoke(progress);
        }

        // UI Event Invokers
        public static void InvokeUpgradeTabChanged(UpgradeData.UpgradeCategory category)
        {
            OnUpgradeTabChanged?.Invoke(category);
        }

        public static void InvokeNotificationShown(string title, string message)
        {
            OnNotificationShown?.Invoke(title, message);
        }

        // System Event Invokers
        public static void InvokeGameLoaded()
        {
            OnGameLoaded?.Invoke();
        }

        public static void InvokeGameSaved()
        {
            OnGameSaved?.Invoke();
        }

        public static void InvokeOfflineEarningsCalculated(OfflineEarnings earnings)
        {
            OnOfflineEarningsCalculated?.Invoke(earnings);
        }

        public static void InvokeProgressChanged(float progress)
        {
            OnProgressChanged?.Invoke(progress);
        }

        public static void ClearAllListeners()
        {
            OnMoneyChanged = null;
            OnExperienceChanged = null;
            OnClickValueChanged = null;
            OnAutoIncomeChanged = null;
            OnClickPerformed = null;
            OnCriticalClick = null;
            OnStageUnlocked = null;
            OnLevelUp = null;
            OnUpgradePurchased = null;
            OnFeatureUnlocked = null;
            OnProjectCompleted = null;
            OnProjectProgressChanged = null;
            OnUpgradeTabChanged = null;
            OnNotificationShown = null;
            OnGameLoaded = null;
            OnGameSaved = null;
            OnOfflineEarningsCalculated = null;
            OnProgressChanged = null;
        }
    }

    [System.Serializable]
    public class OfflineEarnings
    {
        public long money;
        public long experience;
        public TimeSpan timeOffline;
    }

}