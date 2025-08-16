using UnityEngine;
using GameDevClicker.Core.Patterns;
using GameDevClicker.Game.Managers;

namespace GameDevClicker.Game.Systems
{
    public class HTMLBackgroundSystem : MonoBehaviour
    {
        [Header("HTML Background Configuration")]
        [SerializeField] private HTMLBackgroundManager htmlManager;
        [SerializeField] private bool autoChangeWithStage = true;
        [SerializeField] private float stageTransitionDelay = 0.5f;
        
        private int _currentStage = 0;
        private bool _isTransitioning = false;
        
        private void Start()
        {
            InitializeSystem();
        }
        
        private void InitializeSystem()
        {
            if (htmlManager == null)
            {
                htmlManager = FindObjectOfType<HTMLBackgroundManager>();
                if (htmlManager == null)
                {
                    Debug.LogError("[HTMLBackgroundSystem] HTMLBackgroundManager not found in scene!");
                    return;
                }
            }
            
            // Subscribe to game events
            SubscribeToGameEvents();
            
            // Load initial stage
            LoadStageBackground(0);
        }
        
        private void SubscribeToGameEvents()
        {
            // Subscribe to existing game events
            GameEvents.OnStageUnlocked += OnStageUnlocked;
            GameEvents.OnProjectCompleted += OnProjectCompleted;
            GameEvents.OnLevelUp += OnLevelUp;
        }
        
        private void OnStageUnlocked(int stageId)
        {
            if (autoChangeWithStage)
            {
                ChangeToStage(stageId);
            }
        }
        
        private void OnProjectCompleted(long reward)
        {
            if (autoChangeWithStage)
            {
                AdvanceToNextStage();
            }
        }
        
        private void OnLevelUp(int newLevel)
        {
            // Optionally change background based on level milestones
            if (autoChangeWithStage && newLevel % 10 == 0) // Every 10 levels
            {
                int targetStage = Mathf.Min(newLevel / 10, htmlManager != null ? htmlManager.stageData.Count - 1 : 0);
                ChangeToStage(targetStage);
            }
        }
        
        public void ChangeToStage(int stageIndex)
        {
            if (_isTransitioning) return;
            
            _isTransitioning = true;
            _currentStage = stageIndex;
            
            // Add transition effect if desired
            StartCoroutine(TransitionToStage(stageIndex));
        }
        
        private System.Collections.IEnumerator TransitionToStage(int stageIndex)
        {
            // Optional: Add fade out effect
            yield return new WaitForSeconds(stageTransitionDelay * 0.5f);
            
            // Load new stage background
            LoadStageBackground(stageIndex);
            
            // Optional: Add fade in effect
            yield return new WaitForSeconds(stageTransitionDelay * 0.5f);
            
            _isTransitioning = false;
        }
        
        private void LoadStageBackground(int stageIndex)
        {
            if (htmlManager != null)
            {
                htmlManager.LoadStageHTML(stageIndex);
                Debug.Log($"[HTMLBackgroundSystem] Loaded stage {stageIndex} background");
            }
        }
        
        public void AdvanceToNextStage()
        {
            ChangeToStage(_currentStage + 1);
        }
        
        public void GoToPreviousStage()
        {
            if (_currentStage > 0)
            {
                ChangeToStage(_currentStage - 1);
            }
        }
        
        public void LoadSpecificStage(string stageName)
        {
            if (htmlManager != null)
            {
                htmlManager.LoadSpecificStage(stageName);
            }
        }
        
        // Public methods for manual control
        public void SetAutoChangeWithStage(bool autoChange)
        {
            autoChangeWithStage = autoChange;
        }
        
        public int GetCurrentStage()
        {
            return _currentStage;
        }
        
        public bool IsTransitioning()
        {
            return _isTransitioning;
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from events
            GameEvents.OnStageUnlocked -= OnStageUnlocked;
            GameEvents.OnProjectCompleted -= OnProjectCompleted;
            GameEvents.OnLevelUp -= OnLevelUp;
        }
    }
}