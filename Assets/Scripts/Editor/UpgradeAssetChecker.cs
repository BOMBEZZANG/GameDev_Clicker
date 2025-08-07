using UnityEditor;
using UnityEngine;
using GameDevClicker.Data.ScriptableObjects;
using System.IO;

namespace GameDevClicker.Editor
{
    /// <summary>
    /// Tool to check which upgrade assets exist
    /// </summary>
    public class UpgradeAssetChecker : EditorWindow
    {
        [MenuItem("Game Tools/Upgrade Asset Checker")]
        public static void ShowWindow()
        {
            GetWindow<UpgradeAssetChecker>("Upgrade Asset Checker");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("Upgrade Asset Checker", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            if (GUILayout.Button("Check Generated Upgrade Assets", GUILayout.Height(30)))
            {
                CheckGeneratedAssets();
            }
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("Check Resources/Upgrades Folder", GUILayout.Height(30)))
            {
                CheckResourcesFolder();
            }
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("Copy Missing to Resources", GUILayout.Height(30)))
            {
                CopyMissingToResources();
            }
        }
        
        private void CheckGeneratedAssets()
        {
            Debug.Log("=== Checking Generated Upgrade Assets ===");
            
            string[] paths = {
                "Assets/GameData/Upgrades",
                "Assets/GameData/Upgrades/Skills",
                "Assets/GameData/Upgrades/Equipment",
                "Assets/GameData/Upgrades/Team"
            };
            
            int totalCount = 0;
            foreach (string path in paths)
            {
                if (AssetDatabase.IsValidFolder(path))
                {
                    string[] guids = AssetDatabase.FindAssets("t:UpgradeData", new[] { path });
                    Debug.Log($"{path}: {guids.Length} assets");
                    
                    foreach (string guid in guids)
                    {
                        string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                        UpgradeData upgrade = AssetDatabase.LoadAssetAtPath<UpgradeData>(assetPath);
                        if (upgrade != null)
                        {
                            Debug.Log($"  ✅ {upgrade.upgradeId}: {upgrade.upgradeName} (Req: Level {upgrade.requiredLevel}, Stage {upgrade.requiredStage})");
                            totalCount++;
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"{path}: Folder not found");
                }
            }
            
            Debug.Log($"Total generated assets: {totalCount}");
        }
        
        private void CheckResourcesFolder()
        {
            Debug.Log("=== Checking Resources/Upgrades ===");
            
            string resourcesPath = "Assets/Resources/Upgrades";
            
            if (!AssetDatabase.IsValidFolder(resourcesPath))
            {
                Debug.LogError($"❌ Resources/Upgrades folder doesn't exist!");
                Debug.Log("Use 'Game Tools → Setup Upgrades in Resources' to fix this");
                return;
            }
            
            string[] guids = AssetDatabase.FindAssets("t:UpgradeData", new[] { resourcesPath });
            Debug.Log($"Found {guids.Length} upgrade assets in Resources");
            
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                UpgradeData upgrade = AssetDatabase.LoadAssetAtPath<UpgradeData>(assetPath);
                if (upgrade != null)
                {
                    Debug.Log($"  ✅ {upgrade.upgradeId}: {upgrade.upgradeName}");
                }
            }
            
            if (guids.Length == 0)
            {
                Debug.LogError("❌ No upgrade assets in Resources folder!");
                Debug.Log("Click 'Copy Missing to Resources' to fix this");
            }
        }
        
        private void CopyMissingToResources()
        {
            Debug.Log("=== Copying Upgrades to Resources ===");
            
            // Ensure Resources/Upgrades exists
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }
            if (!AssetDatabase.IsValidFolder("Assets/Resources/Upgrades"))
            {
                AssetDatabase.CreateFolder("Assets/Resources", "Upgrades");
            }
            
            // Find all upgrade assets in GameData
            string[] sourceGuids = AssetDatabase.FindAssets("t:UpgradeData", new[] { "Assets/GameData/Upgrades" });
            int copiedCount = 0;
            int skippedCount = 0;
            
            foreach (string guid in sourceGuids)
            {
                string sourcePath = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = Path.GetFileName(sourcePath);
                string targetPath = $"Assets/Resources/Upgrades/{fileName}";
                
                // Check if already exists
                if (AssetDatabase.LoadAssetAtPath<UpgradeData>(targetPath) != null)
                {
                    skippedCount++;
                    continue;
                }
                
                // Copy asset
                if (AssetDatabase.CopyAsset(sourcePath, targetPath))
                {
                    copiedCount++;
                    Debug.Log($"✅ Copied: {fileName}");
                }
                else
                {
                    Debug.LogError($"❌ Failed to copy: {fileName}");
                }
            }
            
            AssetDatabase.Refresh();
            
            Debug.Log($"=== Copy Complete ===");
            Debug.Log($"Copied: {copiedCount} | Skipped: {skippedCount}");
            
            if (copiedCount > 0)
            {
                Debug.Log("✅ Upgrades are now in Resources and will be loaded automatically!");
            }
        }
    }
}