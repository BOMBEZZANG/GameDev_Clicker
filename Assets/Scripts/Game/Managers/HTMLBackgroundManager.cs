using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;
#if UNITY_2018_4_OR_NEWER
using UnityEngine.Networking;
#endif

namespace GameDevClicker.Game.Managers
{
    public class HTMLBackgroundManager : MonoBehaviour
    {
        [Header("WebView Settings")]
        public RectTransform backgroundPanel;
        public Camera uiCamera;
        
        [Header("HTML Stage Configuration")]
        public List<StageHTMLData> stageData = new List<StageHTMLData>();
        
        [Header("Click Zone Detection")]
        public LayerMask clickZoneLayer = 1;
        public float clickDetectionDepth = 100f;
        
        private WebViewObject webViewObject;
        private int currentStageIndex = 0;
        private Dictionary<string, ClickZone> clickZones = new Dictionary<string, ClickZone>();
        
        [System.Serializable]
        public class StageHTMLData
        {
            public string stageName;
            public string htmlFileName;
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
            InitializeWebView();
            LoadStageHTML(currentStageIndex);
        }
        
        void InitializeWebView()
        {
            if (webViewObject != null)
            {
                Destroy(webViewObject.gameObject);
            }
            
            if (!WebViewObject.IsWebViewAvailable())
            {
                Debug.LogError("WebView is not available on this platform!");
                return;
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
        
        void SetWebViewMargins()
        {
            if (backgroundPanel == null) return;
            
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
            if (stageIndex < 0 || stageIndex >= stageData.Count) 
            {
                Debug.LogWarning($"Invalid stage index: {stageIndex}");
                return;
            }
            
            if (webViewObject == null)
            {
                Debug.LogError("WebViewObject is null. Make sure InitializeWebView was called successfully.");
                return;
            }
            
            currentStageIndex = stageIndex;
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
                    Debug.LogError($"HTML file not found: {htmlPath}");
                }
            }
            
            SetupClickZones(stageIndex);
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
                Debug.LogError($"Failed to load HTML: {www.error}");
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
                Debug.LogError($"Failed to load HTML: {www.error}");
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
                
                // Convert screen position to panel-relative position
                Vector2 localPoint;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    backgroundPanel, mousePos, uiCamera, out localPoint))
                {
                    // Convert to normalized coordinates (0-1)
                    Vector2 normalizedPos = new Vector2(
                        (localPoint.x + backgroundPanel.rect.width * 0.5f) / backgroundPanel.rect.width,
                        (localPoint.y + backgroundPanel.rect.height * 0.5f) / backgroundPanel.rect.height
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
                    Debug.Log($"Clicked on zone: {kvp.Key}");
                    kvp.Value.clickEvent?.Invoke();
                    break; // Only trigger the first matching zone
                }
            }
        }
        
        // WebView event handlers
        void OnWebViewMessage(string message)
        {
            Debug.Log($"WebView Message: {message}");
        }
        
        void OnWebViewError(string error)
        {
            Debug.LogError($"WebView Error: {error}");
        }
        
        void OnWebViewHttpError(string error)
        {
            Debug.LogError($"WebView HTTP Error: {error}");
        }
        
        void OnWebViewLoaded(string url)
        {
            Debug.Log($"WebView Loaded: {url}");
        }
        
        void OnWebViewStarted(string url)
        {
            Debug.Log($"WebView Started: {url}");
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
        
        void OnDestroy()
        {
            if (webViewObject != null)
            {
                webViewObject.SetVisibility(false);
            }
        }
        
        void OnApplicationPause(bool pauseStatus)
        {
            if (webViewObject != null)
            {
                if (pauseStatus)
                    webViewObject.Pause();
                else
                    webViewObject.Resume();
            }
        }
    }
}