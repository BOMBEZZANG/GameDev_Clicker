using UnityEngine;

namespace GameDevClicker.Data.ScriptableObjects
{
    [CreateAssetMenu(fileName = "BalanceSettings", menuName = "Game/Balance Settings")]
    public class BalanceSettings : ScriptableObject
    {
        [Header("Click Values")]
        public AnimationCurve moneyPerClickCurve = AnimationCurve.Linear(1, 1, 10, 10);
        public AnimationCurve expPerClickCurve = AnimationCurve.Linear(1, 1, 10, 10);
        
        [Header("Level System")]
        public long baseExpPerLevel = 100;
        public float expLevelMultiplier = 1.5f;
        public int moneyUnlockLevel = 10;
        
        [Header("Stage Requirements")]
        public long[] stageExpRequirements = new long[]
        {
            1000,      // Stage 1 -> 2
            15000,     // Stage 2 -> 3
            225000,    // Stage 3 -> 4
            3375000,   // Stage 4 -> 5
            50625000,  // Stage 5 -> 6
            759375000, // Stage 6 -> 7
            // Add more stages as needed
        };
        
        [Header("Currency Ratios")]
        [Range(0.1f, 10f)]
        public float baseMoneyToExpRatio = 1.0f;
        public float[] stageMoneyMultipliers = { 1f, 1.2f, 1.5f, 2f, 3f, 5f, 8f, 12f, 18f, 25f };
        
        [Header("Auto Income")]
        public float autoIncomeUpdateInterval = 1f;
        public float baseAutoIncomeEfficiency = 0.1f;
        
        [Header("Critical Hits")]
        [Range(0f, 1f)]
        public float baseCriticalChance = 0.05f;
        [Range(1f, 10f)]
        public float baseCriticalMultiplier = 2f;
        
        [Header("Upgrade Categories")]
        public UpgradeCategoryBalance skills = new UpgradeCategoryBalance();
        public UpgradeCategoryBalance equipment = new UpgradeCategoryBalance();
        public UpgradeCategoryBalance team = new UpgradeCategoryBalance();
        
        [Header("Project System")]
        public ProjectSystemBalance projectSystem = new ProjectSystemBalance();
        
        [Header("Offline Earnings")]
        public OfflineEarningsBalance offlineEarnings = new OfflineEarningsBalance();
        
        [Header("Prestige System")]
        public PrestigeBalance prestige = new PrestigeBalance();

        public float GetMoneyPerClick(int level)
        {
            return moneyPerClickCurve.Evaluate(level);
        }

        public float GetExpPerClick(int level)
        {
            return expPerClickCurve.Evaluate(level);
        }

        public long GetStageExpRequirement(int stage)
        {
            if (stage <= 1 || stage - 2 >= stageExpRequirements.Length)
                return stageExpRequirements[stageExpRequirements.Length - 1];
            
            return stageExpRequirements[stage - 2];
        }

        public float GetStageMoneyMultiplier(int stage)
        {
            if (stage <= 0 || stage - 1 >= stageMoneyMultipliers.Length)
                return stageMoneyMultipliers[stageMoneyMultipliers.Length - 1];
                
            return stageMoneyMultipliers[stage - 1];
        }

        public long CalculateExpRequiredForLevel(int level)
        {
            if (level <= 1) return 0;
            
            long totalExp = 0;
            for (int i = 1; i < level; i++)
            {
                totalExp += (long)(baseExpPerLevel * Mathf.Pow(expLevelMultiplier, i - 1));
            }
            return totalExp;
        }

        public int CalculateLevelFromExp(long experience)
        {
            if (experience <= 0) return 1;
            
            int level = 1;
            long totalExpRequired = 0;
            
            while (true)
            {
                long expForNextLevel = (long)(baseExpPerLevel * Mathf.Pow(expLevelMultiplier, level - 1));
                if (totalExpRequired + expForNextLevel > experience)
                    break;
                    
                totalExpRequired += expForNextLevel;
                level++;
            }
            
            return level;
        }

        private void OnValidate()
        {
            // Ensure arrays have minimum required elements
            if (stageExpRequirements == null || stageExpRequirements.Length == 0)
            {
                stageExpRequirements = new long[] { 1000 };
            }
            
            if (stageMoneyMultipliers == null || stageMoneyMultipliers.Length == 0)
            {
                stageMoneyMultipliers = new float[] { 1f };
            }
            
            // Ensure positive values
            baseExpPerLevel = System.Math.Max(1L, baseExpPerLevel);
            expLevelMultiplier = Mathf.Max(1.1f, expLevelMultiplier);
            moneyUnlockLevel = Mathf.Max(1, moneyUnlockLevel);
            baseMoneyToExpRatio = Mathf.Max(0.1f, baseMoneyToExpRatio);
            autoIncomeUpdateInterval = Mathf.Max(0.1f, autoIncomeUpdateInterval);
        }
    }

    [System.Serializable]
    public class UpgradeCategoryBalance
    {
        [Header("Pricing")]
        [Range(0.1f, 10f)]
        public float basePriceMultiplier = 1.0f;
        [Range(1.01f, 2f)]
        public float priceGrowthRate = 1.15f;
        
        [Header("Effectiveness")]
        [Range(0.1f, 5f)]
        public float effectivenessMultiplier = 1.0f;
        
        [Header("Availability")]
        public int unlocksAtStage = 1;
        public int unlocksAtLevel = 1;
        
        public float CalculatePrice(float basePrice, int level)
        {
            return basePrice * basePriceMultiplier * Mathf.Pow(priceGrowthRate, level);
        }
        
        public float CalculateEffect(float baseEffect, int level)
        {
            return baseEffect * effectivenessMultiplier;
        }
    }

    [System.Serializable]
    public class ProjectSystemBalance
    {
        [Header("Base Values")]
        public float baseExpRequired = 1000f;
        public float requirementMultiplier = 1.5f;
        public float baseReward = 500f;
        public float rewardMultiplier = 1.3f;
        
        [Header("Difficulty Scaling")]
        [Range(0.5f, 3f)]
        public float easyProjectMultiplier = 1f;
        [Range(0.8f, 4f)]
        public float mediumProjectMultiplier = 1.3f;
        [Range(1.2f, 5f)]
        public float hardProjectMultiplier = 1.8f;
        [Range(1.5f, 8f)]
        public float expertProjectMultiplier = 2.5f;
        
        [Header("Unlock Requirements")]
        public int unlocksAtStage = 2;
        public int unlocksAtLevel = 1;
    }

    [System.Serializable]
    public class OfflineEarningsBalance
    {
        [Header("Time Limits")]
        public float maxOfflineHours = 8f;
        public float gracePeriodMinutes = 5f;
        
        [Header("Efficiency")]
        [Range(0.1f, 1f)]
        public float offlineEfficiency = 0.5f;
        [Range(0.1f, 2f)]
        public float autoIncomeEfficiency = 0.8f;
        
        [Header("Bonuses")]
        public bool enableOfflineBonus = true;
        [Range(1f, 3f)]
        public float maxOfflineBonus = 2f;
    }

    [System.Serializable]
    public class PrestigeBalance
    {
        [Header("Requirements")]
        public int minStageForPrestige = 5;
        public long minExperienceForPrestige = 1000000;
        
        [Header("Rewards")]
        public float basePrestigeMultiplier = 1.1f;
        public float prestigePointsPerStage = 1f;
        
        [Header("Costs")]
        public AnimationCurve prestigeUpgradeCostCurve = AnimationCurve.Linear(0, 1, 10, 100);
    }
}