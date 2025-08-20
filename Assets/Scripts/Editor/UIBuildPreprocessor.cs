using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using GameDevClicker.Game.Views;

namespace GameDevClicker.Editor
{
    /// <summary>
    /// Build preprocessor that automatically fixes UI issues before building
    /// This ensures the build includes the properly fixed UI
    /// </summary>
    public class UIBuildPreprocessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            Debug.Log("[UIBuildPreprocessor] Starting pre-build UI fixes...");
            
            // Get the current active scene
            Scene activeScene = SceneManager.GetActiveScene();
            
            if (activeScene.isLoaded)
            {
                FixUIInCurrentScene();
                
                // Mark scene as dirty and save it
                EditorSceneManager.MarkSceneDirty(activeScene);
                EditorSceneManager.SaveScene(activeScene);
                
                Debug.Log($"[UIBuildPreprocessor] ✅ UI fixes applied and saved to scene: {activeScene.name}");
            }
        }

        private void FixUIInCurrentScene()
        {
            FixCanvasAssignment();
            FixScrollViewContent();
            FixParentAssignment();
            FixItemPositioning();
        }

        private void FixCanvasAssignment()
        {
            var gameViewUI = Object.FindObjectOfType<GameViewUI>();
            if (gameViewUI == null) return;

            var canvas = gameViewUI.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                var allCanvases = Object.FindObjectsOfType<Canvas>();
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
                    Debug.Log($"[UIBuildPreprocessor] ✅ Canvas assignment fixed");
                }
            }
        }

        private void FixScrollViewContent()
        {
            var scrollRects = Object.FindObjectsOfType<ScrollRect>();
            foreach (var scrollRect in scrollRects)
            {
                if (scrollRect.content != null)
                {
                    var layoutGroup = scrollRect.content.GetComponent<VerticalLayoutGroup>();
                    if (layoutGroup == null)
                    {
                        layoutGroup = scrollRect.content.gameObject.AddComponent<VerticalLayoutGroup>();
                        layoutGroup.childForceExpandWidth = true;
                        layoutGroup.childForceExpandHeight = false;
                        layoutGroup.childControlHeight = false;
                        layoutGroup.childControlWidth = true;
                        layoutGroup.spacing = 5f;
                        layoutGroup.padding = new RectOffset(10, 10, 10, 10);
                    }

                    var sizeFitter = scrollRect.content.GetComponent<ContentSizeFitter>();
                    if (sizeFitter == null)
                    {
                        sizeFitter = scrollRect.content.gameObject.AddComponent<ContentSizeFitter>();
                        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                    }

                    scrollRect.content.anchoredPosition = Vector2.zero;
                }
            }
            Debug.Log("[UIBuildPreprocessor] ✅ ScrollView content fixed");
        }

        private void FixParentAssignment()
        {
            var upgradeItems = Object.FindObjectsOfType<UpgradeItemUI>(true);
            var scrollRect = Object.FindObjectOfType<ScrollRect>();

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
                Debug.Log($"[UIBuildPreprocessor] ✅ Fixed parent assignment for {fixedCount} items");
            }
        }

        private void FixItemPositioning()
        {
            var upgradeItems = Object.FindObjectsOfType<UpgradeItemUI>(true);
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

            var scrollRect = Object.FindObjectOfType<ScrollRect>();
            if (scrollRect?.content != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
            }
            
            if (fixedCount > 0)
            {
                Debug.Log($"[UIBuildPreprocessor] ✅ Fixed positioning for {fixedCount} items");
            }
        }
    }

    /// <summary>
    /// Menu item to manually apply UI fixes and save the scene
    /// </summary>
    public class UIFixerMenu
    {
        [MenuItem("Game Tools/Fix UI and Save Scene")]
        public static void FixUIAndSaveScene()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("Cannot save scene changes while in Play Mode. Stop Play Mode first.");
                return;
            }

            Debug.Log("[UIFixerMenu] Applying UI fixes to scene...");
            
            var preprocessor = new UIBuildPreprocessor();
            preprocessor.OnPreprocessBuild(null); // null is fine for this usage
            
            Debug.Log("[UIFixerMenu] ✅ UI fixes applied and scene saved!");
            EditorUtility.DisplayDialog("UI Fixer", "UI fixes have been applied and the scene has been saved. These fixes will now persist in builds.", "OK");
        }
    }
}