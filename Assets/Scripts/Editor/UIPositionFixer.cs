using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
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
            
            // Add new section for permanent fixes
            EditorGUILayout.HelpBox("✅ PERMANENT FIXES (Works in Edit Mode)", MessageType.Info);
            
            if (GUILayout.Button("Fix UI and Save Scene (PERMANENT)", GUILayout.Height(40)))
            {
                if (Application.isPlaying)
                {
                    EditorGUILayout.HelpBox("Stop Play Mode first to save permanent changes", MessageType.Warning);
                }
                else
                {
                    FixUIAndSaveScene();
                }
            }
            
            GUILayout.Space(10);
            EditorGUILayout.HelpBox("⚠️ TEMPORARY FIXES (Play Mode Only)", MessageType.Warning);
            
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("These tools only work in Play Mode and don't save changes", MessageType.Warning);
                GUI.enabled = false;
            }
            
            if (GUILayout.Button("Fix All UI Issues (Temporary)", GUILayout.Height(40)))
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
            
            // Re-enable GUI
            GUI.enabled = true;
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
        
        private void FixUIAndSaveScene()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("Cannot save scene changes while in Play Mode. Stop Play Mode first.");
                return;
            }

            Debug.Log("[UIPositionFixer] Applying permanent UI fixes to scene...");
            
            FixAllUIIssuesInEditMode();
            
            // Mark scene as dirty and save it
            Scene activeScene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
            
            Debug.Log("[UIPositionFixer] ✅ UI fixes applied and scene saved!");
            EditorUtility.DisplayDialog("UI Position Fixer", "UI fixes have been applied and the scene has been saved. These fixes will now persist in builds.", "OK");
        }
        
        private void FixAllUIIssuesInEditMode()
        {
            Debug.Log("=== FIXING ALL UI ISSUES (EDIT MODE) ===");
            
            FixCanvasAssignmentInEditMode();
            FixScrollViewContentInEditMode();
            FixParentAssignmentInEditMode();
            FixItemPositioningInEditMode();
            
            Debug.Log("=== ALL EDIT MODE FIXES COMPLETE ===");
        }
        
        private void FixCanvasAssignmentInEditMode()
        {
            var gameViewUI = FindObjectOfType<GameViewUI>();
            if (gameViewUI == null) return;

            var canvas = gameViewUI.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
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
                    Debug.Log($"✅ Canvas assignment fixed in edit mode");
                }
            }
        }

        private void FixScrollViewContentInEditMode()
        {
            var scrollRects = FindObjectsOfType<ScrollRect>();
            foreach (var scrollRect in scrollRects)
            {
                if (scrollRect.content != null)
                {
                    // Fix content RectTransform anchoring
                    var contentRect = scrollRect.content;
                    contentRect.anchorMin = new Vector2(0, 1);
                    contentRect.anchorMax = new Vector2(1, 1);
                    contentRect.pivot = new Vector2(0.5f, 1);
                    contentRect.anchoredPosition = Vector2.zero;
                    
                    var layoutGroup = contentRect.GetComponent<VerticalLayoutGroup>();
                    if (layoutGroup == null)
                    {
                        layoutGroup = contentRect.gameObject.AddComponent<VerticalLayoutGroup>();
                    }
                    // Configure layout group
                    layoutGroup.childForceExpandWidth = true;
                    layoutGroup.childForceExpandHeight = false;
                    layoutGroup.childControlHeight = false;
                    layoutGroup.childControlWidth = true;
                    layoutGroup.spacing = 5f;
                    layoutGroup.padding = new RectOffset(10, 10, 10, 10);

                    var sizeFitter = contentRect.GetComponent<ContentSizeFitter>();
                    if (sizeFitter == null)
                    {
                        sizeFitter = contentRect.gameObject.AddComponent<ContentSizeFitter>();
                    }
                    // Configure size fitter
                    sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                    sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

                    // Ensure ScrollRect is properly configured
                    scrollRect.horizontal = false;
                    scrollRect.vertical = true;
                    scrollRect.movementType = ScrollRect.MovementType.Elastic;
                    scrollRect.elasticity = 0.1f;
                    scrollRect.inertia = true;
                    scrollRect.decelerationRate = 0.135f;
                    scrollRect.scrollSensitivity = 15f;
                    
                    // Fix vertical scrollbar if it exists
                    if (scrollRect.verticalScrollbar != null)
                    {
                        scrollRect.verticalScrollbar.direction = Scrollbar.Direction.TopToBottom;
                        scrollRect.verticalScrollbar.value = 1f;
                        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
                        scrollRect.verticalScrollbarSpacing = -3f;
                        
                        // Ensure scrollbar is active
                        scrollRect.verticalScrollbar.gameObject.SetActive(true);
                        
                        // Mark as dirty for saving
                        EditorUtility.SetDirty(scrollRect.verticalScrollbar);
                    }
                    
                    // Mark components as dirty for saving
                    EditorUtility.SetDirty(scrollRect);
                    EditorUtility.SetDirty(contentRect);
                    if (layoutGroup != null) EditorUtility.SetDirty(layoutGroup);
                    if (sizeFitter != null) EditorUtility.SetDirty(sizeFitter);
                }
            }
            Debug.Log("✅ ScrollView content fixed in edit mode");
        }

        private void FixParentAssignmentInEditMode()
        {
            var upgradeItems = FindObjectsOfType<UpgradeItemUI>(true);
            var scrollRect = FindObjectOfType<ScrollRect>();

            if (scrollRect?.content == null) return;

            int fixedCount = 0;
            foreach (var item in upgradeItems)
            {
                if (item == null || item.transform == null) continue;

                if (item.transform.parent != null && item.transform.parent.name == "Viewport")
                {
                    item.transform.SetParent(scrollRect.content, false);
                    fixedCount++;
                }
            }
            
            if (fixedCount > 0)
            {
                Debug.Log($"✅ Fixed parent assignment for {fixedCount} items in edit mode");
            }
        }

        private void FixItemPositioningInEditMode()
        {
            var upgradeItems = FindObjectsOfType<UpgradeItemUI>(true);
            int fixedCount = 0;

            foreach (var item in upgradeItems)
            {
                if (item == null) continue;

                var rectTransform = item.GetComponent<RectTransform>();
                if (rectTransform == null)
                {
                    rectTransform = item.gameObject.AddComponent<RectTransform>();
                }

                if (rectTransform != null)
                {
                    rectTransform.anchorMin = new Vector2(0, 1);
                    rectTransform.anchorMax = new Vector2(1, 1);
                    rectTransform.pivot = new Vector2(0.5f, 1f);
                    rectTransform.anchoredPosition = Vector2.zero;
                    rectTransform.sizeDelta = new Vector2(0, 100);
                    fixedCount++;
                }
            }

            var scrollRect = FindObjectOfType<ScrollRect>();
            if (scrollRect?.content != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
            }
            
            if (fixedCount > 0)
            {
                Debug.Log($"✅ Fixed positioning for {fixedCount} items in edit mode");
            }
        }
    }
}