using UnityEngine;
using GameDevClicker.Core.Patterns;
using GameDevClicker.Game.Models;
using GameDevClicker.Game.Systems;
using GameDevClicker.Game.Managers;

namespace GameDevClicker.Game.Presenters
{
    /// <summary>
    /// Presenter for HTML Background system, integrates with the existing GamePresenter pattern
    /// </summary>
    public class HTMLBackgroundPresenter : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private HTMLBackgroundManager htmlBackgroundManager;
        [SerializeField] private HTMLBackgroundSystem htmlBackgroundSystem;
        
        [Header("Stage Configuration")]
        [SerializeField] private int[] stageUnlockLevels = { 1, 10, 25, 50, 75, 100, 150, 200, 300, 500 };
        [SerializeField] private bool useProjectBasedStages = true;
        
        private GameModel _model;
        private int _lastStageIndex = 0;
        
        private void Awake()
        {
            InitializePresenter();
        }
        
        private void Start()
        {
            SetupConnections();
            InitializeBackgroundSystem();
        }
        
        private void InitializePresenter()
        {
            _model = GameModel.Instance;
            
            if (htmlBackgroundManager == null)
            {
                htmlBackgroundManager = FindObjectOfType<HTMLBackgroundManager>();
            }
            
            if (htmlBackgroundSystem == null)
            {
                htmlBackgroundSystem = FindObjectOfType<HTMLBackgroundSystem>();
            }
        }
        
        private void SetupConnections()
        {
            // Subscribe to game events that should trigger background changes
            GameEvents.OnStageUnlocked += OnStageUnlocked;
            GameEvents.OnLevelUp += OnLevelUp;
            GameEvents.OnProjectCompleted += OnProjectCompleted;
            GameEvents.OnFeatureUnlocked += OnFeatureUnlocked;
        }
        
        private void InitializeBackgroundSystem()
        {
            if (_model != null)
            {
                // Set initial stage based on current game progress
                int currentStage = CalculateCurrentStage();
                LoadStageBackground(currentStage);
            }
        }
        
        private int CalculateCurrentStage()
        {
            if (_model == null) return 0;
            
            if (useProjectBasedStages)
            {
                // Calculate stage based on projects completed or other game metrics
                // This is a placeholder - adapt to your game's progression system
                return Mathf.Min(_model.PlayerLevel / 10, stageUnlockLevels.Length - 1);
            }
            else
            {
                // Calculate stage based on level milestones
                for (int i = stageUnlockLevels.Length - 1; i >= 0; i--)
                {
                    if (_model.PlayerLevel >= stageUnlockLevels[i])
                    {
                        return i;
                    }
                }
                return 0;
            }
        }
        
        private void OnStageUnlocked(int stageId)
        {
            LoadStageBackground(stageId);
        }
        
        private void OnLevelUp(int newLevel)
        {
            int newStage = CalculateCurrentStage();
            if (newStage != _lastStageIndex)
            {
                LoadStageBackground(newStage);
            }
        }
        
        private void OnProjectCompleted(long reward)
        {
            if (useProjectBasedStages)
            {
                int newStage = CalculateCurrentStage();
                if (newStage != _lastStageIndex)
                {
                    LoadStageBackground(newStage);
                    
                    // Trigger notification for stage advancement
                    string stageName = GetStageName(newStage);
                    GameEvents.InvokeNotificationShown("New Environment!", $"Unlocked: {stageName}");
                }
            }
        }
        
        private void OnFeatureUnlocked(string featureName)
        {
            // Optionally trigger stage changes when specific features are unlocked
            switch (featureName.ToLower())
            {
                case "mobile development":
                    LoadStageBackground(1);
                    break;
                case "pc games":
                    LoadStageBackground(2);
                    break;
                case "vr development":
                    LoadStageBackground(3);
                    break;
                case "ai research":
                    LoadStageBackground(4);
                    break;
                // Add more feature-to-stage mappings as needed
            }
        }
        
        private void LoadStageBackground(int stageIndex)
        {
            if (htmlBackgroundManager != null)
            {
                htmlBackgroundManager.LoadStageHTML(stageIndex);
                _lastStageIndex = stageIndex;
                
                Debug.Log($"[HTMLBackgroundPresenter] Loaded stage {stageIndex}: {GetStageName(stageIndex)}");
            }
        }
        
        private string GetStageName(int stageIndex)
        {
            if (htmlBackgroundManager != null && 
                stageIndex >= 0 && 
                stageIndex < htmlBackgroundManager.stageData.Count)
            {
                return htmlBackgroundManager.stageData[stageIndex].stageName;
            }
            return $"Stage {stageIndex + 1}";
        }
        
        // Public methods for manual control (can be called from UI or other systems)
        public void ForceLoadStage(int stageIndex)
        {
            LoadStageBackground(stageIndex);
        }
        
        public void NextStage()
        {
            if (htmlBackgroundManager != null)
            {
                int nextStage = Mathf.Min(_lastStageIndex + 1, htmlBackgroundManager.stageData.Count - 1);
                LoadStageBackground(nextStage);
            }
        }
        
        public void PreviousStage()
        {
            int prevStage = Mathf.Max(_lastStageIndex - 1, 0);
            LoadStageBackground(prevStage);
        }
        
        public int GetCurrentStageIndex()
        {
            return _lastStageIndex;
        }
        
        public string GetCurrentStageName()
        {
            return GetStageName(_lastStageIndex);
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from events
            GameEvents.OnStageUnlocked -= OnStageUnlocked;
            GameEvents.OnLevelUp -= OnLevelUp;
            GameEvents.OnProjectCompleted -= OnProjectCompleted;
            GameEvents.OnFeatureUnlocked -= OnFeatureUnlocked;
        }
    }
}