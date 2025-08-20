using UnityEngine;
using UnityEngine.UI;
using GameDevClicker.Game.Views;

namespace GameDevClicker.Game.Utils
{
    /// <summary>
    /// Runtime UI auto-fixer that ensures proper UI setup without requiring manual intervention
    /// </summary>
    public class UIAutoFixer : MonoBehaviour
    {
        [Header("Auto Fix Settings")]
        [SerializeField] private bool enableAutoFix = true;
        [SerializeField] private bool fixOnStart = true;
        [SerializeField] private bool fixOnEnable = false;
        [SerializeField] private float delayBeforeFix = 0.1f;
        
        private bool hasFixed = false;

        void Start()
        {
            if (enableAutoFix && fixOnStart)
            {
                Invoke(nameof(PerformAutoFix), delayBeforeFix);
            }
        }

        void OnEnable()
        {
            if (enableAutoFix && fixOnEnable && !hasFixed)
            {
                Invoke(nameof(PerformAutoFix), delayBeforeFix);
            }
        }

        public void PerformAutoFix()
        {
            if (hasFixed) return;
            
            Debug.Log("[UIAutoFixer] Starting automatic UI fixes...");
            
            FixCanvasAssignment();
            FixScrollViewContent();
            FixParentAssignment();
            FixItemPositioning();
            
            hasFixed = true;
            Debug.Log("[UIAutoFixer] ✅ All UI fixes completed!");
        }

        private void FixCanvasAssignment()
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
                    Debug.Log($"[UIAutoFixer] ✅ Moved GameViewUI under Canvas: {targetCanvas.name}");
                }
            }
        }

        private void FixScrollViewContent()
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
                        layoutGroup.childForceExpandWidth = true;
                        layoutGroup.childForceExpandHeight = false;
                        layoutGroup.childControlHeight = false;
                        layoutGroup.childControlWidth = true;
                        layoutGroup.spacing = 5f;
                        layoutGroup.padding = new RectOffset(10, 10, 10, 10);
                    }

                    var sizeFitter = contentRect.GetComponent<ContentSizeFitter>();
                    if (sizeFitter == null)
                    {
                        sizeFitter = contentRect.gameObject.AddComponent<ContentSizeFitter>();
                        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                        sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                    }

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
                        scrollRect.verticalScrollbar.value = 1f; // Start at top
                        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
                        scrollRect.verticalScrollbarSpacing = -3f;
                        
                        // Ensure scrollbar is active
                        scrollRect.verticalScrollbar.gameObject.SetActive(true);
                    }
                    
                    // Force rebuild
                    LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
                }
            }
        }

        private void FixParentAssignment()
        {
            var upgradeItems = FindObjectsOfType<UpgradeItemUI>(true);
            var scrollRect = FindObjectOfType<ScrollRect>();

            if (scrollRect?.content == null) return;

            foreach (var item in upgradeItems)
            {
                if (item == null || item.transform == null) continue;

                if (item.transform.parent != null && item.transform.parent.name == "Viewport")
                {
                    item.transform.SetParent(scrollRect.content, false);
                }
            }
        }

        private void FixItemPositioning()
        {
            var upgradeItems = FindObjectsOfType<UpgradeItemUI>(true);

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
                }
            }

            var scrollRect = FindObjectOfType<ScrollRect>();
            if (scrollRect?.content != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
            }
        }

        [ContextMenu("Force Fix UI Now")]
        public void ForceFixUI()
        {
            hasFixed = false;
            PerformAutoFix();
        }
    }
}