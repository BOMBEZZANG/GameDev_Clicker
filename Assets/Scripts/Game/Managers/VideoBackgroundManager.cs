using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;

namespace GameDevClicker.Game.Managers
{
    public class VideoBackgroundManager : MonoBehaviour
    {
        [Header("Video Settings")]
        public RawImage backgroundImage;
        public VideoPlayer videoPlayer;
        [Tooltip("Stretch: Fill entire screen (may distort)\nFitInside: Fit video maintaining aspect ratio (may have black bars)\nFitOutside: Cover entire screen maintaining aspect ratio (may crop)")]
        public VideoAspectRatio aspectRatioMode = VideoAspectRatio.Stretch;
        
        [Header("Stage Configuration")]
        public List<StageVideoData> stageData = new List<StageVideoData>();
        
        [Header("Click Zone Detection")]
        public List<ClickZoneData> clickZones = new List<ClickZoneData>();
        
        private int currentStageIndex = 0;
        private RenderTexture renderTexture;
        
        [System.Serializable]
        public class StageVideoData
        {
            public string stageName;
            public string videoFileName;
            public List<ClickZoneData> stageClickZones;
        }
        
        [System.Serializable]
        public class ClickZoneData
        {
            public string zoneName;
            public RectTransform zoneTransform;
            public Button zoneButton;
            public UnityEngine.Events.UnityEvent onClickEvent;
        }
        
        void Awake()
        {
            SetupVideoPlayer();
            InitializeStageData();
        }
        
        void Start()
        {
            SetupFullScreenBackground();
            
            if (stageData.Count > 0)
            {
                LoadStageVideo(currentStageIndex);
            }
            else
            {
                Debug.LogWarning("No stage data configured. Please set up stage data in the Inspector.");
            }
        }
        
        void Update()
        {
            // Continuously ensure the background stays full screen
            EnsureFullScreenBackground();
        }
        
        void EnsureFullScreenBackground()
        {
            if (backgroundImage != null)
            {
                RectTransform rectTransform = backgroundImage.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    // Check if size has changed and reset if needed
                    if (rectTransform.anchorMin != Vector2.zero || 
                        rectTransform.anchorMax != Vector2.one ||
                        rectTransform.offsetMin != Vector2.zero ||
                        rectTransform.offsetMax != Vector2.zero)
                    {
                        // Reset to full screen
                        rectTransform.anchorMin = Vector2.zero;
                        rectTransform.anchorMax = Vector2.one;
                        rectTransform.offsetMin = Vector2.zero;
                        rectTransform.offsetMax = Vector2.zero;
                        rectTransform.anchoredPosition = Vector2.zero;
                        rectTransform.sizeDelta = Vector2.zero;
                        
                        // Ensure it stays non-interactive
                        backgroundImage.raycastTarget = false;
                        
                        // Keep it as the first child (behind other UI elements)
                        rectTransform.SetAsFirstSibling();
                    }
                }
            }
        }
        
        void SetupFullScreenBackground()
        {
            if (backgroundImage != null)
            {
                RectTransform rectTransform = backgroundImage.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    // Set anchors to stretch
                    rectTransform.anchorMin = Vector2.zero;
                    rectTransform.anchorMax = Vector2.one;
                    
                    // Reset offsets to make it fill the parent
                    rectTransform.offsetMin = Vector2.zero;
                    rectTransform.offsetMax = Vector2.zero;
                    
                    // Ensure proper stretch
                    rectTransform.anchoredPosition = Vector2.zero;
                    rectTransform.sizeDelta = Vector2.zero;
                    
                    // Make the RawImage non-interactive to prevent clicks
                    backgroundImage.raycastTarget = false;
                    
                    // Remove any potentially interfering components
                    var button = backgroundImage.GetComponent<Button>();
                    if (button != null)
                    {
                        DestroyImmediate(button);
                        Debug.Log("Removed Button component from video background");
                    }
                    
                    var selectable = backgroundImage.GetComponent<Selectable>();
                    if (selectable != null)
                    {
                        DestroyImmediate(selectable);
                        Debug.Log("Removed Selectable component from video background");
                    }
                    
                    Debug.Log("Background RawImage set to full screen and made non-interactive");
                }
            }
        }
        
        void InitializeStageData()
        {
            // Auto-populate stage data if empty
            if (stageData.Count == 0)
            {
                stageData = new List<StageVideoData>
                {
                    new StageVideoData { stageName = "Indie Room", videoFileName = "stage1_indie_room.mp4" },
                    new StageVideoData { stageName = "Mobile Dev", videoFileName = "stage2_mobile_dev.mp4" },
                    new StageVideoData { stageName = "PC Game Dev", videoFileName = "stage3_PC_Game_dev.mp4" },
                    new StageVideoData { stageName = "VR Lab", videoFileName = "stage4_VR_lab.mp4" },
                    new StageVideoData { stageName = "AI", videoFileName = "stage5_Ai.mp4" },
                    new StageVideoData { stageName = "Robot", videoFileName = "stage6_lobot.mp4" },
                    new StageVideoData { stageName = "Rocket", videoFileName = "stage7_rocket.mp4" },
                    new StageVideoData { stageName = "Space", videoFileName = "stage8_space.mp4" },
                    new StageVideoData { stageName = "Black Hole", videoFileName = "stage9_blackhole.mp4" },
                    new StageVideoData { stageName = "Time Machine", videoFileName = "stage10_Timemachine.mp4" }
                };
                Debug.Log("Stage data auto-populated with default values");
            }
        }
        
        void SetupVideoPlayer()
        {
            if (videoPlayer == null)
            {
                videoPlayer = gameObject.AddComponent<VideoPlayer>();
            }
            
            // Create render texture for video output
            renderTexture = new RenderTexture(1920, 1080, 16, RenderTextureFormat.ARGB32);
            renderTexture.Create();
            
            // Configure video player
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.targetTexture = renderTexture;
            videoPlayer.aspectRatio = aspectRatioMode;  // Use configurable aspect ratio
            videoPlayer.isLooping = true;
            videoPlayer.playOnAwake = false;
            
            // Set the render texture to the background image
            if (backgroundImage != null)
            {
                backgroundImage.texture = renderTexture;
            }
            
            // Add video player event listeners
            videoPlayer.prepareCompleted += OnVideoPrepared;
            videoPlayer.errorReceived += OnVideoError;
        }
        
        public void LoadStageVideo(int stageIndex)
        {
            if (stageIndex < 0 || stageIndex >= stageData.Count)
            {
                Debug.LogWarning($"Invalid stage index: {stageIndex}");
                return;
            }
            
            if (string.IsNullOrEmpty(stageData[stageIndex].videoFileName))
            {
                Debug.LogError($"No video filename specified for stage {stageIndex} ({stageData[stageIndex].stageName})");
                return;
            }
            
            currentStageIndex = stageIndex;
            string videoPath = Path.Combine(Application.dataPath, "GameData", "BG_Images", "mp4", stageData[stageIndex].videoFileName);
            
            // Check if file exists
            if (!File.Exists(videoPath))
            {
                Debug.LogError($"Video file not found: {videoPath}");
                return;
            }
            
            // Use file:// protocol for local files
            string videoUrl = "file:///" + videoPath.Replace("\\", "/");
            
            Debug.Log($"Loading video from: {videoUrl}");
            
            videoPlayer.url = videoUrl;
            videoPlayer.Prepare();
            
            // Setup click zones for this stage
            SetupClickZones(stageIndex);
        }
        
        void OnVideoPrepared(VideoPlayer source)
        {
            Debug.Log("Video prepared successfully");
            videoPlayer.Play();
        }
        
        void OnVideoError(VideoPlayer source, string message)
        {
            Debug.LogError($"Video Error: {message}");
        }
        
        void SetupClickZones(int stageIndex)
        {
            // Disable all click zones first
            foreach (var zone in clickZones)
            {
                if (zone.zoneButton != null)
                {
                    zone.zoneButton.gameObject.SetActive(false);
                }
            }
            
            // Enable click zones for current stage
            if (stageIndex < stageData.Count)
            {
                var stage = stageData[stageIndex];
                if (stage.stageClickZones != null)
                {
                    foreach (var zoneData in stage.stageClickZones)
                    {
                        if (zoneData.zoneButton != null)
                        {
                            zoneData.zoneButton.gameObject.SetActive(true);
                            
                            // Clear previous listeners and add new ones
                            zoneData.zoneButton.onClick.RemoveAllListeners();
                            if (zoneData.onClickEvent != null)
                            {
                                zoneData.zoneButton.onClick.AddListener(() => zoneData.onClickEvent.Invoke());
                            }
                        }
                    }
                }
            }
        }
        
        // Public methods for stage management
        public void NextStage()
        {
            if (currentStageIndex < stageData.Count - 1)
            {
                LoadStageVideo(currentStageIndex + 1);
            }
        }
        
        public void PreviousStage()
        {
            if (currentStageIndex > 0)
            {
                LoadStageVideo(currentStageIndex - 1);
            }
        }
        
        public void LoadSpecificStage(string stageName)
        {
            for (int i = 0; i < stageData.Count; i++)
            {
                if (stageData[i].stageName == stageName)
                {
                    LoadStageVideo(i);
                    break;
                }
            }
        }
        
        public void LoadStageByNumber(int stageNumber)
        {
            // Stage numbers are 1-based, array is 0-based
            int index = stageNumber - 1;
            if (index >= 0 && index < stageData.Count)
            {
                LoadStageVideo(index);
            }
        }
        
        [ContextMenu("Fix Background Size")]
        public void FixBackgroundSize()
        {
            SetupFullScreenBackground();
            Debug.Log("Background size manually fixed");
        }
        
        void OnDestroy()
        {
            if (renderTexture != null)
            {
                renderTexture.Release();
                Destroy(renderTexture);
            }
            
            if (videoPlayer != null)
            {
                videoPlayer.Stop();
                videoPlayer.prepareCompleted -= OnVideoPrepared;
                videoPlayer.errorReceived -= OnVideoError;
            }
        }
        
        void OnApplicationPause(bool pauseStatus)
        {
            if (videoPlayer != null)
            {
                if (pauseStatus)
                    videoPlayer.Pause();
                else
                    videoPlayer.Play();
            }
        }
    }
}