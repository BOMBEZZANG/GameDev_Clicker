using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using GameDevClicker.Game.Views;

namespace GameDevClicker.Editor
{
    /// <summary>
    /// Fixes common UI positioning issues found in debug logs
    /// </summary>
    public class UIPositionFixer : EditorWindow
    {
        [MenuItem("Game Tools/UI Position Fixer")]
        public static void ShowWindow()
        {
            GetWindow<UIPositionFixer>("UI Position Fixer");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("UI Position Fixer", EditorStyles.boldLabel);
            GUILayout.Label("Fixes invisible UI positioning issues", EditorStyles.helpBox);
            GUILayout.Space(10);
            
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("This tool only works in Play Mode", MessageType.Warning);
                return;
            }
            
            if (GUILayout.Button("Fix All UI Issues", GUILayout.Height(40)))
            {
                FixAllUIIssues();
            }
            
            GUILayout.Space(10);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Fix Canvas Assignment"))
            {
                FixCanvasAssignment();
            }
            
            if (GUILayout.Button("Fix ScrollView Content"))
            {
                FixScrollViewContent();
            }
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(5);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Fix Item Positioning"))
            {
                FixItemPositioning();
            }
            
            if (GUILayout.Button("Fix Parent Assignment"))
            {
                FixParentAssignment();
            }
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("Diagnose UI Issues"))
            {
                DiagnoseUIIssues();
            }
        }
        
        private void DiagnoseUIIssues()
        {
            Debug.Log("=== DIAGNOSING UI ISSUES ===");
            
            var upgradeItems = FindObjectsOfType<UpgradeItemUI>(true);
            Debug.Log($"Found {upgradeItems.Length} UpgradeItemUI objects (including inactive)");
            
            int missingRectTransform = 0;
            int wrongParent = 0;
            int inactive = 0;
            
            foreach (var item in upgradeItems)
            {
                if (item == null) continue;
                
                if (!item.gameObject.activeSelf)
                    inactive++;
                
                var rectTransform = item.GetComponent<RectTransform>();
                if (rectTransform == null)
                {
                    missingRectTransform++;
                    Debug.LogWarning($"❌ {item.name} is missing RectTransform!");
                }
                
                if (item.transform.parent != null && item.transform.parent.name == "Viewport")
                {
                    wrongParent++;
                    Debug.LogWarning($"⚠️ {item.name} has wrong parent (Viewport instead of Content)");
                }
            }
            
            Debug.Log($"--- Diagnosis Summary ---");
            Debug.Log($"Total Items: {upgradeItems.Length}");
            Debug.Log($"Inactive Items: {inactive}");
            Debug.Log($"Missing RectTransform: {missingRectTransform}");
            Debug.Log($"Wrong Parent: {wrongParent}");
            Debug.Log("=== DIAGNOSIS COMPLETE ===");
        }
        
        private void FixAllUIIssues()
        {
            Debug.Log("=== FIXING ALL UI ISSUES ===");
            
            FixCanvasAssignment();
            FixScrollViewContent();
            FixParentAssignment();
            FixItemPositioning();
            
            Debug.Log("=== ALL FIXES COMPLETE ===");
            Debug.Log("🎯 Check your game screen - UI should now be visible!");
        }
        
        private void FixCanvasAssignment()
        {
            Debug.Log("--- Fixing Canvas Assignment ---");
            
            var gameViewUI = FindObjectOfType<GameViewUI>();
            if (gameViewUI == null)
            {
                Debug.LogError("❌ GameViewUI not found!");
                return;
            }
            
            // Check if GameViewUI is child of Canvas
            var canvas = gameViewUI.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                // Find main Canvas and move GameViewUI under it
                var allCanvases = FindObjectsOfType<Canvas>();
                Canvas targetCanvas = null;
                
                foreach (var c in allCanvases)
                {
                    if (c.renderMode == RenderMode.ScreenSpaceOverlay)
                    {
                        targetCanvas = c;
                        break;
                    }
                }
                
                if (targetCanvas != null)
                {
                    gameViewUI.transform.SetParent(targetCanvas.transform, false);
                    Debug.Log($"✅ Moved GameViewUI under Canvas: {targetCanvas.name}");
                }
                else
                {
                    Debug.LogWarning("❌ No suitable Canvas found!");
                }
            }
            else
            {
                Debug.Log($"✅ Canvas already assigned: {canvas.name}");
            }
        }
        
        private void FixScrollViewContent()
        {
            Debug.Log("--- Fixing ScrollView Content ---");
            
            var scrollRects = FindObjectsOfType<ScrollRect>();
            foreach (var scrollRect in scrollRects)
            {
                if (scrollRect.content != null)
                {
                    // Add VerticalLayoutGroup if missing
                    var layoutGroup = scrollRect.content.GetComponent<VerticalLayoutGroup>();
                    if (layoutGroup == null)
                    {
                        layoutGroup = scrollRect.content.gameObject.AddComponent<VerticalLayoutGroup>();
                        layoutGroup.childForceExpandWidth = true;
                        layoutGroup.childForceExpandHeight = false;
                        layoutGroup.childControlHeight = false;
                        layoutGroup.childControlWidth = true;
                        Debug.Log($"✅ Added VerticalLayoutGroup to {scrollRect.content.name}");
                    }
                    
                    // Add ContentSizeFitter if missing
                    var sizeFitter = scrollRect.content.GetComponent<ContentSizeFitter>();
                    if (sizeFitter == null)
                    {
                        sizeFitter = scrollRect.content.gameObject.AddComponent<ContentSizeFitter>();
                        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                        Debug.Log($"✅ Added ContentSizeFitter to {scrollRect.content.name}");
                    }
                    
                    // Reset content position
                    scrollRect.content.anchoredPosition = Vector2.zero;
                    Debug.Log($"✅ Reset position for {scrollRect.content.name}");
                }
            }
        }
        
        private void FixParentAssignment()
        {
            Debug.Log("--- Fixing Parent Assignment ---");
            
            // Find both active and inactive upgrade items
            var upgradeItems = FindObjectsOfType<UpgradeItemUI>(true); // Include inactive objects
            var scrollRect = FindObjectOfType<ScrollRect>();
            
            if (scrollRect?.content == null)
            {
                Debug.LogError("❌ No ScrollView Content found!");
                return;
            }
            
            foreach (var item in upgradeItems)
            {
                if (item == null || item.transform == null)
                {
                    Debug.LogWarning("⚠️ Found null UpgradeItemUI or transform, skipping...");
                    continue;
                }
                
                // Check if item is directly under Viewport (wrong parent)
                if (item.transform.parent != null && item.transform.parent.name == "Viewport")
                {
                    // Move to Content instead
                    item.transform.SetParent(scrollRect.content, false);
                    Debug.Log($"✅ Moved {item.name} from Viewport to Content");
                }
            }
        }
        
        private void FixItemPositioning()
        {
            Debug.Log("--- Fixing Item Positioning ---");
            
            // Find both active and inactive upgrade items
            var upgradeItems = FindObjectsOfType<UpgradeItemUI>(true); // Include inactive objects
            
            foreach (var item in upgradeItems)
            {
                if (item == null)
                {
                    Debug.LogWarning("⚠️ Found null UpgradeItemUI reference, skipping...");
                    continue;
                }
                
                var rectTransform = item.GetComponent<RectTransform>();
                
                // Check if RectTransform exists
                if (rectTransform == null)
                {
                    Debug.LogWarning($"⚠️ No RectTransform found on {item.name}, attempting to add one...");
                    rectTransform = item.gameObject.AddComponent<RectTransform>();
                    
                    if (rectTransform == null)
                    {
                        Debug.LogError($"❌ Failed to add RectTransform to {item.name}, skipping...");
                        continue;
                    }
                }
                
                // Fix anchors - should be top-left anchor for vertical list
                rectTransform.anchorMin = new Vector2(0, 1);
                rectTransform.anchorMax = new Vector2(1, 1);
                rectTransform.pivot = new Vector2(0.5f, 1f);
                
                // Reset position and size
                rectTransform.anchoredPosition = Vector2.zero;
                rectTransform.sizeDelta = new Vector2(0, 100); // Full width, 100px height
                
                Debug.Log($"✅ Fixed positioning for {item.name} (Active: {item.gameObject.activeSelf})");
            }
            
            // Force layout rebuild
            var scrollRect = FindObjectOfType<ScrollRect>();
            if (scrollRect?.content != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
                Debug.Log("✅ Forced layout rebuild");
            }
        }
        
        private void UpdateGameViewUIReference()
        {
            Debug.Log("--- Updating GameViewUI References ---");
            
            var gameViewUI = FindObjectOfType<GameViewUI>();
            var scrollRect = FindObjectOfType<ScrollRect>();
            
            if (gameViewUI != null && scrollRect?.content != null)
            {
                // Use reflection to set the upgradeListParent to the correct Content
                var field = typeof(GameViewUI).GetField("upgradeListParent", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (field != null)
                {
                    field.SetValue(gameViewUI, scrollRect.content);
                    Debug.Log("✅ Updated GameViewUI.upgradeListParent to ScrollView Content");
                }
            }
        }
    }
}