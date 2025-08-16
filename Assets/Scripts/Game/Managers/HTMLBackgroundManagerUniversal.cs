using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;
#if UNITY_2018_4_OR_NEWER
using UnityEngine.Networking;
#endif

namespace GameDevClicker.Game.Managers
{
    /// <summary>
    /// Universal HTML Background Manager that automatically falls back to Image sprites when WebView is not available
    /// </summary>
    public class HTMLBackgroundManagerUniversal : MonoBehaviour
    {
        [Header("Background Display")]
        public Image backgroundImage; // Fallback for non-WebView platforms
        public RectTransform backgroundPanel; // For WebView platforms
        public Camera uiCamera;
        
        [Header("HTML Stage Configuration")]
        public List<StageHTMLData> stageData = new List<StageHTMLData>();
        
        [Header("Fallback Sprite Configuration")]
        public List<Sprite> stageSprites = new List<Sprite>(); // Backup sprites for each stage
        
        [Header("Click Zone Detection")]
        public LayerMask clickZoneLayer = 1;
        public float clickDetectionDepth = 100f;
        
        private WebViewObject webViewObject;
        private int currentStageIndex = 0;
        private Dictionary<string, ClickZone> clickZones = new Dictionary<string, ClickZone>();
        private bool useWebView = false;
        private bool isInitialized = false;
        
        [System.Serializable]
        public class StageHTMLData
        {
            public string stageName;
            public string htmlFileName;
            public Sprite fallbackSprite; // Individual fallback sprite for this stage
            public List<ClickZoneData> clickZones;
        }
        
        [System.Serializable]
        public class ClickZoneData
        {
            public string zoneName;
            public Vector2 topLeft;
            public Vector2 bottomRight;
            public UnityEngine.Events.UnityEvent onClickEvent;
        }
        
        public class ClickZone
        {
            public Rect rect;
            public UnityEngine.Events.UnityEvent clickEvent;
            
            public ClickZone(Rect rect, UnityEngine.Events.UnityEvent clickEvent)
            {
                this.rect = rect;
                this.clickEvent = clickEvent;
            }
        }

        void Start()
        {
            InitializeBackgroundSystem();
            LoadStageHTML(currentStageIndex);
        }
        
        void InitializeBackgroundSystem()
        {
            // Check if WebView is available on this platform
            useWebView = IsWebViewSupported();
            
            if (useWebView)
            {
                Debug.Log("[HTMLBackgroundManager] Using WebView for HTML backgrounds");
                InitializeWebView();
            }
            else
            {
                Debug.Log("[HTMLBackgroundManager] WebView not supported. Using Image fallback for backgrounds");
                InitializeImageFallback();
            }
            
            isInitialized = true;
        }
        
        bool IsWebViewSupported()
        {
            // WebView is primarily supported on mobile and WebGL
            return (Application.platform == RuntimePlatform.Android ||
                    Application.platform == RuntimePlatform.IPhonePlayer ||
                    Application.platform == RuntimePlatform.WebGLPlayer) &&
                   WebViewObject.IsWebViewAvailable();
        }
        
        void InitializeWebView()
        {
            if (backgroundPanel == null)
            {
                Debug.LogError("[HTMLBackgroundManager] Background panel not assigned for WebView mode!");
                useWebView = false;
                InitializeImageFallback();
                return;
            }
            
            if (webViewObject != null)
            {
                Destroy(webViewObject.gameObject);
            }
            
            GameObject webViewGO = new GameObject("WebViewBackground");
            webViewGO.transform.SetParent(backgroundPanel);
            webViewObject = webViewGO.AddComponent<WebViewObject>();
            
            // Configure WebView for background display
            webViewObject.Init(
                cb: OnWebViewMessage,
                err: OnWebViewError,
                httpErr: OnWebViewHttpError,
                ld: OnWebViewLoaded,
                started: OnWebViewStarted,
                transparent: true,
                zoom: false
            );
            
            // Set WebView to cover the background panel
            SetWebViewMargins();
            webViewObject.SetVisibility(true);
            
            // Disable WebView interaction for click-through
            if (webViewObject != null)
            {
                webViewObject.SetInteractionEnabled(false);
            }
        }
        
        void InitializeImageFallback()
        {
            if (backgroundImage == null)
            {
                Debug.LogError("[HTMLBackgroundManager] Background image not assigned for fallback mode!");
                return;
            }
            
            // Hide WebView elements if they exist
            if (backgroundPanel != null)
            {
                backgroundPanel.gameObject.SetActive(false);
            }
        }
        
        void SetWebViewMargins()
        {
            if (backgroundPanel == null || webViewObject == null) return;
            
            RectTransform canvasRect = backgroundPanel.GetComponentInParent<Canvas>().GetComponent<RectTransform>();
            
            // Convert UI coordinates to screen coordinates
            Vector2 panelMin = RectTransformUtility.WorldToScreenPoint(uiCamera, backgroundPanel.TransformPoint(backgroundPanel.rect.min));
            Vector2 panelMax = RectTransformUtility.WorldToScreenPoint(uiCamera, backgroundPanel.TransformPoint(backgroundPanel.rect.max));
            
            int left = Mathf.RoundToInt(panelMin.x);
            int bottom = Mathf.RoundToInt(Screen.height - panelMax.y);
            int right = Mathf.RoundToInt(Screen.width - panelMax.x);
            int top = Mathf.RoundToInt(panelMin.y);
            
            webViewObject.SetMargins(left, top, right, bottom);
        }
        
        public void LoadStageHTML(int stageIndex)
        {
            if (!isInitialized)
            {
                Debug.LogWarning("[HTMLBackgroundManager] System not initialized yet. Call will be ignored.");
                return;
            }
            
            if (stageIndex < 0 || stageIndex >= stageData.Count) 
            {
                Debug.LogWarning($"[HTMLBackgroundManager] Invalid stage index: {stageIndex}");
                return;
            }
            
            currentStageIndex = stageIndex;
            
            if (useWebView)
            {
                LoadHTMLInWebView(stageIndex);
            }
            else
            {
                LoadSpriteInImage(stageIndex);
            }
            
            SetupClickZones(stageIndex);
        }
        
        void LoadHTMLInWebView(int stageIndex)
        {
            if (webViewObject == null)
            {
                Debug.LogError("[HTMLBackgroundManager] WebViewObject is null!");
                return;
            }
            
            string htmlPath = Path.Combine(Application.streamingAssetsPath, "HTML", stageData[stageIndex].htmlFileName);
            
            // For Android, use StreamingAssets approach
            if (Application.platform == RuntimePlatform.Android)
            {
                StartCoroutine(LoadHTMLFromStreamingAssets(stageData[stageIndex].htmlFileName));
            }
            else
            {
                // For other platforms, load directly
                if (File.Exists(htmlPath))
                {
                    string htmlContent = File.ReadAllText(htmlPath);
                    webViewObject.LoadHTML(htmlContent, "");
                }
                else
                {
                    Debug.LogError($"[HTMLBackgroundManager] HTML file not found: {htmlPath}");
                    // Fallback to sprite if HTML fails
                    FallbackToSprite(stageIndex);
                }
            }
        }
        
        void LoadSpriteInImage(int stageIndex)
        {
            if (backgroundImage == null)
            {
                Debug.LogError("[HTMLBackgroundManager] Background image is null!");
                return;
            }
            
            Sprite spriteToUse = null;
            
            // Try to use stage-specific fallback sprite first
            if (stageData[stageIndex].fallbackSprite != null)
            {
                spriteToUse = stageData[stageIndex].fallbackSprite;
            }
            // Then try the general sprite list
            else if (stageIndex < stageSprites.Count && stageSprites[stageIndex] != null)
            {
                spriteToUse = stageSprites[stageIndex];
            }
            
            if (spriteToUse != null)
            {
                backgroundImage.sprite = spriteToUse;
                Debug.Log($"[HTMLBackgroundManager] Loaded sprite for stage {stageIndex}: {stageData[stageIndex].stageName}");
            }
            else
            {
                Debug.LogWarning($"[HTMLBackgroundManager] No sprite available for stage {stageIndex}");
            }
        }
        
        void FallbackToSprite(int stageIndex)
        {
            Debug.Log("[HTMLBackgroundManager] Falling back to sprite display");
            useWebView = false;
            InitializeImageFallback();
            LoadSpriteInImage(stageIndex);
        }
        
        System.Collections.IEnumerator LoadHTMLFromStreamingAssets(string fileName)
        {
            string path = Path.Combine(Application.streamingAssetsPath, "HTML", fileName);
            
#if UNITY_2018_4_OR_NEWER
            UnityWebRequest www = UnityWebRequest.Get(path);
            yield return www.SendWebRequest();
            
            if (www.result == UnityWebRequest.Result.Success)
            {
                if (webViewObject != null)
                {
                    webViewObject.LoadHTML(www.downloadHandler.text, "");
                }
            }
            else
            {
                Debug.LogError($"[HTMLBackgroundManager] Failed to load HTML: {www.error}");
                FallbackToSprite(currentStageIndex);
            }
            
            www.Dispose();
#else
            WWW www = new WWW(path);
            yield return www;
            
            if (string.IsNullOrEmpty(www.error))
            {
                if (webViewObject != null)
                {
                    webViewObject.LoadHTML(www.text, "");
                }
            }
            else
            {
                Debug.LogError($"[HTMLBackgroundManager] Failed to load HTML: {www.error}");
                FallbackToSprite(currentStageIndex);
            }
#endif
        }
        
        void SetupClickZones(int stageIndex)
        {
            clickZones.Clear();
            
            if (stageIndex >= stageData.Count) return;
            
            var stage = stageData[stageIndex];
            foreach (var zoneData in stage.clickZones)
            {
                Rect zoneRect = new Rect(
                    zoneData.topLeft.x,
                    zoneData.topLeft.y,
                    zoneData.bottomRight.x - zoneData.topLeft.x,
                    zoneData.bottomRight.y - zoneData.topLeft.y
                );
                
                clickZones[zoneData.zoneName] = new ClickZone(zoneRect, zoneData.onClickEvent);
            }
        }
        
        void Update()
        {
            HandleClickDetection();
        }
        
        void HandleClickDetection()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Vector2 mousePos = Input.mousePosition;
                Vector2 localPoint;
                
                // Use appropriate UI element for click detection based on mode
                RectTransform targetRect = useWebView ? backgroundPanel : backgroundImage.rectTransform;
                
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    targetRect, mousePos, uiCamera, out localPoint))
                {
                    // Convert to normalized coordinates (0-1)
                    Vector2 normalizedPos = new Vector2(
                        (localPoint.x + targetRect.rect.width * 0.5f) / targetRect.rect.width,
                        (localPoint.y + targetRect.rect.height * 0.5f) / targetRect.rect.height
                    );
                    
                    // Check click zones
                    CheckClickZones(normalizedPos);
                }
            }
        }
        
        void CheckClickZones(Vector2 normalizedPosition)
        {
            foreach (var kvp in clickZones)
            {
                if (kvp.Value.rect.Contains(normalizedPosition))
                {
                    Debug.Log($"[HTMLBackgroundManager] Clicked on zone: {kvp.Key}");
                    kvp.Value.clickEvent?.Invoke();
                    break; // Only trigger the first matching zone
                }
            }
        }
        
        // WebView event handlers
        void OnWebViewMessage(string message)
        {
            Debug.Log($"[HTMLBackgroundManager] WebView Message: {message}");
        }
        
        void OnWebViewError(string error)
        {
            Debug.LogError($"[HTMLBackgroundManager] WebView Error: {error}");
        }
        
        void OnWebViewHttpError(string error)
        {
            Debug.LogError($"[HTMLBackgroundManager] WebView HTTP Error: {error}");
        }
        
        void OnWebViewLoaded(string url)
        {
            Debug.Log($"[HTMLBackgroundManager] WebView Loaded: {url}");
        }
        
        void OnWebViewStarted(string url)
        {
            Debug.Log($"[HTMLBackgroundManager] WebView Started: {url}");
        }
        
        // Public methods for stage management
        public void NextStage()
        {
            if (currentStageIndex < stageData.Count - 1)
            {
                LoadStageHTML(currentStageIndex + 1);
            }
        }
        
        public void PreviousStage()
        {
            if (currentStageIndex > 0)
            {
                LoadStageHTML(currentStageIndex - 1);
            }
        }
        
        public void LoadSpecificStage(string stageName)
        {
            for (int i = 0; i < stageData.Count; i++)
            {
                if (stageData[i].stageName == stageName)
                {
                    LoadStageHTML(i);
                    break;
                }
            }
        }
        
        public bool IsUsingWebView()
        {
            return useWebView;
        }
        
        public int GetCurrentStageIndex()
        {
            return currentStageIndex;
        }
        
        public string GetCurrentStageName()
        {
            if (currentStageIndex >= 0 && currentStageIndex < stageData.Count)
            {
                return stageData[currentStageIndex].stageName;
            }
            return "Unknown Stage";
        }
        
        void OnDestroy()
        {
            if (webViewObject != null)
            {
                webViewObject.SetVisibility(false);
            }
        }
        
        void OnApplicationPause(bool pauseStatus)
        {
            if (webViewObject != null && useWebView)
            {
                if (pauseStatus)
                    webViewObject.Pause();
                else
                    webViewObject.Resume();
            }
        }
    }
}