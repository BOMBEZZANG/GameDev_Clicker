using UnityEngine;

namespace GameDevClicker.Data.ScriptableObjects
{
    [CreateAssetMenu(fileName = "UpgradeData", menuName = "Game/Upgrade Data")]
    public class UpgradeData : ScriptableObject
    {
        [Header("Basic Information")]
        public string upgradeId;
        public string upgradeName;
        [TextArea(3, 5)]
        public string description;
        public Sprite icon;
        
        [Header("Category & Currency")]
        public CurrencyType currencyType;
        public UpgradeCategory category;
        
        [Header("Pricing")]
        public float basePrice;
        public float priceMultiplier = 1.15f;
        
        [Header("Effects")]
        public UpgradeEffect[] effects;
        
        [Header("Limits")]
        public int maxLevel = -1; // -1 means no limit
        
        [Header("Unlock Requirements")]
        public int requiredStage = 1;
        public int requiredLevel = 1;
        public string[] requiredUpgrades;
        
        public enum CurrencyType 
        { 
            Money, 
            Experience 
        }
        
        public enum UpgradeCategory 
        { 
            Skills, 
            Equipment, 
            Team 
        }
        
        public long CalculatePrice(int currentLevel)
        {
            if (currentLevel == 0) return (long)basePrice;
            return (long)(basePrice * Mathf.Pow(priceMultiplier, currentLevel));
        }
        
        public bool IsUnlocked(int playerStage, int playerLevel, System.Collections.Generic.HashSet<string> purchasedUpgrades)
        {
            if (playerStage < requiredStage || playerLevel < requiredLevel)
                return false;
                
            if (requiredUpgrades != null && requiredUpgrades.Length > 0)
            {
                foreach (string requiredUpgrade in requiredUpgrades)
                {
                    if (!purchasedUpgrades.Contains(requiredUpgrade))
                        return false;
                }
            }
            
            return true;
        }
    }
    
    [System.Serializable]
    public class UpgradeEffect
    {
        [Header("Effect Configuration")]
        public EffectType type;
        public float value;
        public bool isMultiplier = false;
        
        [Header("Scaling")]
        public bool scalesWithLevel = true;
        public float scalingMultiplier = 1f;
        
        public enum EffectType
        {
            // Click Effects
            MoneyPerClick,
            ExpPerClick,
            
            // Auto Income
            AutoMoney,
            AutoExp,
            
            // Multipliers
            AllMultiplier,
            MoneyMultiplier,
            ExpMultiplier,
            
            // Special Effects
            ClickCriticalChance,
            ClickCriticalMultiplier,
            OfflineEarnings,
            ProjectSpeedMultiplier,
            
            // Advanced Effects
            GlobalEfficiency,
            SkillLearningSpeed,
            TeamProductivity
        }
        
        public float CalculateEffectValue(int level)
        {
            if (!scalesWithLevel) return value;
            
            if (isMultiplier)
            {
                return 1f + (value - 1f) * level * scalingMultiplier;
            }
            else
            {
                return value * level * scalingMultiplier;
            }
        }
        
        public string GetEffectDescription(int level)
        {
            float effectValue = CalculateEffectValue(level);
            
            switch (type)
            {
                case EffectType.MoneyPerClick:
                    return $"+{effectValue:F1} money per click";
                case EffectType.ExpPerClick:
                    return $"+{effectValue:F1} experience per click";
                case EffectType.AutoMoney:
                    return $"+{effectValue:F1} money per second";
                case EffectType.AutoExp:
                    return $"+{effectValue:F1} experience per second";
                case EffectType.AllMultiplier:
                    return $"x{effectValue:F2} all earnings";
                case EffectType.MoneyMultiplier:
                    return $"x{effectValue:F2} money earnings";
                case EffectType.ExpMultiplier:
                    return $"x{effectValue:F2} experience earnings";
                case EffectType.ClickCriticalChance:
                    return $"{effectValue:F1}% critical click chance";
                case EffectType.ClickCriticalMultiplier:
                    return $"x{effectValue:F2} critical click multiplier";
                case EffectType.OfflineEarnings:
                    return $"x{effectValue:F2} offline earnings";
                case EffectType.ProjectSpeedMultiplier:
                    return $"x{effectValue:F2} project completion speed";
                case EffectType.GlobalEfficiency:
                    return $"+{effectValue:F1}% global efficiency";
                case EffectType.SkillLearningSpeed:
                    return $"x{effectValue:F2} skill learning speed";
                case EffectType.TeamProductivity:
                    return $"x{effectValue:F2} team productivity";
                default:
                    return $"+{effectValue:F1} effect";
            }
        }
    }
}