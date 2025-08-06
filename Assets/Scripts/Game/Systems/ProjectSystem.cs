using System;
using System.Collections.Generic;
using UnityEngine;
using GameDevClicker.Core.Patterns;
using GameDevClicker.Core.Managers;
using GameDevClicker.Game.Models;
using GameDevClicker.Core.Utilities;

namespace GameDevClicker.Game.Systems
{
    public class ProjectSystem : Singleton<ProjectSystem>
    {
        [Header("Project Configuration")]
        [SerializeField] private float baseExpRequiredForProject = 1000f;
        [SerializeField] private float projectRequirementMultiplier = 1.5f;
        [SerializeField] private float baseProjectReward = 500f;
        [SerializeField] private float projectRewardMultiplier = 1.3f;
        
        [Header("Project Types")]
        [SerializeField] private ProjectTypeData[] projectTypes;
        
        private float _currentProjectProgress;
        private float _currentProjectRequirement;
        private ProjectTypeData _currentProjectType;
        private int _completedProjectsCount;
        private bool _isUnlocked;

        public float CurrentProgress => _currentProjectProgress;
        public float CurrentRequirement => _currentProjectRequirement;
        public float ProgressPercentage => _currentProjectRequirement > 0 ? _currentProjectProgress / _currentProjectRequirement : 0f;
        public ProjectTypeData CurrentProjectType => _currentProjectType;
        public int CompletedProjectsCount => _completedProjectsCount;
        public bool IsUnlocked => _isUnlocked;

        public event Action<ProjectTypeData> OnProjectStarted;
        public event Action<long, ProjectTypeData> OnProjectCompleted;
        public event Action<float> OnProjectProgressUpdated;

        protected override void Awake()
        {
            base.Awake();
            InitializeProjectSystem();
        }

        private void Start()
        {
            SubscribeToEvents();
            LoadProjectData();
        }

        private void InitializeProjectSystem()
        {
            if (projectTypes == null || projectTypes.Length == 0)
            {
                CreateDefaultProjectTypes();
            }

            _currentProjectRequirement = baseExpRequiredForProject;
            _currentProjectProgress = 0f;
            _completedProjectsCount = 0;
            _isUnlocked = false;

            Debug.Log("[ProjectSystem] Initialized with default project requirement: " + _currentProjectRequirement);
        }

        private void CreateDefaultProjectTypes()
        {
            projectTypes = new ProjectTypeData[]
            {
                new ProjectTypeData
                {
                    projectName = "Simple Mobile Game",
                    description = "A basic tap-to-play mobile game",
                    icon = "📱",
                    difficulty = ProjectDifficulty.Easy,
                    baseRewardMultiplier = 1f,
                    stageRequirement = 1
                },
                new ProjectTypeData
                {
                    projectName = "Indie Platformer",
                    description = "A 2D platformer with retro graphics",
                    icon = "🎮",
                    difficulty = ProjectDifficulty.Medium,
                    baseRewardMultiplier = 1.5f,
                    stageRequirement = 2
                },
                new ProjectTypeData
                {
                    projectName = "VR Experience",
                    description = "An immersive virtual reality application",
                    icon = "🥽",
                    difficulty = ProjectDifficulty.Hard,
                    baseRewardMultiplier = 2.5f,
                    stageRequirement = 4
                },
                new ProjectTypeData
                {
                    projectName = "AI Game Assistant",
                    description = "An AI-powered game companion",
                    icon = "🤖",
                    difficulty = ProjectDifficulty.Expert,
                    baseRewardMultiplier = 4f,
                    stageRequirement = 5
                }
            };
        }

        private void SubscribeToEvents()
        {
            GameEvents.OnExperienceChanged += OnExperienceGained;
            GameEvents.OnFeatureUnlocked += OnFeatureUnlocked;
            GameEvents.OnStageUnlocked += OnStageUnlocked;
            GameEvents.OnGameLoaded += LoadProjectData;
        }

        private void LoadProjectData()
        {
            var gameData = SaveManager.Instance.CurrentGameData;
            if (gameData != null)
            {
                _completedProjectsCount = (int)gameData.totalProjectsCompleted;
                _currentProjectRequirement = baseExpRequiredForProject * 
                    Mathf.Pow(projectRequirementMultiplier, _completedProjectsCount);
            }

            StartNewProject();
        }

        private void SaveProjectData()
        {
            var gameData = SaveManager.Instance.CurrentGameData;
            if (gameData != null)
            {
                gameData.totalProjectsCompleted = _completedProjectsCount;
                SaveManager.Instance.MarkDirty();
            }
        }

        #region Project Management

        public void StartNewProject()
        {
            if (!_isUnlocked) return;

            _currentProjectType = SelectProjectType();
            _currentProjectProgress = 0f;
            
            // Adjust requirement based on project difficulty
            float difficultyMultiplier = GetDifficultyMultiplier(_currentProjectType.difficulty);
            _currentProjectRequirement = baseExpRequiredForProject * 
                Mathf.Pow(projectRequirementMultiplier, _completedProjectsCount) * 
                difficultyMultiplier;

            OnProjectStarted?.Invoke(_currentProjectType);
            GameEvents.InvokeNotificationShown(
                "New Project Started!",
                $"Working on: {_currentProjectType.projectName}"
            );

            Debug.Log($"[ProjectSystem] Started project: {_currentProjectType.projectName} (Requirement: {_currentProjectRequirement})");
        }

        public void AddProgress(float experience)
        {
            if (!_isUnlocked || _currentProjectType == null) return;

            float oldProgress = _currentProjectProgress;
            _currentProjectProgress += experience;

            // Clamp progress to requirement
            _currentProjectProgress = Mathf.Min(_currentProjectProgress, _currentProjectRequirement);

            // Update progress events
            if (_currentProjectProgress != oldProgress)
            {
                OnProjectProgressUpdated?.Invoke(_currentProjectProgress);
                GameEvents.InvokeProjectProgressChanged(ProgressPercentage);
            }

            // Check for completion
            if (_currentProjectProgress >= _currentProjectRequirement)
            {
                CompleteProject();
            }
        }

        private void CompleteProject()
        {
            if (_currentProjectType == null) return;

            long reward = CalculateProjectReward();
            _completedProjectsCount++;

            // Award the money
            GameModel.Instance.AddMoney(reward);

            // Trigger completion events
            OnProjectCompleted?.Invoke(reward, _currentProjectType);
            GameEvents.InvokeProjectCompleted(reward);

            SaveProjectData();

            Debug.Log($"[ProjectSystem] Completed project: {_currentProjectType.projectName} (Reward: {reward})");

            // Start next project automatically
            StartNewProject();
        }

        #endregion

        #region Project Selection & Rewards

        private ProjectTypeData SelectProjectType()
        {
            var gameData = SaveManager.Instance.CurrentGameData;
            if (gameData == null) return projectTypes[0];

            // Filter projects by stage requirement
            var availableProjects = new List<ProjectTypeData>();
            foreach (var project in projectTypes)
            {
                if (gameData.currentStage >= project.stageRequirement)
                {
                    availableProjects.Add(project);
                }
            }

            if (availableProjects.Count == 0)
                return projectTypes[0];

            // Weighted random selection based on difficulty (easier projects more likely)
            float totalWeight = 0f;
            foreach (var project in availableProjects)
            {
                totalWeight += GetProjectWeight(project.difficulty);
            }

            float randomValue = UnityEngine.Random.Range(0f, totalWeight);
            float currentWeight = 0f;

            foreach (var project in availableProjects)
            {
                currentWeight += GetProjectWeight(project.difficulty);
                if (randomValue <= currentWeight)
                {
                    return project;
                }
            }

            return availableProjects[availableProjects.Count - 1];
        }

        private float GetProjectWeight(ProjectDifficulty difficulty)
        {
            switch (difficulty)
            {
                case ProjectDifficulty.Easy: return 4f;
                case ProjectDifficulty.Medium: return 3f;
                case ProjectDifficulty.Hard: return 2f;
                case ProjectDifficulty.Expert: return 1f;
                default: return 1f;
            }
        }

        private float GetDifficultyMultiplier(ProjectDifficulty difficulty)
        {
            switch (difficulty)
            {
                case ProjectDifficulty.Easy: return 1f;
                case ProjectDifficulty.Medium: return 1.3f;
                case ProjectDifficulty.Hard: return 1.8f;
                case ProjectDifficulty.Expert: return 2.5f;
                default: return 1f;
            }
        }

        public long CalculateProjectReward()
        {
            if (_currentProjectType == null) return 0;

            float baseReward = baseProjectReward * 
                Mathf.Pow(projectRewardMultiplier, _completedProjectsCount);

            float finalReward = baseReward * _currentProjectType.baseRewardMultiplier;

            // Apply difficulty bonus
            finalReward *= GetDifficultyMultiplier(_currentProjectType.difficulty);

            return (long)finalReward;
        }
        
        public void CompleteProjectOffline()
        {
            // Used by offline progression system
            _completedProjectsCount++;
            _currentProjectRequirement *= projectRequirementMultiplier;
            _currentProjectProgress = 0f;
            
            // Select next project
            _currentProjectType = SelectProjectType();
            
            SaveProjectData();
        }

        #endregion

        #region Event Handlers

        private void OnExperienceGained(long newExperienceTotal)
        {
            // We don't add progress directly from total experience changes
            // Progress is added through specific experience gains (e.g., clicking)
        }

        private void OnFeatureUnlocked(string featureName)
        {
            if (featureName == "project_system" && !_isUnlocked)
            {
                _isUnlocked = true;
                StartNewProject();
                Debug.Log("[ProjectSystem] Project system unlocked!");
            }
        }

        private void OnStageUnlocked(int stage)
        {
            // When a new stage is unlocked, we might have access to new project types
            if (_isUnlocked && _currentProjectType != null)
            {
                // Check if we should consider upgrading to a higher difficulty project
                var newProjectType = SelectProjectType();
                if (newProjectType != _currentProjectType && 
                    newProjectType.difficulty > _currentProjectType.difficulty)
                {
                    // Optionally restart with a more challenging project
                    // For now, we'll keep the current project and let the next one be upgraded
                }
            }
        }

        #endregion

        #region UI Integration Methods

        public string GetProgressText()
        {
            return $"{NumberFormatter.Format((long)_currentProjectProgress)} / {NumberFormatter.Format((long)_currentProjectRequirement)}";
        }

        public string GetProgressBarText()
        {
            return $"{(ProgressPercentage * 100f):F1}%";
        }

        public string GetCurrentProjectName()
        {
            return _currentProjectType?.projectName ?? "No Project";
        }

        public string GetCurrentProjectDescription()
        {
            return _currentProjectType?.description ?? "";
        }

        public string GetNextRewardText()
        {
            long nextReward = CalculateProjectReward();
            return NumberFormatter.FormatCurrency(nextReward);
        }

        public float GetTimeToCompletion()
        {
            if (!_isUnlocked || _currentProjectRequirement <= 0) return 0f;
            
            float remainingExp = _currentProjectRequirement - _currentProjectProgress;
            float expPerSecond = GameModel.Instance.ExpPerClick + GameModel.Instance.AutoExp;
            
            if (expPerSecond <= 0) return float.MaxValue;
            
            return remainingExp / expPerSecond;
        }

        public string GetTimeToCompletionText()
        {
            float time = GetTimeToCompletion();
            if (time == float.MaxValue) return "∞";
            return NumberFormatter.FormatTime(time);
        }

        #endregion

        #region Debug Methods

        public void DebugCompleteCurrentProject()
        {
            if (_currentProjectType != null)
            {
                _currentProjectProgress = _currentProjectRequirement;
                CompleteProject();
            }
        }

        public void DebugAddProgress(float amount)
        {
            AddProgress(amount);
        }

        #endregion

        protected override void OnDestroy()
        {
            GameEvents.OnExperienceChanged -= OnExperienceGained;
            GameEvents.OnFeatureUnlocked -= OnFeatureUnlocked;
            GameEvents.OnStageUnlocked -= OnStageUnlocked;
            GameEvents.OnGameLoaded -= LoadProjectData;
            base.OnDestroy();
        }
    }

    [System.Serializable]
    public class ProjectTypeData
    {
        [Header("Basic Information")]
        public string projectName;
        [TextArea(2, 3)]
        public string description;
        public string icon;

        [Header("Difficulty & Requirements")]
        public ProjectDifficulty difficulty;
        public int stageRequirement = 1;

        [Header("Rewards")]
        public float baseRewardMultiplier = 1f;
    }

    public enum ProjectDifficulty
    {
        Easy,
        Medium,
        Hard,
        Expert
    }
}