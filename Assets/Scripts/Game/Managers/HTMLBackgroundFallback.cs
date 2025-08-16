using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Fallback HTML Background Manager that uses UI Image instead of WebView
/// Use this if WebView setup has issues
/// </summary>
public class HTMLBackgroundFallback : MonoBehaviour
{
    [Header("UI Settings")]
    public Image backgroundImage;
    public Camera uiCamera;
    
    [Header("Stage Configuration")]
    public List<StageImageData> stageData = new List<StageImageData>();
    
    [Header("Click Zone Detection")]
    public LayerMask clickZoneLayer = 1;
    
    private int currentStageIndex = 0;
    private Dictionary<string, ClickZone> clickZones = new Dictionary<string, ClickZone>();
    
    [System.Serializable]
    public class StageImageData
    {
        public string stageName;
        public Sprite backgroundSprite;
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
        LoadStageBackground(currentStageIndex);
    }
    
    public void LoadStageBackground(int stageIndex)
    {
        if (stageIndex < 0 || stageIndex >= stageData.Count) 
        {
            Debug.LogWarning($"Invalid stage index: {stageIndex}");
            return;
        }
        
        currentStageIndex = stageIndex;
        
        // Set background image
        if (backgroundImage != null && stageData[stageIndex].backgroundSprite != null)
        {
            backgroundImage.sprite = stageData[stageIndex].backgroundSprite;
        }
        
        SetupClickZones(stageIndex);
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
            
            // Convert screen position to image-relative position
            RectTransform imageRect = backgroundImage.rectTransform;
            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                imageRect, mousePos, uiCamera, out localPoint))
            {
                // Convert to normalized coordinates (0-1)
                Vector2 normalizedPos = new Vector2(
                    (localPoint.x + imageRect.rect.width * 0.5f) / imageRect.rect.width,
                    (localPoint.y + imageRect.rect.height * 0.5f) / imageRect.rect.height
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
    
    // Public methods for stage management
    public void NextStage()
    {
        if (currentStageIndex < stageData.Count - 1)
        {
            LoadStageBackground(currentStageIndex + 1);
        }
    }
    
    public void PreviousStage()
    {
        if (currentStageIndex > 0)
        {
            LoadStageBackground(currentStageIndex - 1);
        }
    }
    
    public void LoadSpecificStage(string stageName)
    {
        for (int i = 0; i < stageData.Count; i++)
        {
            if (stageData[i].stageName == stageName)
            {
                LoadStageBackground(i);
                break;
            }
        }
    }
    
    public int GetCurrentStageIndex()
    {
        return currentStageIndex;
    }
}