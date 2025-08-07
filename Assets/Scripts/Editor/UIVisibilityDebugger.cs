using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GameDevClicker.Game.Views;

namespace GameDevClicker.Editor
{
    /// <summary>
    /// Debug tool to identify why UI elements are not visible on screen
    /// </summary>
    public class UIVisibilityDebugger : EditorWindow
    {
        private Vector2 scrollPosition;
        
        [MenuItem("Game Tools/UI Visibility Debugger")]
        public static void ShowWindow()
        {
            GetWindow<UIVisibilityDebugger>("UI Visibility Debugger");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("UI Visibility Debugger", EditorStyles.boldLabel);
            GUILayout.Label("Diagnose invisible UI elements", EditorStyles.helpBox);
            GUILayout.Space(10);
            
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("This tool only works in Play Mode", MessageType.Warning);
                return;
            }
            
            if (GUILayout.Button("Debug Upgrade UI Visibility", GUILayout.Height(40)))
            {
                DebugUpgradeUIVisibility();
            }
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("Debug All UI Canvas Elements", GUILayout.Height(30)))
            {
                DebugAllCanvasElements();
            }
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("Fix Common UI Issues (Attempt)", GUILayout.Height(30)))
            {
                FixCommonUIIssues();
            }
            
            GUILayout.Space(10);
            
            EditorGUILayout.HelpBox("Check Console for detailed visibility analysis", MessageType.Info);
        }
        
        private void DebugUpgradeUIVisibility()
        {
            Debug.Log("=== UI VISIBILITY DEBUG ===");
            
            var gameViewUI = FindObjectOfType<GameViewUI>();
            if (gameViewUI == null)
            {
                Debug.LogError("❌ GameViewUI not found in scene!");
                return;
            }
            
            Debug.Log("✅ GameViewUI found");
            
            // Check Canvas setup
            var canvas = gameViewUI.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                Debug.Log($"✅ Canvas found: {canvas.name}");
                Debug.Log($"   Render Mode: {canvas.renderMode}");
                Debug.Log($"   Sort Order: {canvas.sortingOrder}");
                Debug.Log($"   Enabled: {canvas.enabled}");
                
                var canvasScaler = canvas.GetComponent<CanvasScaler>();
                if (canvasScaler != null)
                {
                    Debug.Log($"   Canvas Scaler: {canvasScaler.uiScaleMode} | Reference: {canvasScaler.referenceResolution}");
                }
            }
            else
            {
                Debug.LogError("❌ No Canvas found for GameViewUI!");
            }
            
            // Find upgrade items in hierarchy
            var upgradeItems = FindObjectsOfType<UpgradeItemUI>();
            Debug.Log($"Found {upgradeItems.Length} UpgradeItemUI components in scene");
            
            if (upgradeItems.Length == 0)
            {
                Debug.LogWarning("❌ No UpgradeItemUI components found! Check if prefabs are being instantiated.");
                return;
            }
            
            // Debug each upgrade item
            for (int i = 0; i < upgradeItems.Length; i++)
            {
                var upgradeItem = upgradeItems[i];
                Debug.Log($"\n--- Upgrade Item #{i + 1}: {upgradeItem.name} ---");
                
                DebugGameObjectVisibility(upgradeItem.gameObject);
                DebugRectTransform(upgradeItem.GetComponent<RectTransform>());
                DebugUIComponents(upgradeItem.gameObject);
            }
            
            // Check ScrollView setup
            DebugScrollViewSetup();
            
            Debug.Log("=== DEBUG COMPLETE ===");
        }
        
        private void DebugGameObjectVisibility(GameObject obj)
        {
            Debug.Log($"GameObject: {obj.name}");
            Debug.Log($"  Active in Hierarchy: {obj.activeInHierarchy}");
            Debug.Log($"  Active Self: {obj.activeSelf}");
            Debug.Log($"  Layer: {LayerMask.LayerToName(obj.layer)} ({obj.layer})");
            
            // Check parent chain
            Transform parent = obj.transform.parent;
            int depth = 0;
            while (parent != null && depth < 10)
            {
                Debug.Log($"  Parent {depth}: {parent.name} (Active: {parent.gameObject.activeSelf})");
                parent = parent.parent;
                depth++;
            }
        }
        
        private void DebugRectTransform(RectTransform rectTransform)
        {
            if (rectTransform == null)
            {
                Debug.LogError("  ❌ No RectTransform found!");
                return;
            }
            
            Debug.Log($"  RectTransform:");
            Debug.Log($"    Position: {rectTransform.localPosition}");
            Debug.Log($"    Size: {rectTransform.sizeDelta}");
            Debug.Log($"    Anchors: Min{rectTransform.anchorMin} Max{rectTransform.anchorMax}");
            Debug.Log($"    Pivot: {rectTransform.pivot}");
            Debug.Log($"    Scale: {rectTransform.localScale}");
            
            // Check if it's within screen bounds
            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);
            
            Debug.Log($"    World Corners:");
            for (int i = 0; i < corners.Length; i++)
            {
                Debug.Log($"      [{i}] {corners[i]}");
            }
            
            // Check if any corner is visible
            bool isVisible = false;
            Camera cam = Camera.main;
            if (cam != null)
            {
                foreach (var corner in corners)
                {
                    Vector3 screenPoint = cam.WorldToScreenPoint(corner);
                    if (screenPoint.x >= 0 && screenPoint.x <= Screen.width &&
                        screenPoint.y >= 0 && screenPoint.y <= Screen.height &&
                        screenPoint.z > 0)
                    {
                        isVisible = true;
                        break;
                    }
                }
            }
            
            Debug.Log($"    Potentially Visible on Screen: {(isVisible ? "✅ YES" : "❌ NO")}");
        }
        
        private void DebugUIComponents(GameObject obj)
        {
            var image = obj.GetComponent<Image>();
            if (image != null)
            {
                Debug.Log($"  Image Component:");
                Debug.Log($"    Enabled: {image.enabled}");
                Debug.Log($"    Color: {image.color}");
                Debug.Log($"    Alpha: {image.color.a}");
                Debug.Log($"    Raycast Target: {image.raycastTarget}");
                Debug.Log($"    Sprite: {(image.sprite != null ? image.sprite.name : "null")}");
            }
            
            var texts = obj.GetComponentsInChildren<TextMeshProUGUI>();
            foreach (var text in texts)
            {
                Debug.Log($"  Text Component ({text.name}):");
                Debug.Log($"    Enabled: {text.enabled}");
                Debug.Log($"    Text: '{text.text}'");
                Debug.Log($"    Color: {text.color}");
                Debug.Log($"    Font Size: {text.fontSize}");
                Debug.Log($"    Alpha: {text.color.a}");
            }
            
            var button = obj.GetComponent<Button>();
            if (button != null)
            {
                Debug.Log($"  Button Component:");
                Debug.Log($"    Enabled: {button.enabled}");
                Debug.Log($"    Interactable: {button.interactable}");
                Debug.Log($"    Colors: Normal={button.colors.normalColor}");
            }
        }
        
        private void DebugScrollViewSetup()
        {
            Debug.Log("\n--- ScrollView Setup Debug ---");
            
            var scrollRects = FindObjectsOfType<ScrollRect>();
            Debug.Log($"Found {scrollRects.Length} ScrollRect components");
            
            foreach (var scrollRect in scrollRects)
            {
                Debug.Log($"ScrollRect: {scrollRect.name}");
                Debug.Log($"  Enabled: {scrollRect.enabled}");
                Debug.Log($"  Viewport: {(scrollRect.viewport != null ? scrollRect.viewport.name : "null")}");
                Debug.Log($"  Content: {(scrollRect.content != null ? scrollRect.content.name : "null")}");
                
                if (scrollRect.content != null)
                {
                    Debug.Log($"  Content Size: {scrollRect.content.sizeDelta}");
                    Debug.Log($"  Content Position: {scrollRect.content.localPosition}");
                    Debug.Log($"  Content Children: {scrollRect.content.childCount}");
                    
                    // Check content layout
                    var layoutGroup = scrollRect.content.GetComponent<LayoutGroup>();
                    if (layoutGroup != null)
                    {
                        Debug.Log($"  Layout Group: {layoutGroup.GetType().Name} (Enabled: {layoutGroup.enabled})");
                    }
                    else
                    {
                        Debug.LogWarning($"  ⚠️ No Layout Group on Content - items may not position correctly!");
                    }
                }
            }
        }
        
        private void DebugAllCanvasElements()
        {
            Debug.Log("=== ALL CANVAS ELEMENTS DEBUG ===");
            
            var canvases = FindObjectsOfType<Canvas>();
            foreach (var canvas in canvases)
            {
                Debug.Log($"\nCanvas: {canvas.name}");
                Debug.Log($"  Render Mode: {canvas.renderMode}");
                Debug.Log($"  Enabled: {canvas.enabled}");
                Debug.Log($"  Sort Order: {canvas.sortingOrder}");
                
                // Check all immediate children
                for (int i = 0; i < canvas.transform.childCount; i++)
                {
                    var child = canvas.transform.GetChild(i);
                    Debug.Log($"    Child {i}: {child.name} (Active: {child.gameObject.activeSelf})");
                }
            }
        }
        
        private void FixCommonUIIssues()
        {
            Debug.Log("=== ATTEMPTING TO FIX COMMON UI ISSUES ===");
            
            int fixesApplied = 0;
            
            // Fix 1: Ensure ScrollView Content has Layout Group
            var scrollRects = FindObjectsOfType<ScrollRect>();
            foreach (var scrollRect in scrollRects)
            {
                if (scrollRect.content != null)
                {
                    var layoutGroup = scrollRect.content.GetComponent<LayoutGroup>();
                    if (layoutGroup == null)
                    {
                        scrollRect.content.gameObject.AddComponent<VerticalLayoutGroup>();
                        Debug.Log($"✅ Added VerticalLayoutGroup to {scrollRect.content.name}");
                        fixesApplied++;
                    }
                    
                    // Enable Layout Group if disabled
                    if (layoutGroup != null && !layoutGroup.enabled)
                    {
                        layoutGroup.enabled = true;
                        Debug.Log($"✅ Enabled LayoutGroup on {scrollRect.content.name}");
                        fixesApplied++;
                    }
                }
            }
            
            // Fix 2: Set ContentSizeFitter on ScrollView Content
            foreach (var scrollRect in scrollRects)
            {
                if (scrollRect.content != null)
                {
                    var sizeFitter = scrollRect.content.GetComponent<ContentSizeFitter>();
                    if (sizeFitter == null)
                    {
                        sizeFitter = scrollRect.content.gameObject.AddComponent<ContentSizeFitter>();
                        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                        Debug.Log($"✅ Added ContentSizeFitter to {scrollRect.content.name}");
                        fixesApplied++;
                    }
                }
            }
            
            // Fix 3: Check for zero-sized RectTransforms
            var upgradeItems = FindObjectsOfType<UpgradeItemUI>();
            foreach (var item in upgradeItems)
            {
                var rectTransform = item.GetComponent<RectTransform>();
                if (rectTransform.sizeDelta.x == 0 || rectTransform.sizeDelta.y == 0)
                {
                    rectTransform.sizeDelta = new Vector2(300, 80); // Default size
                    Debug.Log($"✅ Fixed zero size on {item.name}");
                    fixesApplied++;
                }
                
                // Fix transparency
                var image = item.GetComponent<Image>();
                if (image != null && image.color.a < 0.1f)
                {
                    var color = image.color;
                    color.a = 0.8f;
                    image.color = color;
                    Debug.Log($"✅ Fixed transparency on {item.name}");
                    fixesApplied++;
                }
            }
            
            Debug.Log($"=== FIXES COMPLETE: {fixesApplied} issues resolved ===");
            
            if (fixesApplied > 0)
            {
                Debug.Log("🔄 UI should now be more visible. Check your game screen!");
            }
            else
            {
                Debug.Log("ℹ️ No common issues found. Check the detailed debug output above.");
            }
        }
    }
}