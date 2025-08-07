using UnityEngine;
using UnityEngine.UI;
using GameDevClicker.Game.Views;

namespace GameDevClicker.Game.Utils
{
    /// <summary>
    /// Runtime utility to fix common UI issues
    /// </summary>
    public class UIFixHelper : MonoBehaviour
    {
        [Header("Debug Options")]
        [SerializeField] private bool fixOnStart = true;
        [SerializeField] private KeyCode debugKey = KeyCode.F1;
        
        private void Start()
        {
            if (fixOnStart)
            {
                Invoke(nameof(FixUIIssues), 1f); // Delay to let everything initialize
            }
        }
        
        private void Update()
        {
            if (Input.GetKeyDown(debugKey))
            {
                FixUIIssues();
            }
        }
        
        [ContextMenu("Fix UI Issues")]
        public void FixUIIssues()
        {
            Debug.Log("🔧 [UIFixHelper] Starting UI fixes...");
            
            FixScrollViewContent();
            FixUpgradeItemPositions();
            ForceLayoutRebuild();
            
            Debug.Log("✅ [UIFixHelper] UI fixes complete!");
        }
        
        private void FixScrollViewContent()
        {
            var scrollRect = FindObjectOfType<ScrollRect>();
            if (scrollRect?.content == null) return;
            
            // Add VerticalLayoutGroup if missing
            var layoutGroup = scrollRect.content.GetComponent<VerticalLayoutGroup>();
            if (layoutGroup == null)
            {
                layoutGroup = scrollRect.content.gameObject.AddComponent<VerticalLayoutGroup>();
                Debug.Log("✅ Added VerticalLayoutGroup to ScrollView Content");
            }
            
            // Configure layout group
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.childControlHeight = false;
            layoutGroup.childControlWidth = true;
            layoutGroup.spacing = 10f;
            
            // Add ContentSizeFitter if missing
            var sizeFitter = scrollRect.content.GetComponent<ContentSizeFitter>();
            if (sizeFitter == null)
            {
                sizeFitter = scrollRect.content.gameObject.AddComponent<ContentSizeFitter>();
                Debug.Log("✅ Added ContentSizeFitter to ScrollView Content");
            }
            
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            // Reset content position
            scrollRect.content.anchoredPosition = Vector2.zero;
        }
        
        private void FixUpgradeItemPositions()
        {
            var upgradeItems = FindObjectsOfType<UpgradeItemUI>();
            var scrollRect = FindObjectOfType<ScrollRect>();
            
            if (scrollRect?.content == null) return;
            
            foreach (var item in upgradeItems)
            {
                // Move to correct parent if needed
                if (item.transform.parent != scrollRect.content)
                {
                    item.transform.SetParent(scrollRect.content, false);
                    Debug.Log($"✅ Moved {item.name} to ScrollView Content");
                }
                
                // Fix RectTransform settings
                var rectTransform = item.GetComponent<RectTransform>();
                
                // Set anchors for full width, fixed height
                rectTransform.anchorMin = new Vector2(0, 1);
                rectTransform.anchorMax = new Vector2(1, 1);
                rectTransform.pivot = new Vector2(0.5f, 1f);
                
                // Set size (full width, fixed height)
                rectTransform.sizeDelta = new Vector2(0, 100);
                rectTransform.anchoredPosition = new Vector2(0, 0);
                
                // Ensure proper scale
                rectTransform.localScale = Vector3.one;
            }
        }
        
        private void ForceLayoutRebuild()
        {
            var scrollRect = FindObjectOfType<ScrollRect>();
            if (scrollRect?.content != null)
            {
                // Force immediate layout update
                LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
                
                // Also rebuild the entire scroll view
                LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.GetComponent<RectTransform>());
                
                Debug.Log("✅ Forced layout rebuild");
            }
        }
        
        // Method to manually fix GameViewUI reference
        public void FixGameViewUIReference()
        {
            var gameViewUI = FindObjectOfType<GameViewUI>();
            var scrollRect = FindObjectOfType<ScrollRect>();
            
            if (gameViewUI != null && scrollRect?.content != null)
            {
                // Use reflection to fix the upgradeListParent reference
                var field = typeof(GameViewUI).GetField("upgradeListParent", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (field != null)
                {
                    field.SetValue(gameViewUI, scrollRect.content);
                    Debug.Log("✅ Fixed GameViewUI.upgradeListParent reference");
                }
            }
        }
    }
}