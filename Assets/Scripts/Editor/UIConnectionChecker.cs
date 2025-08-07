using UnityEditor;
using UnityEngine;
using GameDevClicker.Game.Views;
using GameDevClicker.Game.Presenters;
using GameDevClicker.Core.Managers;

namespace GameDevClicker.Editor
{
    /// <summary>
    /// Tool to check and validate UI connections for upgrade system
    /// </summary>
    public class UIConnectionChecker : EditorWindow
    {
        private Vector2 scrollPosition;
        private string checkResults = "";
        
        [MenuItem("Game Tools/UI Connection Checker")]
        public static void ShowWindow()
        {
            GetWindow<UIConnectionChecker>("UI Connection Checker");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("UI Connection Checker", EditorStyles.boldLabel);
            GUILayout.Label("Diagnose upgrade UI connection issues", EditorStyles.helpBox);
            GUILayout.Space(10);
            
            if (GUILayout.Button("Check All UI Connections", GUILayout.Height(30)))
            {
                CheckAllConnections();
            }
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("Auto-Fix Missing References (if possible)", GUILayout.Height(25)))
            {
                AutoFixReferences();
            }
            
            GUILayout.Space(10);
            
            GUILayout.Label("Diagnostic Results:", EditorStyles.boldLabel);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(300));
            EditorGUILayout.TextArea(checkResults, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
            
            if (GUILayout.Button("Clear Results"))
            {
                checkResults = "";
            }
        }
        
        private void CheckAllConnections()
        {
            checkResults = "=== UI Connection Diagnostic ===\n\n";
            
            // Check GamePresenter
            var gamePresenter = FindObjectOfType<GamePresenter>();
            if (gamePresenter == null)
            {
                checkResults += "❌ CRITICAL: GamePresenter not found in scene!\n";
                checkResults += "   → Add GamePresenter to your scene\n\n";
                return;
            }
            else
            {
                checkResults += "✅ GamePresenter found\n";
            }
            
            // Check GameViewUI
            var gameViewUI = FindObjectOfType<GameViewUI>();
            if (gameViewUI == null)
            {
                checkResults += "❌ CRITICAL: GameViewUI not found in scene!\n";
                checkResults += "   → Add GameViewUI component to your UI Canvas\n\n";
                return;
            }
            else
            {
                checkResults += "✅ GameViewUI found\n";
                CheckGameViewUIReferences(gameViewUI);
            }
            
            // Check UpgradeManager
            var upgradeManager = FindObjectOfType<UpgradeManager>();
            if (upgradeManager == null)
            {
                checkResults += "❌ CRITICAL: UpgradeManager not found in scene!\n";
                checkResults += "   → UpgradeManager should be created as singleton\n\n";
            }
            else
            {
                checkResults += "✅ UpgradeManager found\n";
                CheckUpgradeManagerData(upgradeManager);
            }
            
            // Check prefab references
            CheckPrefabReferences();
            
            checkResults += "\n=== Diagnostic Complete ===\n";
        }
        
        private void CheckGameViewUIReferences(GameViewUI gameViewUI)
        {
            checkResults += "\n--- GameViewUI References ---\n";
            
            // Use reflection to check private fields
            var fields = typeof(GameViewUI).GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            foreach (var field in fields)
            {
                if (field.GetCustomAttributes(typeof(SerializeField), false).Length > 0)
                {
                    var value = field.GetValue(gameViewUI);
                    string fieldName = field.Name;
                    
                    // Check critical upgrade system fields
                    if (fieldName.Contains("upgrade"))
                    {
                        if (value == null)
                        {
                            checkResults += $"❌ {fieldName}: NOT ASSIGNED\n";
                            if (fieldName == "upgradeItemPrefab")
                                checkResults += "   → Create a prefab with UpgradeItemUI component\n";
                            if (fieldName == "upgradeListParent")
                                checkResults += "   → Assign the parent transform for upgrade list\n";
                        }
                        else
                        {
                            checkResults += $"✅ {fieldName}: Assigned\n";
                        }
                    }
                }
            }
        }
        
        private void CheckUpgradeManagerData(UpgradeManager upgradeManager)
        {
            checkResults += "\n--- UpgradeManager Data ---\n";
            
            // Check if upgrades are loaded
            try
            {
                var skillsUpgrades = upgradeManager.GetUpgradesByCategory(GameDevClicker.Data.ScriptableObjects.UpgradeData.UpgradeCategory.Skills);
                var equipmentUpgrades = upgradeManager.GetUpgradesByCategory(GameDevClicker.Data.ScriptableObjects.UpgradeData.UpgradeCategory.Equipment);
                var teamUpgrades = upgradeManager.GetUpgradesByCategory(GameDevClicker.Data.ScriptableObjects.UpgradeData.UpgradeCategory.Team);
                
                checkResults += $"✅ Skills Upgrades: {skillsUpgrades.Count}\n";
                checkResults += $"✅ Equipment Upgrades: {equipmentUpgrades.Count}\n";
                checkResults += $"✅ Team Upgrades: {teamUpgrades.Count}\n";
                
                if (skillsUpgrades.Count == 0 && equipmentUpgrades.Count == 0 && teamUpgrades.Count == 0)
                {
                    checkResults += "❌ NO UPGRADES LOADED!\n";
                    checkResults += "   → Check Resources/Upgrades folder for .asset files\n";
                    checkResults += "   → Use 'Game Tools → Setup Upgrades in Resources'\n";
                }
            }
            catch (System.Exception e)
            {
                checkResults += $"❌ Error checking upgrade data: {e.Message}\n";
            }
        }
        
        private void CheckPrefabReferences()
        {
            checkResults += "\n--- Prefab References ---\n";
            
            // Look for UpgradeItemUI prefabs
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab UpgradeItem");
            if (prefabGuids.Length == 0)
            {
                checkResults += "❌ No UpgradeItemUI prefabs found\n";
                checkResults += "   → Create a prefab with UpgradeItemUI component\n";
            }
            else
            {
                foreach (string guid in prefabGuids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    var upgradeItemUI = prefab.GetComponent<UpgradeItemUI>();
                    
                    if (upgradeItemUI != null)
                    {
                        checkResults += $"✅ Found UpgradeItemUI prefab: {prefab.name}\n";
                    }
                }
            }
        }
        
        private void AutoFixReferences()
        {
            checkResults = "=== Auto-Fix Attempt ===\n\n";
            
            var gameViewUI = FindObjectOfType<GameViewUI>();
            if (gameViewUI == null)
            {
                checkResults += "❌ Cannot auto-fix: GameViewUI not found\n";
                return;
            }
            
            // Try to find upgrade item prefab automatically
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab UpgradeItem");
            GameObject upgradeItemPrefab = null;
            
            foreach (string guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab.GetComponent<UpgradeItemUI>() != null)
                {
                    upgradeItemPrefab = prefab;
                    break;
                }
            }
            
            if (upgradeItemPrefab != null)
            {
                checkResults += $"✅ Found UpgradeItemUI prefab: {upgradeItemPrefab.name}\n";
                checkResults += "   → Manually assign this to GameViewUI's Upgrade Item Prefab field\n";
            }
            else
            {
                checkResults += "❌ No UpgradeItemUI prefab found\n";
                checkResults += "   → You need to create one manually\n";
            }
            
            // Try to find upgrade list parent
            Transform[] allTransforms = gameViewUI.GetComponentsInChildren<Transform>();
            Transform upgradeListParent = null;
            
            foreach (Transform t in allTransforms)
            {
                if (t.name.ToLower().Contains("upgrade") && t.name.ToLower().Contains("list"))
                {
                    upgradeListParent = t;
                    break;
                }
            }
            
            if (upgradeListParent != null)
            {
                checkResults += $"✅ Found potential upgrade list parent: {upgradeListParent.name}\n";
                checkResults += "   → Manually assign this to GameViewUI's Upgrade List Parent field\n";
            }
            
            checkResults += "\n--- Manual Steps Required ---\n";
            checkResults += "1. Select GameViewUI in scene\n";
            checkResults += "2. In Inspector, assign the Upgrade Item Prefab field\n";
            checkResults += "3. In Inspector, assign the Upgrade List Parent field\n";
            checkResults += "4. Make sure upgrade tab buttons are assigned\n";
            checkResults += "5. Run the game to test\n";
        }
    }
}