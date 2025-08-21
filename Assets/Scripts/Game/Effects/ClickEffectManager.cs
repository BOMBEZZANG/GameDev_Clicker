using System.Collections.Generic;
using UnityEngine;
using TMPro;
using GameDevClicker.Core.Utilities;
using GameDevClicker.Core.Patterns;

namespace GameDevClicker.Game.Effects
{
    /// <summary>
    /// Manages click effects including floating text for currency gains
    /// </summary>
    public class ClickEffectManager : Singleton<ClickEffectManager>
    {
        [Header("Prefab Settings")]
        [SerializeField] private GameObject floatingTextPrefab;
        [SerializeField] private Transform effectContainer;
        [SerializeField] private Canvas effectCanvas;
        
        [Header("Pool Settings")]
        [SerializeField] private int poolSize = 20;
        [SerializeField] private int maxPoolSize = 50; // Maximum objects ever created
        [SerializeField] private bool expandPoolIfNeeded = true;
        [SerializeField] private float cleanupInterval = 5f; // Clean up every 5 seconds
        
        [Header("Visual Settings")]
        [SerializeField] private Color moneyColor = new Color(1f, 0.84f, 0f); // Gold
        [SerializeField] private Color expColor = new Color(0.5f, 0.8f, 1f); // Light Blue
        [SerializeField] private Color bonusColor = new Color(1f, 0.4f, 0.4f); // Red for bonuses
        [SerializeField] private Color criticalColor = new Color(1f, 0.2f, 0.2f); // Bright Red for crits
        
        [Header("Effect Settings")]
        [SerializeField] private float baseTextSize = 48f;
        [SerializeField] private float criticalTextSize = 64f;
        [SerializeField] private Vector2 randomOffsetRange = new Vector2(50f, 30f);
        [SerializeField] private bool combineNearbyEffects = true;
        [SerializeField] private float combineRadius = 50f;
        
        private Queue<FloatingText> textPool = new Queue<FloatingText>();
        private List<FloatingText> activeTexts = new List<FloatingText>();
        private int totalObjectsCreated = 0; // Track total objects created for memory monitoring
        
        // For combining nearby clicks
        private struct PendingEffect
        {
            public Vector2 position;
            public float moneyAmount;
            public float expAmount;
            public float timestamp;
        }
        private List<PendingEffect> pendingEffects = new List<PendingEffect>();
        private float combineWindow = 0.1f; // Combine effects within 100ms
        
        protected override void Awake()
        {
            base.Awake();
            InitializePool();
            
            // Start cleanup coroutine
            StartCoroutine(MemoryCleanupRoutine());
        }
        
        private void InitializePool()
        {
            // Create or find effect container
            if (effectContainer == null)
            {
                // Always create a new container with high sorting order canvas
                GameObject containerObj = GameObject.Find("ClickEffectContainer");
                if (containerObj != null)
                {
                    Debug.Log("[ClickEffectManager] Destroying existing ClickEffectContainer to create new one with proper sorting");
                    DestroyImmediate(containerObj);
                }
                
                // Force creation of new container with unique name
                containerObj = new GameObject("FloatingTextCanvas_HighPriority");
                
                // Create dedicated canvas for floating text effects with high sorting order
                Canvas floatingTextCanvas = containerObj.AddComponent<Canvas>();
                floatingTextCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                floatingTextCanvas.sortingOrder = 1000; // Very high priority to appear above everything
                
                // Add CanvasScaler to handle different screen resolutions
                UnityEngine.UI.CanvasScaler scaler = containerObj.AddComponent<UnityEngine.UI.CanvasScaler>();
                scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
                
                // Add GraphicRaycaster for UI interactions (though we don't need clicks on floating text)
                containerObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                
                effectCanvas = floatingTextCanvas;
                effectContainer = containerObj.transform;
                
                Debug.Log($"[ClickEffectManager] Created dedicated floating text canvas with sorting order: {floatingTextCanvas.sortingOrder}");
                Debug.Log($"[ClickEffectManager] Canvas hierarchy: {containerObj.name} -> Parent: {(containerObj.transform.parent ? containerObj.transform.parent.name : "None")}");
                
                // Set up RectTransform for proper UI positioning
                RectTransform rect = effectContainer.GetComponent<RectTransform>();
                if (rect == null)
                {
                    rect = effectContainer.gameObject.AddComponent<RectTransform>();
                }
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.sizeDelta = Vector2.zero;
                rect.anchoredPosition = Vector2.zero;
            }
            
            // Create initial pool
            for (int i = 0; i < poolSize; i++)
            {
                CreatePooledText();
            }
        }
        
        private FloatingText CreatePooledText()
        {
            GameObject textObj;
            
            if (floatingTextPrefab != null)
            {
                textObj = Instantiate(floatingTextPrefab, effectContainer);
            }
            else
            {
                // Create text object programmatically if no prefab
                textObj = new GameObject("FloatingText");
                textObj.transform.SetParent(effectContainer, false);
                
                // Add RectTransform for UI
                RectTransform rect = textObj.AddComponent<RectTransform>();
                rect.sizeDelta = new Vector2(200, 50);
                
                // Add TextMeshPro
                TextMeshProUGUI tmpText = textObj.AddComponent<TextMeshProUGUI>();
                tmpText.fontSize = 48f;
                tmpText.fontStyle = FontStyles.Bold;
                tmpText.alignment = TextAlignmentOptions.Center;
                tmpText.enableWordWrapping = false;
                
                // Add outline for better visibility
                tmpText.outlineColor = Color.black;
                tmpText.outlineWidth = 0.2f;
                
                // Add CanvasGroup for fading
                textObj.AddComponent<CanvasGroup>();
            }
            
            FloatingText floatingText = textObj.GetComponent<FloatingText>();
            if (floatingText == null)
            {
                floatingText = textObj.AddComponent<FloatingText>();
            }
            
            textObj.SetActive(false);
            textPool.Enqueue(floatingText);
            totalObjectsCreated++;
            
            Debug.Log($"[ClickEffectManager] Created pooled text object #{totalObjectsCreated}");
            
            return floatingText;
        }
        
        private FloatingText GetPooledText()
        {
            if (textPool.Count == 0)
            {
                if (expandPoolIfNeeded && totalObjectsCreated < maxPoolSize)
                {
                    return CreatePooledText();
                }
                else
                {
                    // Reuse the oldest active text - this prevents infinite object creation
                    if (activeTexts.Count > 0)
                    {
                        FloatingText oldest = activeTexts[0];
                        activeTexts.RemoveAt(0);
                        oldest.Reset();
                        Debug.Log($"[ClickEffectManager] Reusing oldest text object. Total created: {totalObjectsCreated}");
                        return oldest;
                    }
                    Debug.LogWarning("[ClickEffectManager] No available text objects!");
                    return null;
                }
            }
            
            return textPool.Dequeue();
        }
        
        private void ReturnToPool(FloatingText text)
        {
            activeTexts.Remove(text);
            textPool.Enqueue(text);
        }
        
        /// <summary>
        /// Show a click effect at the specified position
        /// </summary>
        public void ShowClickEffect(Vector2 screenPosition, float moneyGained, float expGained, bool isCritical = false)
        {
            ShowClickEffect(screenPosition, moneyGained, expGained, isCritical, 1); // Default to stage 1
        }
        
        /// <summary>
        /// Show a click effect at the specified position with stage-specific sound
        /// </summary>
        public void ShowClickEffect(Vector2 screenPosition, float moneyGained, float expGained, bool isCritical, int stage)
        {
            Debug.Log($"[ClickEffectManager] ShowClickEffect called: Money={moneyGained}, EXP={expGained}, Position={screenPosition}, Stage={stage}");
            Debug.Log($"[ClickEffectManager] Screen resolution: {Screen.width}x{Screen.height}");
            
            // Play stage-specific click sound
            if (ClickSoundManager.Instance != null)
            {
                ClickSoundManager.Instance.PlayClickSound(stage);
            }
            
            // Calculate position around click zone (not inside it)
            Vector2 canvasPosition = GetPositionAroundClickZone();
            
            Debug.Log($"[ClickEffectManager] Canvas position calculated: {canvasPosition}");
            
            // Show money effect
            if (moneyGained > 0)
            {
                string moneyText = $"+{NumberFormatter.Format((long)moneyGained)} 💰";
                Debug.Log($"[ClickEffectManager] Showing money text: {moneyText}");
                ShowFloatingText(
                    moneyText,
                    canvasPosition + Vector2.left * 30,
                    isCritical ? criticalColor : moneyColor,
                    isCritical ? criticalTextSize : baseTextSize,
                    isCritical ? FloatingTextStyle.Critical : FloatingTextStyle.Default
                );
            }
            
            // Show EXP effect (slightly offset to the right)
            if (expGained > 0)
            {
                string expText = $"+{NumberFormatter.Format((long)expGained)} EXP";
                Debug.Log($"[ClickEffectManager] Showing EXP text: {expText}");
                ShowFloatingText(
                    expText,
                    canvasPosition + Vector2.right * 30,
                    expColor,
                    baseTextSize,
                    FloatingTextStyle.Default
                );
            }
        }
        
        /// <summary>
        /// Calculate a position around the click zone but not inside it
        /// Click zone: Left 400, Top 600, Right 400, Bottom 500 (in 1080x1920 screen)
        /// </summary>
        private Vector2 GetPositionAroundClickZone()
        {
            // Define click zone boundaries (in screen coordinates)
            float clickZoneLeft = 400f;
            float clickZoneTop = 600f;
            float clickZoneRight = Screen.width - 400f; // 680 for 1080 width
            float clickZoneBottom = Screen.height - 500f; // 1420 for 1920 height
            
            // Define margin around click zone
            float margin = 50f;
            
            // Available positions around the click zone
            Vector2[] positions = new Vector2[]
            {
                // Above click zone
                new Vector2(Random.Range(clickZoneLeft, clickZoneRight), clickZoneTop - margin - Random.Range(0, 100)),
                // Below click zone
                new Vector2(Random.Range(clickZoneLeft, clickZoneRight), clickZoneBottom + margin + Random.Range(0, 100)),
                // Left of click zone
                new Vector2(clickZoneLeft - margin - Random.Range(0, 100), Random.Range(clickZoneTop, clickZoneBottom)),
                // Right of click zone
                new Vector2(clickZoneRight + margin + Random.Range(0, 100), Random.Range(clickZoneTop, clickZoneBottom))
            };
            
            // Randomly select one of the positions
            Vector2 screenPos = positions[Random.Range(0, positions.Length)];
            
            // Convert to canvas space (ScreenSpaceOverlay uses screen center as origin)
            Vector2 canvasPosition = new Vector2(
                screenPos.x - Screen.width * 0.5f,
                screenPos.y - Screen.height * 0.5f
            );
            
            // Add small random variation for more natural spread
            Vector2 randomVariation = new Vector2(
                Random.Range(-20f, 20f),
                Random.Range(-20f, 20f)
            );
            
            return canvasPosition + randomVariation;
        }
        
        /// <summary>
        /// Show a custom floating text effect
        /// </summary>
        public void ShowFloatingText(string text, Vector2 position, Color color, float fontSize = 0, FloatingTextStyle style = FloatingTextStyle.Default)
        {
            Debug.Log($"[ClickEffectManager] ShowFloatingText: '{text}' at {position}, Color={color}, Size={fontSize}");
            
            FloatingText floatingText = GetPooledText();
            if (floatingText == null) 
            {
                Debug.LogError("[ClickEffectManager] Could not get pooled text object!");
                return;
            }
            
            if (fontSize <= 0) fontSize = baseTextSize;
            
            Debug.Log($"[ClickEffectManager] Pool status: Active={activeTexts.Count}, Pooled={textPool.Count}");
            
            floatingText.SetAnimationStyle(style);
            floatingText.Show(text, position, color, fontSize, ReturnToPool);
            activeTexts.Add(floatingText);
            
            Debug.Log($"[ClickEffectManager] Text object activated: {floatingText.name}");
        }
        
        /// <summary>
        /// Show a combo or multiplier effect
        /// </summary>
        public void ShowComboEffect(int comboCount, Vector2 position)
        {
            string comboText = $"COMBO x{comboCount}!";
            Color comboColor = Color.Lerp(moneyColor, bonusColor, Mathf.Min(comboCount / 10f, 1f));
            float comboSize = baseTextSize + Mathf.Min(comboCount * 2, 20);
            
            ShowFloatingText(comboText, position, comboColor, comboSize, FloatingTextStyle.Bounce);
        }
        
        /// <summary>
        /// Show level up or achievement effect
        /// </summary>
        public void ShowSpecialEffect(string message, Vector2 position)
        {
            ShowFloatingText(message, position, bonusColor, criticalTextSize, FloatingTextStyle.Critical);
        }
        
        /// <summary>
        /// Clear all active effects
        /// </summary>
        public void ClearAllEffects()
        {
            foreach (var text in activeTexts)
            {
                text.Reset();
                textPool.Enqueue(text);
            }
            activeTexts.Clear();
        }
        
        /// <summary>
        /// Memory cleanup routine to prevent excessive object accumulation
        /// </summary>
        private System.Collections.IEnumerator MemoryCleanupRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(cleanupInterval);
                
                // Log current memory usage
                int activeCount = activeTexts.Count;
                int pooledCount = textPool.Count;
                Debug.Log($"[ClickEffectManager] Memory Status: Active={activeCount}, Pooled={pooledCount}, Total Created={totalObjectsCreated}");
                
                // If we have too many pooled objects, destroy some
                if (textPool.Count > poolSize * 2)
                {
                    int excessCount = textPool.Count - poolSize;
                    for (int i = 0; i < excessCount; i++)
                    {
                        if (textPool.Count > poolSize)
                        {
                            FloatingText excess = textPool.Dequeue();
                            if (excess != null && excess.gameObject != null)
                            {
                                Destroy(excess.gameObject);
                                totalObjectsCreated--;
                            }
                        }
                    }
                    Debug.Log($"[ClickEffectManager] Cleaned up {excessCount} excess objects");
                }
            }
        }
        
        /// <summary>
        /// Get current memory usage statistics
        /// </summary>
        public string GetMemoryStats()
        {
            return $"Active: {activeTexts.Count}, Pooled: {textPool.Count}, Total Created: {totalObjectsCreated}";
        }
        
        private void OnDestroy()
        {
            ClearAllEffects();
        }
    }
}