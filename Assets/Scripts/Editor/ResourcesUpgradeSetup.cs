using UnityEditor;
using UnityEngine;
using System.IO;

namespace GameDevClicker.Editor
{
    /// <summary>
    /// Tool to set up upgrades in Resources folder for automatic loading
    /// </summary>
    public class ResourcesUpgradeSetup : EditorWindow
    {
        [MenuItem("Game Tools/Setup Upgrades in Resources")]
        public static void ShowWindow()
        {
            var window = GetWindow<ResourcesUpgradeSetup>("Resources Setup");
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Label("Resources Upgrade Setup", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            GUILayout.Label("This tool will copy/move your generated upgrade assets\nto the Resources/Upgrades folder for automatic loading.", EditorStyles.helpBox);
            GUILayout.Space(10);

            EditorGUILayout.LabelField("Source:", "Assets/GameData/Upgrades/");
            EditorGUILayout.LabelField("Target:", "Assets/Resources/Upgrades/");
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("Copy Upgrades to Resources", GUILayout.Height(30)))
            {
                CopyUpgradesToResources();
            }
            
            GUILayout.Space(5);
            
            if (GUILayout.Button("Create Symbolic Links (Advanced)", GUILayout.Height(25)))
            {
                CreateSymbolicLinks();
            }
            
            GUILayout.Space(10);
            
            GUILayout.Label("Alternative: Manual Assignment", EditorStyles.boldLabel);
            if (GUILayout.Button("Open UpgradeManager for Manual Assignment"))
            {
                OpenUpgradeManagerForInspection();
            }
        }

        private void CopyUpgradesToResources()
        {
            string sourcePath = "Assets/GameData/Upgrades";
            string targetPath = "Assets/Resources/Upgrades";
            
            // Ensure target directory exists
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }
            if (!AssetDatabase.IsValidFolder(targetPath))
            {
                AssetDatabase.CreateFolder("Assets/Resources", "Upgrades");
            }
            
            try
            {
                // Copy all upgrade assets
                string[] upgradeAssets = AssetDatabase.FindAssets("t:UpgradeData", new[] { sourcePath });
                int copiedCount = 0;
                
                foreach (string guid in upgradeAssets)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    string fileName = Path.GetFileName(assetPath);
                    string targetAssetPath = Path.Combine(targetPath, fileName).Replace('\\', '/');
                    
                    if (AssetDatabase.CopyAsset(assetPath, targetAssetPath))
                    {
                        copiedCount++;
                    }
                }
                
                AssetDatabase.Refresh();
                
                EditorUtility.DisplayDialog("Success", 
                    $"Copied {copiedCount} upgrade assets to Resources folder!\n\n" +
                    "Your UpgradeManager will now automatically load these upgrades.", 
                    "OK");
                    
                Debug.Log($"[ResourcesUpgradeSetup] Copied {copiedCount} upgrade assets to Resources");
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("Error", $"Failed to copy upgrades: {e.Message}", "OK");
                Debug.LogError($"[ResourcesUpgradeSetup] Error: {e.Message}");
            }
        }
        
        private void CreateSymbolicLinks()
        {
            EditorUtility.DisplayDialog("Info", 
                "Symbolic links require advanced setup and may not work on all systems.\n\n" +
                "Use the 'Copy to Resources' option for reliable functionality.", 
                "OK");
        }
        
        private void OpenUpgradeManagerForInspection()
        {
            // Find UpgradeManager in the scene
            var upgradeManager = FindObjectOfType<GameDevClicker.Core.Managers.UpgradeManager>();
            if (upgradeManager != null)
            {
                Selection.activeObject = upgradeManager.gameObject;
                EditorUtility.FocusProjectWindow();
                EditorUtility.DisplayDialog("Info", 
                    "UpgradeManager selected in Inspector.\n\n" +
                    "You can manually assign upgrades to the 'All Upgrades' array if needed.", 
                    "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Warning", 
                    "UpgradeManager not found in current scene.\n\n" +
                    "Make sure you have a GameManager or UpgradeManager in your scene.", 
                    "OK");
            }
        }
    }
}