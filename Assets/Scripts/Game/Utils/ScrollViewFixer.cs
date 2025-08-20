using UnityEngine;
using UnityEngine.UI;
using GameDevClicker.Game.Views;
using System.Collections;

namespace GameDevClicker.Game.Utils
{
    /// <summary>
    /// Dedicated component to fix and maintain ScrollView functionality
    /// </summary>
    [RequireComponent(typeof(ScrollRect))]
    public class ScrollViewFixer : MonoBehaviour
    {
        private ScrollRect scrollRect;
        private RectTransform content;
        private VerticalLayoutGroup layoutGroup;
        private ContentSizeFitter sizeFitter;
        private Scrollbar verticalScrollbar;
        
        [Header("Settings")]
        [SerializeField] private bool autoFixOnStart = true;
        [SerializeField] private bool continuousCheck = true;
        [SerializeField] private float checkInterval = 0.5f;
        
        private float lastCheckTime;
        private int lastChildCount;

        void Awake()
        {
            scrollRect = GetComponent<ScrollRect>();
            if (scrollRect != null)
            {
                content = scrollRect.content;
                verticalScrollbar = scrollRect.verticalScrollbar;
            }
        }

        void Start()
        {
            if (autoFixOnStart)
            {
                StartCoroutine(DelayedFix());
            }
        }

        IEnumerator DelayedFix()
        {
            // Wait a frame to ensure everything is initialized
            yield return null;
            FixScrollView();
        }

        void Update()
        {
            if (continuousCheck && Time.time - lastCheckTime > checkInterval)
            {
                lastCheckTime = Time.time;
                CheckAndFixScrollView();
            }
        }

        void CheckAndFixScrollView()
        {
            if (content == null) return;
            
            // Check if child count changed
            int currentChildCount = content.childCount;
            if (currentChildCount != lastChildCount)
            {
                lastChildCount = currentChildCount;
                RefreshScrollView();
            }
            
            // Ensure scrollbar stays active when there's content
            if (verticalScrollbar != null && currentChildCount > 0)
            {
                // Calculate if content is scrollable
                float contentHeight = GetContentHeight();
                float viewportHeight = scrollRect.viewport != null ? 
                    scrollRect.viewport.rect.height : 
                    scrollRect.GetComponent<RectTransform>().rect.height;
                
                bool shouldBeScrollable = contentHeight > viewportHeight;
                
                if (shouldBeScrollable && !verticalScrollbar.gameObject.activeSelf)
                {
                    verticalScrollbar.gameObject.SetActive(true);
                    Debug.Log($"[ScrollViewFixer] Activated scrollbar - Content: {contentHeight:F1}, Viewport: {viewportHeight:F1}");
                }
            }
        }

        float GetContentHeight()
        {
            if (content == null) return 0;
            
            float totalHeight = 0;
            
            // Calculate total height including spacing
            if (layoutGroup != null)
            {
                totalHeight = layoutGroup.padding.top + layoutGroup.padding.bottom;
                
                for (int i = 0; i < content.childCount; i++)
                {
                    var child = content.GetChild(i);
                    if (child.gameObject.activeSelf)
                    {
                        var childRect = child.GetComponent<RectTransform>();
                        if (childRect != null)
                        {
                            totalHeight += childRect.sizeDelta.y;
                            if (i < content.childCount - 1)
                            {
                                totalHeight += layoutGroup.spacing;
                            }
                        }
                    }
                }
            }
            else
            {
                // Fallback if no layout group
                totalHeight = content.sizeDelta.y;
            }
            
            return totalHeight;
        }

        public void FixScrollView()
        {
            Debug.Log("[ScrollViewFixer] Fixing ScrollView configuration");
            
            if (scrollRect == null) return;
            
            // Get or create content
            if (content == null)
            {
                content = scrollRect.content;
                if (content == null)
                {
                    Debug.LogError("[ScrollViewFixer] No content found in ScrollRect!");
                    return;
                }
            }
            
            // Configure content RectTransform
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.anchoredPosition = new Vector2(0, 0);
            
            // Setup VerticalLayoutGroup
            layoutGroup = content.GetComponent<VerticalLayoutGroup>();
            if (layoutGroup == null)
            {
                layoutGroup = content.gameObject.AddComponent<VerticalLayoutGroup>();
            }
            
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.childControlHeight = false;
            layoutGroup.childControlWidth = true;
            layoutGroup.childScaleHeight = false;
            layoutGroup.childScaleWidth = false;
            layoutGroup.spacing = 5f;
            layoutGroup.padding = new RectOffset(10, 10, 10, 10);
            
            // Setup ContentSizeFitter
            sizeFitter = content.GetComponent<ContentSizeFitter>();
            if (sizeFitter == null)
            {
                sizeFitter = content.gameObject.AddComponent<ContentSizeFitter>();
            }
            
            sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            // Configure ScrollRect
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            scrollRect.elasticity = 0.1f;
            scrollRect.inertia = true;
            scrollRect.decelerationRate = 0.135f;
            scrollRect.scrollSensitivity = 15f;
            
            // Setup Viewport if missing
            if (scrollRect.viewport == null)
            {
                var viewport = transform.Find("Viewport");
                if (viewport != null)
                {
                    scrollRect.viewport = viewport.GetComponent<RectTransform>();
                }
            }
            
            // Configure Scrollbar
            if (verticalScrollbar != null)
            {
                verticalScrollbar.direction = Scrollbar.Direction.TopToBottom;
                verticalScrollbar.size = 0.2f;
                verticalScrollbar.value = 1f;
                scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
                scrollRect.verticalScrollbarSpacing = -3f;
                
                // Ensure scrollbar is active
                verticalScrollbar.gameObject.SetActive(true);
                
                // Setup scrollbar rect
                var scrollbarRect = verticalScrollbar.GetComponent<RectTransform>();
                if (scrollbarRect != null)
                {
                    scrollbarRect.anchorMin = new Vector2(1, 0);
                    scrollbarRect.anchorMax = new Vector2(1, 1);
                    scrollbarRect.pivot = new Vector2(1, 0.5f);
                    scrollbarRect.sizeDelta = new Vector2(20, 0);
                    scrollbarRect.anchoredPosition = new Vector2(-10, 0);
                }
            }
            
            // Move all UpgradeItemUI to content if they're in wrong parent
            MoveUpgradeItemsToContent();
            
            // Force immediate layout rebuild
            RefreshScrollView();
            
            Debug.Log($"[ScrollViewFixer] Fixed - Children: {content.childCount}, Scrollbar Active: {verticalScrollbar?.gameObject.activeSelf}");
        }

        void MoveUpgradeItemsToContent()
        {
            var allUpgradeItems = FindObjectsOfType<UpgradeItemUI>(true);
            int movedCount = 0;
            
            foreach (var item in allUpgradeItems)
            {
                if (item.transform.parent != content)
                {
                    // Check if it's under Viewport (common mistake)
                    if (item.transform.parent != null && item.transform.parent.name == "Viewport")
                    {
                        item.transform.SetParent(content, false);
                        movedCount++;
                    }
                }
            }
            
            if (movedCount > 0)
            {
                Debug.Log($"[ScrollViewFixer] Moved {movedCount} upgrade items to Content");
            }
        }

        public void RefreshScrollView()
        {
            if (content != null)
            {
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(content);
                
                // Update scrollbar
                if (scrollRect != null)
                {
                    scrollRect.verticalNormalizedPosition = 1f; // Reset to top
                    scrollRect.CalculateLayoutInputVertical();
                    scrollRect.SetLayoutVertical();
                }
            }
        }

        [ContextMenu("Force Fix ScrollView")]
        public void ForceFixScrollView()
        {
            FixScrollView();
        }

        [ContextMenu("Debug ScrollView Info")]
        public void DebugInfo()
        {
            if (content == null) return;
            
            Debug.Log($"[ScrollViewFixer] Debug Info:");
            Debug.Log($"  - Content Children: {content.childCount}");
            Debug.Log($"  - Content Height: {GetContentHeight():F1}");
            Debug.Log($"  - Viewport Height: {(scrollRect.viewport != null ? scrollRect.viewport.rect.height : 0):F1}");
            Debug.Log($"  - Scrollbar Active: {verticalScrollbar?.gameObject.activeSelf}");
            Debug.Log($"  - Can Scroll: {GetContentHeight() > (scrollRect.viewport != null ? scrollRect.viewport.rect.height : 0)}");
        }
    }
}