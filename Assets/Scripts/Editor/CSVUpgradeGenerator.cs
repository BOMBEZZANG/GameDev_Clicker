using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using GameDevClicker.Data.ScriptableObjects;
using GameDevClicker.Data;

namespace GameDevClicker.Editor
{
    public class CSVUpgradeGenerator : EditorWindow
    {
        private static readonly string UPGRADE_ASSET_PATH = "Assets/GameData/Upgrades/";
        private static readonly string CSV_PATH = "Assets/GameData/CSV/Balancing/Upgrades.csv";
        
        private bool overwriteExisting = false;
        private bool generateIcons = true;
        private bool groupByCategory = true;
        private Vector2 scrollPosition;
        
        [MenuItem("Game Tools/CSV Upgrade Generator")]
        public static void ShowWindow()
        {
            GetWindow<CSVUpgradeGenerator>("CSV Upgrade Generator");
        }

        private void OnGUI()
        {
            GUILayout.Label("CSV to UpgradeData Generator", EditorStyles.boldLabel);
            GUILayout.Space(10);

            // Settings
            GUILayout.Label("Settings", EditorStyles.boldLabel);
            overwriteExisting = EditorGUILayout.Toggle("Overwrite Existing Assets", overwriteExisting);
            generateIcons = EditorGUILayout.Toggle("Generate Default Icons", generateIcons);
            groupByCategory = EditorGUILayout.Toggle("Group by Category in Folders", groupByCategory);
            
            GUILayout.Space(10);
            
            // File paths
            GUILayout.Label("Paths", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("CSV File:", CSV_PATH);
            EditorGUILayout.LabelField("Output Path:", UPGRADE_ASSET_PATH);
            
            GUILayout.Space(10);

            // Action buttons
            if (GUILayout.Button("Generate All Upgrades from CSV", GUILayout.Height(30)))
            {
                GenerateAllUpgrades();
            }
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("Preview CSV Data", GUILayout.Height(25)))
            {
                PreviewCSVData();
            }
            
            GUILayout.Space(10);
            
            // Scroll view for preview
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            if (!string.IsNullOrEmpty(previewText))
            {
                GUILayout.Label("CSV Preview:", EditorStyles.boldLabel);
                EditorGUILayout.TextArea(previewText, GUILayout.ExpandHeight(true));
            }
            EditorGUILayout.EndScrollView();
        }

        private string previewText = "";

        private void PreviewCSVData()
        {
            string fullPath = Path.Combine(Application.dataPath.Replace("Assets", ""), CSV_PATH);
            
            if (!File.Exists(fullPath))
            {
                previewText = $"ERROR: CSV file not found at {fullPath}";
                return;
            }

            try
            {
                var csvData = LoadCSVData();
                previewText = $"Found {csvData.Count} upgrades in CSV:\n\n";
                
                foreach (var upgrade in csvData)
                {
                    previewText += $"ID: {upgrade.upgradeId} | Category: {upgrade.category} | Name: {upgrade.nameEn}\n";
                    previewText += $"  Price: {upgrade.basePrice} {upgrade.currencyType} | Effect: {upgrade.effectType} ({upgrade.effectValue})\n\n";
                }
            }
            catch (System.Exception e)
            {
                previewText = $"ERROR reading CSV: {e.Message}";
            }
        }

        private void GenerateAllUpgrades()
        {
            try
            {
                var csvData = LoadCSVData();
                
                if (csvData.Count == 0)
                {
                    EditorUtility.DisplayDialog("Error", "No upgrade data found in CSV file!", "OK");
                    return;
                }

                // Ensure output directory exists
                EnsureDirectoryExists(UPGRADE_ASSET_PATH);

                int createdCount = 0;
                int updatedCount = 0;
                int skippedCount = 0;

                foreach (var csvUpgrade in csvData)
                {
                    string assetPath = GetAssetPath(csvUpgrade);
                    
                    // Check if asset already exists
                    bool assetExists = AssetDatabase.LoadAssetAtPath<UpgradeData>(assetPath) != null;
                    
                    if (assetExists && !overwriteExisting)
                    {
                        skippedCount++;
                        continue;
                    }

                    // Create or update the ScriptableObject
                    UpgradeData upgradeAsset = assetExists ? 
                        AssetDatabase.LoadAssetAtPath<UpgradeData>(assetPath) : 
                        ScriptableObject.CreateInstance<UpgradeData>();

                    // Populate the asset with CSV data
                    PopulateUpgradeData(upgradeAsset, csvUpgrade);

                    if (assetExists)
                    {
                        EditorUtility.SetDirty(upgradeAsset);
                        updatedCount++;
                    }
                    else
                    {
                        AssetDatabase.CreateAsset(upgradeAsset, assetPath);
                        createdCount++;
                    }
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                string resultMessage = $"Generation Complete!\n\nCreated: {createdCount}\nUpdated: {updatedCount}\nSkipped: {skippedCount}";
                EditorUtility.DisplayDialog("Success", resultMessage, "OK");
                
                Debug.Log($"[CSVUpgradeGenerator] {resultMessage}");
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("Error", $"Failed to generate upgrades: {e.Message}", "OK");
                Debug.LogError($"[CSVUpgradeGenerator] Error: {e.Message}\n{e.StackTrace}");
            }
        }

        private List<UpgradeInfo> LoadCSVData()
        {
            var csvLoader = ScriptableObject.CreateInstance<CSVLoaderHelper>();
            return csvLoader.LoadUpgradeCSV(CSV_PATH);
        }

        private string GetAssetPath(UpgradeInfo upgrade)
        {
            string fileName = $"{upgrade.upgradeId}.asset";
            
            if (groupByCategory)
            {
                string categoryFolder = GetCategoryFolderName(upgrade.category);
                string categoryPath = Path.Combine(UPGRADE_ASSET_PATH, categoryFolder);
                EnsureDirectoryExists(categoryPath);
                return Path.Combine(categoryPath, fileName).Replace('\\', '/');
            }
            else
            {
                return Path.Combine(UPGRADE_ASSET_PATH, fileName).Replace('\\', '/');
            }
        }

        private string GetCategoryFolderName(string category)
        {
            switch (category.ToLower())
            {
                case "skills": return "Skills";
                case "equipment": return "Equipment";
                case "team": return "Team";
                default: return "Other";
            }
        }

        private void EnsureDirectoryExists(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parentPath = Path.GetDirectoryName(path).Replace('\\', '/');
                string folderName = Path.GetFileName(path);
                AssetDatabase.CreateFolder(parentPath, folderName);
            }
        }

        private void PopulateUpgradeData(UpgradeData asset, UpgradeInfo csv)
        {
            // Basic Information
            asset.upgradeId = csv.upgradeId;
            asset.upgradeName = csv.nameEn;
            asset.description = csv.descriptionEn;

            // Category & Currency
            asset.category = ParseCategory(csv.category);
            asset.currencyType = ParseCurrencyType(csv.currencyType);

            // Pricing
            asset.basePrice = csv.basePrice;
            asset.priceMultiplier = csv.priceMultiplier > 0 ? csv.priceMultiplier : 1.15f;

            // Limits
            asset.maxLevel = csv.maxLevel > 0 ? csv.maxLevel : -1;

            // Unlock Requirements
            ParseUnlockConditions(asset, csv.unlockCondition);

            // Effects
            asset.effects = CreateEffects(csv);

            // Generate icon if needed
            if (generateIcons && asset.icon == null)
            {
                asset.icon = GenerateDefaultIcon(asset.category);
            }
        }

        private UpgradeData.UpgradeCategory ParseCategory(string category)
        {
            switch (category.ToLower())
            {
                case "skills": return UpgradeData.UpgradeCategory.Skills;
                case "equipment": return UpgradeData.UpgradeCategory.Equipment;
                case "team": return UpgradeData.UpgradeCategory.Team;
                default: return UpgradeData.UpgradeCategory.Skills;
            }
        }

        private UpgradeData.CurrencyType ParseCurrencyType(string currency)
        {
            switch (currency.ToLower())
            {
                case "money": return UpgradeData.CurrencyType.Money;
                case "experience": return UpgradeData.CurrencyType.Experience;
                default: return UpgradeData.CurrencyType.Experience;
            }
        }

        private void ParseUnlockConditions(UpgradeData asset, string unlockCondition)
        {
            asset.requiredLevel = 1;
            asset.requiredStage = 1;
            asset.requiredUpgrades = new string[0];

            if (string.IsNullOrEmpty(unlockCondition) || unlockCondition == "none")
                return;

            if (unlockCondition.StartsWith("level_"))
            {
                if (int.TryParse(unlockCondition.Substring(6), out int level))
                    asset.requiredLevel = level;
            }
            else if (unlockCondition.StartsWith("stage_"))
            {
                if (int.TryParse(unlockCondition.Substring(6), out int stage))
                    asset.requiredStage = stage;
            }
        }

        private UpgradeEffect[] CreateEffects(UpgradeInfo csv)
        {
            var effect = new UpgradeEffect();
            effect.type = ParseEffectType(csv.effectType);
            effect.value = csv.effectValue;
            effect.scalesWithLevel = true;
            effect.scalingMultiplier = 1f;

            // Set appropriate flags based on effect type
            switch (effect.type)
            {
                case UpgradeEffect.EffectType.AllMultiplier:
                case UpgradeEffect.EffectType.MoneyMultiplier:
                case UpgradeEffect.EffectType.ExpMultiplier:
                case UpgradeEffect.EffectType.ProjectSpeedMultiplier:
                    effect.isMultiplier = true;
                    effect.value = 1f + csv.effectValue; // Convert additive to multiplier format
                    break;
                default:
                    effect.isMultiplier = false;
                    break;
            }

            return new UpgradeEffect[] { effect };
        }

        private UpgradeEffect.EffectType ParseEffectType(string effectType)
        {
            switch (effectType.ToLower())
            {
                case "exp_per_click": return UpgradeEffect.EffectType.ExpPerClick;
                case "money_per_click": return UpgradeEffect.EffectType.MoneyPerClick;
                case "auto_money": return UpgradeEffect.EffectType.AutoMoney;
                case "auto_exp": return UpgradeEffect.EffectType.AutoExp;
                case "exp_multiplier": return UpgradeEffect.EffectType.ExpMultiplier;
                case "money_multiplier": return UpgradeEffect.EffectType.MoneyMultiplier;
                case "all_multiplier": return UpgradeEffect.EffectType.AllMultiplier;
                case "project_speed": return UpgradeEffect.EffectType.ProjectSpeedMultiplier;
                case "critical_chance": return UpgradeEffect.EffectType.ClickCriticalChance;
                case "critical_multiplier": return UpgradeEffect.EffectType.ClickCriticalMultiplier;
                case "offline_earnings": return UpgradeEffect.EffectType.OfflineEarnings;
                case "global_efficiency": return UpgradeEffect.EffectType.GlobalEfficiency;
                case "skill_learning_speed": return UpgradeEffect.EffectType.SkillLearningSpeed;
                case "team_productivity": return UpgradeEffect.EffectType.TeamProductivity;
                default: 
                    Debug.LogWarning($"Unknown effect type: {effectType}, defaulting to ExpPerClick");
                    return UpgradeEffect.EffectType.ExpPerClick;
            }
        }

        private Sprite GenerateDefaultIcon(UpgradeData.UpgradeCategory category)
        {
            // Try to find default icons in Resources folder
            string iconName = "";
            switch (category)
            {
                case UpgradeData.UpgradeCategory.Skills:
                    iconName = "Icons/skill_default";
                    break;
                case UpgradeData.UpgradeCategory.Equipment:
                    iconName = "Icons/equipment_default";
                    break;
                case UpgradeData.UpgradeCategory.Team:
                    iconName = "Icons/team_default";
                    break;
            }

            return Resources.Load<Sprite>(iconName);
        }
    }

    // Helper class to load CSV data without relying on runtime singletons
    public class CSVLoaderHelper : ScriptableObject
    {
        public List<UpgradeInfo> LoadUpgradeCSV(string csvPath)
        {
            string fullPath = Path.Combine(Application.dataPath.Replace("Assets", ""), csvPath);
            
            if (!File.Exists(fullPath))
            {
                throw new System.Exception($"CSV file not found: {fullPath}");
            }

            var upgrades = new List<UpgradeInfo>();
            string[] lines = File.ReadAllLines(fullPath);
            
            if (lines.Length < 2)
            {
                throw new System.Exception("CSV file is empty or has no data rows");
            }

            string[] headers = ParseCSVLine(lines[0]);
            
            for (int i = 1; i < lines.Length; i++)
            {
                try
                {
                    string[] values = ParseCSVLine(lines[i]);
                    if (values.Length != headers.Length)
                    {
                        Debug.LogWarning($"Skipping malformed line {i} in CSV");
                        continue;
                    }

                    var upgrade = new UpgradeInfo();
                    
                    // Map CSV columns to UpgradeInfo fields
                    for (int j = 0; j < headers.Length; j++)
                    {
                        string header = headers[j].ToLower();
                        string value = values[j];
                        
                        switch (header)
                        {
                            case "upgrade_id": upgrade.upgradeId = value; break;
                            case "category": upgrade.category = value; break;
                            case "name_en": upgrade.nameEn = value; break;
                            case "name_ko": upgrade.nameKo = value; break;
                            case "description_en": upgrade.descriptionEn = value; break;
                            case "description_ko": upgrade.descriptionKo = value; break;
                            case "currency_type": upgrade.currencyType = value; break;
                            case "base_price": upgrade.basePrice = ParseFloat(value); break;
                            case "price_multiplier": upgrade.priceMultiplier = ParseFloat(value); break;
                            case "effect_type": upgrade.effectType = value; break;
                            case "effect_value": upgrade.effectValue = ParseFloat(value); break;
                            case "max_level": upgrade.maxLevel = ParseInt(value); break;
                            case "unlock_condition": upgrade.unlockCondition = value; break;
                        }
                    }
                    
                    upgrades.Add(upgrade);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error parsing line {i}: {e.Message}");
                }
            }

            return upgrades;
        }

        private string[] ParseCSVLine(string line)
        {
            var result = new List<string>();
            bool inQuotes = false;
            string currentField = "";

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(currentField);
                    currentField = "";
                }
                else
                {
                    currentField += c;
                }
            }

            result.Add(currentField);
            return result.ToArray();
        }

        private float ParseFloat(string value)
        {
            if (float.TryParse(value, out float result))
                return result;
            return 0f;
        }

        private int ParseInt(string value)
        {
            if (int.TryParse(value, out int result))
                return result;
            return 0;
        }
    }
}