using UnityEditor;
using UnityEngine;
using GameDevClicker.Core.Managers;
using GameDevClicker.Data.ScriptableObjects;

namespace GameDevClicker.Editor
{
    /// <summary>
    /// Debug tool to check upgrade visibility and unlock conditions
    /// </summary>
    public class UpgradeDebugger : EditorWindow
    {
        private Vector2 scrollPosition;
        
        [MenuItem("Game Tools/Upgrade Debugger")]
        public static void ShowWindow()
        {
            GetWindow<UpgradeDebugger>("Upgrade Debugger");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("Upgrade Visibility Debugger", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("This tool only works in Play Mode", MessageType.Warning);
                return;
            }
            
            if (GUILayout.Button("Debug All Upgrades", GUILayout.Height(30)))
            {
                DebugAllUpgrades();
            }
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("Force Show All Upgrades (Cheat)", GUILayout.Height(25)))
            {
                ForceShowAllUpgrades();
            }
            
            EditorGUILayout.HelpBox("Use 'Force Show All' to test UI with all upgrades visible", MessageType.Info);
        }
        
        private void DebugAllUpgrades()
        {
            if (UpgradeManager.Instance == null)
            {
                Debug.LogError("[UpgradeDebugger] UpgradeManager not found!");
                return;
            }
            
            if (SaveManager.Instance?.CurrentGameData == null)
            {
                Debug.LogError("[UpgradeDebugger] No game data found!");
                return;
            }
            
            var gameData = SaveManager.Instance.CurrentGameData;
            Debug.Log("=== UPGRADE DEBUGGER ===");
            Debug.Log($"Player Level: {gameData.playerLevel}");
            Debug.Log($"Player Stage: {gameData.currentStage}");
            Debug.Log($"Player Experience: {gameData.experience}");
            Debug.Log($"Player Money: {gameData.money}");
            
            // Check each category
            DebugCategory(UpgradeData.UpgradeCategory.Skills);
            DebugCategory(UpgradeData.UpgradeCategory.Equipment);
            DebugCategory(UpgradeData.UpgradeCategory.Team);
            
            Debug.Log("=== DEBUG COMPLETE ===");
        }
        
        private void DebugCategory(UpgradeData.UpgradeCategory category)
        {
            Debug.Log($"\n--- {category} CATEGORY ---");
            
            var upgrades = UpgradeManager.Instance.GetUpgradesByCategory(category);
            Debug.Log($"Total {category} upgrades loaded: {upgrades.Count}");
            
            if (upgrades.Count == 0)
            {
                Debug.LogWarning($"No {category} upgrades found! Check if assets are in Resources/Upgrades/");
                return;
            }
            
            foreach (var upgrade in upgrades)
            {
                bool isUnlocked = UpgradeManager.Instance.IsUpgradeUnlocked(upgrade);
                int currentLevel = UpgradeManager.Instance.GetUpgradeLevel(upgrade.upgradeId);
                bool canAfford = UpgradeManager.Instance.CanAffordUpgrade(upgrade);
                
                string status = isUnlocked ? "✅ UNLOCKED" : "❌ LOCKED";
                Debug.Log($"{status} {upgrade.upgradeId}: {upgrade.upgradeName}");
                Debug.Log($"  └ Requires: Level {upgrade.requiredLevel}, Stage {upgrade.requiredStage} | Current Level: {currentLevel} | Can Afford: {canAfford}");
            }
        }
        
        private void ForceShowAllUpgrades()
        {
            if (SaveManager.Instance?.CurrentGameData == null)
            {
                Debug.LogError("[UpgradeDebugger] No game data to modify!");
                return;
            }
            
            // Temporarily boost player level and stage to unlock everything
            var gameData = SaveManager.Instance.CurrentGameData;
            int originalLevel = gameData.playerLevel;
            int originalStage = gameData.currentStage;
            
            gameData.playerLevel = 50;  // High level to unlock everything
            gameData.currentStage = 10; // High stage to unlock everything
            
            Debug.Log($"[UpgradeDebugger] 🎮 CHEAT: Temporarily boosted player to Level {gameData.playerLevel}, Stage {gameData.currentStage}");
            Debug.Log("[UpgradeDebugger] All upgrades should now be visible!");
            Debug.Log("[UpgradeDebugger] ⚠️ This is for testing only - restart game to reset");
            
            // Refresh the upgrade lists
            var gamePresenter = FindObjectOfType<GameDevClicker.Game.Presenters.GamePresenter>();
            if (gamePresenter != null)
            {
                gamePresenter.RefreshUI();
            }
        }
    }
}