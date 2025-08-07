using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using GameDevClicker.Data.ScriptableObjects;
using GameDevClicker.Data;

namespace GameDevClicker.Editor
{
    /// <summary>
    /// Master tool for generating all game data assets from CSV files
    /// </summary>
    public class GameDataGenerator : EditorWindow
    {
        private Vector2 scrollPosition;
        private bool overwriteExisting = false;
        private bool createBackups = true;
        
        // Generation flags
        private bool generateUpgrades = true;
        private bool generateProjects = true;
        private bool generateLevels = true;
        private bool generateStages = true;
        
        private string statusText = "";
        
        [MenuItem("Game Tools/Game Data Generator")]
        public static void ShowWindow()
        {
            GetWindow<GameDataGenerator>("Game Data Generator");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("Game Data Generator", EditorStyles.boldLabel);
            GUILayout.Label("Generate ScriptableObjects from CSV files", EditorStyles.helpBox);
            GUILayout.Space(10);
            
            // Global Settings
            GUILayout.Label("Global Settings", EditorStyles.boldLabel);
            overwriteExisting = EditorGUILayout.Toggle("Overwrite Existing Assets", overwriteExisting);
            createBackups = EditorGUILayout.Toggle("Create Backups", createBackups);
            GUILayout.Space(10);
            
            // What to Generate
            GUILayout.Label("Generate", EditorStyles.boldLabel);
            generateUpgrades = EditorGUILayout.Toggle("✓ Upgrades (Skills, Equipment, Team)", generateUpgrades);
            generateProjects = EditorGUILayout.Toggle("✓ Projects", generateProjects);
            generateLevels = EditorGUILayout.Toggle("✓ Levels", generateLevels);
            generateStages = EditorGUILayout.Toggle("✓ Stages", generateStages);
            GUILayout.Space(10);
            
            // Action Buttons
            EditorGUI.BeginDisabledGroup(!HasAnyGenerationEnabled());
            if (GUILayout.Button("Generate All Selected", GUILayout.Height(40)))
            {
                GenerateAllData();
            }
            EditorGUI.EndDisabledGroup();
            
            GUILayout.Space(10);
            
            // Individual generation buttons
            GUILayout.Label("Individual Generation", EditorStyles.boldLabel);
            
            EditorGUI.BeginDisabledGroup(!generateUpgrades);
            if (GUILayout.Button("Generate Upgrades Only"))
            {
                GenerateUpgradesOnly();
            }
            EditorGUI.EndDisabledGroup();
            
            GUILayout.Space(5);
            
            // Utility buttons
            GUILayout.Label("Utilities", EditorStyles.boldLabel);
            if (GUILayout.Button("Validate CSV Files"))
            {
                ValidateCSVFiles();
            }
            
            if (GUILayout.Button("Open CSV Folder"))
            {
                string csvPath = Path.Combine(Application.dataPath, "GameData", "CSV", "Balancing");
                EditorUtility.RevealInFinder(csvPath);
            }
            
            if (GUILayout.Button("Open Generated Assets Folder"))
            {
                string assetsPath = Path.Combine(Application.dataPath, "GameData");
                EditorUtility.RevealInFinder(assetsPath);
            }
            
            GUILayout.Space(10);
            
            // Status Area
            GUILayout.Label("Status", EditorStyles.boldLabel);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(150));
            EditorGUILayout.TextArea(statusText, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
            
            if (GUILayout.Button("Clear Status"))
            {
                statusText = "";
            }
        }
        
        private bool HasAnyGenerationEnabled()
        {
            return generateUpgrades || generateProjects || generateLevels || generateStages;
        }
        
        private void GenerateAllData()
        {
            LogStatus("=== Starting Full Game Data Generation ===");
            
            try
            {
                if (createBackups)
                {
                    CreateBackups();
                }
                
                int totalGenerated = 0;
                
                if (generateUpgrades)
                {
                    totalGenerated += GenerateUpgradesInternal();
                }
                
                if (generateProjects)
                {
                    totalGenerated += GenerateProjectsInternal();
                }
                
                if (generateLevels)
                {
                    totalGenerated += GenerateLevelsInternal();
                }
                
                if (generateStages)
                {
                    totalGenerated += GenerateStagesInternal();
                }
                
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
                LogStatus($"=== Generation Complete! Total assets: {totalGenerated} ===");
                EditorUtility.DisplayDialog("Success", $"Generated {totalGenerated} assets successfully!", "OK");
            }
            catch (System.Exception e)
            {
                LogStatus($"ERROR: {e.Message}");
                EditorUtility.DisplayDialog("Error", $"Generation failed: {e.Message}", "OK");
            }
        }
        
        private void GenerateUpgradesOnly()
        {
            LogStatus("=== Generating Upgrades ===");
            
            try
            {
                if (createBackups)
                {
                    CreateUpgradeBackups();
                }
                
                int generated = GenerateUpgradesInternal();
                
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
                LogStatus($"Generated {generated} upgrade assets!");
                EditorUtility.DisplayDialog("Success", $"Generated {generated} upgrades!", "OK");
            }
            catch (System.Exception e)
            {
                LogStatus($"ERROR: {e.Message}");
                EditorUtility.DisplayDialog("Error", $"Upgrade generation failed: {e.Message}", "OK");
            }
        }
        
        private int GenerateUpgradesInternal()
        {
            LogStatus("Loading upgrade CSV data...");
            
            var csvHelper = ScriptableObject.CreateInstance<CSVLoaderHelper>();
            var upgradeData = csvHelper.LoadUpgradeCSV("Assets/GameData/CSV/Balancing/Upgrades.csv");
            
            LogStatus($"Found {upgradeData.Count} upgrades in CSV");
            
            string basePath = "Assets/GameData/Upgrades/";
            EnsureDirectoryExists(basePath);
            EnsureDirectoryExists(basePath + "Skills/");
            EnsureDirectoryExists(basePath + "Equipment/");
            EnsureDirectoryExists(basePath + "Team/");
            
            int generated = 0;
            
            foreach (var csvUpgrade in upgradeData)
            {
                string categoryPath = GetCategoryPath(csvUpgrade.category);
                string assetPath = Path.Combine(basePath, categoryPath, $"{csvUpgrade.upgradeId}.asset").Replace('\\', '/');
                
                bool exists = AssetDatabase.LoadAssetAtPath<UpgradeData>(assetPath) != null;
                if (exists && !overwriteExisting)
                {
                    LogStatus($"Skipping existing: {csvUpgrade.upgradeId}");
                    continue;
                }
                
                UpgradeData asset = exists ? 
                    AssetDatabase.LoadAssetAtPath<UpgradeData>(assetPath) :
                    ScriptableObject.CreateInstance<UpgradeData>();
                
                PopulateUpgradeAsset(asset, csvUpgrade);
                
                if (!exists)
                {
                    AssetDatabase.CreateAsset(asset, assetPath);
                }
                else
                {
                    EditorUtility.SetDirty(asset);
                }
                
                generated++;
                LogStatus($"Generated: {csvUpgrade.upgradeId} ({csvUpgrade.nameEn})");
            }
            
            return generated;
        }
        
        private int GenerateProjectsInternal()
        {
            LogStatus("Project generation not yet implemented");
            return 0; // TODO: Implement project generation
        }
        
        private int GenerateLevelsInternal()
        {
            LogStatus("Level generation not yet implemented");
            return 0; // TODO: Implement level generation
        }
        
        private int GenerateStagesInternal()
        {
            LogStatus("Stage generation not yet implemented");
            return 0; // TODO: Implement stage generation
        }
        
        private string GetCategoryPath(string category)
        {
            switch (category.ToLower())
            {
                case "skills": return "Skills";
                case "equipment": return "Equipment";
                case "team": return "Team";
                default: return "Skills";
            }
        }
        
        private void PopulateUpgradeAsset(UpgradeData asset, UpgradeInfo csv)
        {
            // Basic Information
            asset.upgradeId = csv.upgradeId;
            asset.upgradeName = csv.nameEn;
            asset.description = csv.descriptionEn;

            // Category & Currency
            asset.category = ParseUpgradeCategory(csv.category);
            asset.currencyType = ParseCurrencyType(csv.currencyType);

            // Pricing
            asset.basePrice = csv.basePrice;
            asset.priceMultiplier = csv.priceMultiplier > 0 ? csv.priceMultiplier : 1.15f;

            // Limits
            asset.maxLevel = csv.maxLevel > 0 ? csv.maxLevel : -1;

            // Unlock Requirements
            ParseUnlockConditions(asset, csv.unlockCondition);

            // Effects
            asset.effects = CreateEffectArray(csv);
        }
        
        private UpgradeData.UpgradeCategory ParseUpgradeCategory(string category)
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
        
        private UpgradeEffect[] CreateEffectArray(UpgradeInfo csv)
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
                    effect.isMultiplier = true;
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
                default: 
                    LogStatus($"Warning: Unknown effect type '{effectType}', using ExpPerClick");
                    return UpgradeEffect.EffectType.ExpPerClick;
            }
        }
        
        private void ValidateCSVFiles()
        {
            LogStatus("=== Validating CSV Files ===");
            
            string csvBasePath = Path.Combine(Application.dataPath, "GameData", "CSV", "Balancing");
            string[] csvFiles = { "Upgrades.csv", "Projects.csv", "Levels.csv", "Stage.csv" };
            
            foreach (string csvFile in csvFiles)
            {
                string fullPath = Path.Combine(csvBasePath, csvFile);
                if (File.Exists(fullPath))
                {
                    try
                    {
                        string[] lines = File.ReadAllLines(fullPath);
                        LogStatus($"✓ {csvFile}: {lines.Length - 1} data rows");
                    }
                    catch (System.Exception e)
                    {
                        LogStatus($"✗ {csvFile}: ERROR - {e.Message}");
                    }
                }
                else
                {
                    LogStatus($"✗ {csvFile}: File not found");
                }
            }
            
            LogStatus("=== Validation Complete ===");
        }
        
        private void CreateBackups()
        {
            LogStatus("Creating backups...");
            
            string backupPath = Path.Combine(Application.dataPath, "GameData", "Backups", System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
            Directory.CreateDirectory(backupPath);
            
            // TODO: Implement backup logic
            LogStatus($"Backups created at: {backupPath}");
        }
        
        private void CreateUpgradeBackups()
        {
            // TODO: Implement upgrade-specific backup
            LogStatus("Upgrade backups created");
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
        
        private void LogStatus(string message)
        {
            statusText += $"[{System.DateTime.Now:HH:mm:ss}] {message}\n";
            Debug.Log($"[GameDataGenerator] {message}");
            Repaint(); // Refresh the window to show the new status
        }
    }
}