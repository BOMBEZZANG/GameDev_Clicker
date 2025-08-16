using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections.Generic;
using GameDevClicker.Core.Patterns;
using GameDevClicker.Game.Models;
using GameDevClicker.Game.Managers;

/// <summary>
/// Setup script for HTML Background system that integrates with GameDevClicker
/// This demonstrates how to configure the HTMLBackgroundManager for your game
/// </summary>
public class HTMLBackgroundSetup : MonoBehaviour
{
    [Header("UI References")]
    public RectTransform backgroundPanel;
    public Camera uiCamera;
    public Button nextStageButton;
    public Button prevStageButton;
    public Text stageDisplayText;
    
    [Header("Game Integration")]
    public MonoBehaviour gamePresenter; // Reference to your GamePresenter
    
    private HTMLBackgroundManager htmlManager;
    private int currentDisplayStage = 0;
    
    void Start()
    {
        SetupHTMLBackground();
        SetupUIControls();
    }
    
    void SetupHTMLBackground()
    {
        // Create HTMLBackgroundManager if it doesn't exist
        htmlManager = FindObjectOfType<HTMLBackgroundManager>();
        if (htmlManager == null)
        {
            GameObject managerGO = new GameObject("HTMLBackgroundManager");
            htmlManager = managerGO.AddComponent<HTMLBackgroundManager>();
        }
        
        // Configure the manager
        htmlManager.backgroundPanel = backgroundPanel;
        htmlManager.uiCamera = uiCamera != null ? uiCamera : Camera.main;
        
        // Setup stage data
        ConfigureStageData();
        
        Debug.Log("HTML Background system configured successfully!");
    }
    
    void ConfigureStageData()
    {
        htmlManager.stageData = new List<HTMLBackgroundManager.StageHTMLData>
        {
            CreateStageData("Stage 1: Indie Room", "stage1_indie_room.html", new List<HTMLBackgroundManager.ClickZoneData>
            {
                CreateClickZone("Laptop", new Vector2(0.35f, 0.4f), new Vector2(0.65f, 0.7f), OnLaptopClick),
                CreateClickZone("Coffee", new Vector2(0.65f, 0.4f), new Vector2(0.75f, 0.6f), OnCoffeeClick),
                CreateClickZone("Cat", new Vector2(0.75f, 0.6f), new Vector2(0.9f, 0.8f), OnCatClick),
                CreateClickZone("Bookshelf", new Vector2(0.8f, 0.2f), new Vector2(0.95f, 0.6f), OnBookshelfClick)
            }),
            
            CreateStageData("Stage 2: Mobile Dev", "stage2_mobile_dev.html", new List<HTMLBackgroundManager.ClickZoneData>
            {
                CreateClickZone("Phone", new Vector2(0.4f, 0.4f), new Vector2(0.6f, 0.7f), OnPhoneClick),
                CreateClickZone("Tablet", new Vector2(0.2f, 0.3f), new Vector2(0.4f, 0.6f), OnTabletClick)
            }),
            
            CreateStageData("Stage 3: PC Game Dev", "stage3_PC_Game_dev.html", new List<HTMLBackgroundManager.ClickZoneData>
            {
                CreateClickZone("PC", new Vector2(0.3f, 0.2f), new Vector2(0.7f, 0.8f), OnPCClick),
                CreateClickZone("Monitor", new Vector2(0.25f, 0.1f), new Vector2(0.75f, 0.5f), OnMonitorClick)
            }),
            
            CreateStageData("Stage 4: VR Lab", "stage4_VR_lab.html", new List<HTMLBackgroundManager.ClickZoneData>
            {
                CreateClickZone("VR Headset", new Vector2(0.3f, 0.3f), new Vector2(0.7f, 0.7f), OnVRClick)
            }),
            
            CreateStageData("Stage 5: AI Development", "stage5_Ai.html", new List<HTMLBackgroundManager.ClickZoneData>
            {
                CreateClickZone("AI Server", new Vector2(0.2f, 0.2f), new Vector2(0.8f, 0.8f), OnAIClick)
            }),
            
            CreateStageData("Stage 6: Robot Lab", "stage6_lobot.html", new List<HTMLBackgroundManager.ClickZoneData>
            {
                CreateClickZone("Robot", new Vector2(0.3f, 0.3f), new Vector2(0.7f, 0.7f), OnRobotClick)
            }),
            
            CreateStageData("Stage 7: Rocket Launch", "stage7_rocket.html", new List<HTMLBackgroundManager.ClickZoneData>
            {
                CreateClickZone("Rocket", new Vector2(0.4f, 0.1f), new Vector2(0.6f, 0.9f), OnRocketClick)
            }),
            
            CreateStageData("Stage 8: Space Station", "stage8_space.html", new List<HTMLBackgroundManager.ClickZoneData>
            {
                CreateClickZone("Control Panel", new Vector2(0.2f, 0.4f), new Vector2(0.8f, 0.8f), OnSpaceClick)
            }),
            
            CreateStageData("Stage 9: Black Hole Research", "stage9_blackhole.html", new List<HTMLBackgroundManager.ClickZoneData>
            {
                CreateClickZone("Black Hole", new Vector2(0.3f, 0.3f), new Vector2(0.7f, 0.7f), OnBlackHoleClick)
            }),
            
            CreateStageData("Stage 10: Time Machine", "stage10_Timemachine.html", new List<HTMLBackgroundManager.ClickZoneData>
            {
                CreateClickZone("Time Machine", new Vector2(0.2f, 0.2f), new Vector2(0.8f, 0.8f), OnTimeMachineClick)
            })
        };
    }
    
    HTMLBackgroundManager.StageHTMLData CreateStageData(string stageName, string htmlFileName, List<HTMLBackgroundManager.ClickZoneData> clickZones)
    {
        return new HTMLBackgroundManager.StageHTMLData
        {
            stageName = stageName,
            htmlFileName = htmlFileName,
            clickZones = clickZones
        };
    }
    
    HTMLBackgroundManager.ClickZoneData CreateClickZone(string zoneName, Vector2 topLeft, Vector2 bottomRight, UnityAction clickAction)
    {
        var clickZone = new HTMLBackgroundManager.ClickZoneData
        {
            zoneName = zoneName,
            topLeft = topLeft,
            bottomRight = bottomRight,
            onClickEvent = new UnityEvent()
        };
        
        clickZone.onClickEvent.AddListener(clickAction);
        return clickZone;
    }
    
    void SetupUIControls()
    {
        if (nextStageButton != null)
        {
            nextStageButton.onClick.AddListener(() => {
                currentDisplayStage++;
                if (currentDisplayStage >= htmlManager.stageData.Count)
                    currentDisplayStage = htmlManager.stageData.Count - 1;
                LoadCurrentStage();
            });
        }
        
        if (prevStageButton != null)
        {
            prevStageButton.onClick.AddListener(() => {
                currentDisplayStage--;
                if (currentDisplayStage < 0)
                    currentDisplayStage = 0;
                LoadCurrentStage();
            });
        }
        
        LoadCurrentStage();
    }
    
    void LoadCurrentStage()
    {
        if (htmlManager != null)
        {
            htmlManager.LoadStageHTML(currentDisplayStage);
            
            if (stageDisplayText != null)
            {
                stageDisplayText.text = $"Stage {currentDisplayStage + 1}: {htmlManager.stageData[currentDisplayStage].stageName}";
            }
        }
    }
    
    // Click event handlers for different objects - integrated with GameDevClicker
    void OnLaptopClick()
    {
        Debug.Log("Clicked on Laptop! Adding programming progress...");
        
        // Simulate a coding session - give money and experience
        var model = GameModel.Instance;
        if (model != null)
        {
            float bonusMoney = model.MoneyPerClick * 2f; // Double click value
            float bonusExp = model.ExpPerClick * 2f;
            
            model.AddMoney((long)bonusMoney);
            model.AddExperience((long)bonusExp);
            
            // Trigger click effect event
            GameEvents.InvokeClickPerformed(bonusMoney, bonusExp);
            GameEvents.InvokeNotificationShown("Coding Session", $"Earned ${bonusMoney:F0} and {bonusExp:F0} XP!");
        }
    }
    
    void OnCoffeeClick()
    {
        Debug.Log("Clicked on Coffee! Restoring energy...");
        
        // Give a temporary productivity boost
        var model = GameModel.Instance;
        if (model != null)
        {
            float energyBonus = model.MoneyPerClick * 5f; // Big energy boost
            model.AddMoney((long)energyBonus);
            
            GameEvents.InvokeNotificationShown("Coffee Break", $"Productivity boost! +${energyBonus:F0}");
        }
    }
    
    void OnCatClick()
    {
        Debug.Log("Clicked on Cat! Gaining inspiration...");
        
        // Give creativity/inspiration bonus
        var model = GameModel.Instance;
        if (model != null)
        {
            float inspirationBonus = model.ExpPerClick * 3f;
            model.AddExperience((long)inspirationBonus);
            
            GameEvents.InvokeNotificationShown("Cat Inspiration", $"Feeling creative! +{inspirationBonus:F0} XP");
        }
    }
    
    void OnBookshelfClick()
    {
        Debug.Log("Clicked on Bookshelf! Learning new skills...");
        
        // Give knowledge/learning bonus
        var model = GameModel.Instance;
        if (model != null)
        {
            float learningBonus = model.ExpPerClick * 4f;
            model.AddExperience((long)learningBonus);
            
            GameEvents.InvokeNotificationShown("Knowledge Gained", $"Learned something new! +{learningBonus:F0} XP");
        }
    }
    
    void OnPhoneClick()
    {
        Debug.Log("Clicked on Phone! Mobile development boost...");
    }
    
    void OnTabletClick()
    {
        Debug.Log("Clicked on Tablet! App development progress...");
    }
    
    void OnPCClick()
    {
        Debug.Log("Clicked on PC! Game development boost...");
    }
    
    void OnMonitorClick()
    {
        Debug.Log("Clicked on Monitor! Visual debugging...");
    }
    
    void OnVRClick()
    {
        Debug.Log("Clicked on VR Headset! Virtual reality breakthrough...");
    }
    
    void OnAIClick()
    {
        Debug.Log("Clicked on AI Server! Machine learning progress...");
    }
    
    void OnRobotClick()
    {
        Debug.Log("Clicked on Robot! Robotics advancement...");
    }
    
    void OnRocketClick()
    {
        Debug.Log("Clicked on Rocket! Space technology progress...");
    }
    
    void OnSpaceClick()
    {
        Debug.Log("Clicked on Space Control Panel! Interstellar research...");
    }
    
    void OnBlackHoleClick()
    {
        Debug.Log("Clicked on Black Hole! Quantum physics breakthrough...");
    }
    
    void OnTimeMachineClick()
    {
        Debug.Log("Clicked on Time Machine! Temporal mechanics mastered...");
    }
    
    // Public method to load a specific stage (can be called from other scripts)
    public void LoadStage(int stageIndex)
    {
        currentDisplayStage = Mathf.Clamp(stageIndex, 0, htmlManager.stageData.Count - 1);
        LoadCurrentStage();
    }
    
    public void LoadStageByName(string stageName)
    {
        for (int i = 0; i < htmlManager.stageData.Count; i++)
        {
            if (htmlManager.stageData[i].stageName.Contains(stageName))
            {
                LoadStage(i);
                break;
            }
        }
    }
}