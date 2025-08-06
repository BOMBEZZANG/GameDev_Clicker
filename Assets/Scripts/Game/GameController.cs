using UnityEngine;
using GameDevClicker.Core.Managers;
using GameDevClicker.Core.Patterns;
using GameDevClicker.Core.Utilities;
using GameDevClicker.Game.Models;
using GameDevClicker.Game.Views;
using GameDevClicker.Game.Presenters;
using GameDevClicker.Game.Systems;

namespace GameDevClicker.Game
{
    public class GameController : MonoBehaviour
    {
        [Header("System References")]
        [SerializeField] private GameViewUI gameViewUI;
        [SerializeField] private GamePresenter gamePresenter;

        [Header("Auto-Setup")]
        [SerializeField] private bool autoFindComponents = true;
        [SerializeField] private bool initializeOnStart = true;

        private bool _isInitialized = false;

        private void Awake()
        {
            if (autoFindComponents)
            {
                AutoFindComponents();
            }
        }

        private void Start()
        {
            if (initializeOnStart)
            {
                InitializeGame();
            }
        }

        private void AutoFindComponents()
        {
            if (gameViewUI == null)
                gameViewUI = FindObjectOfType<GameViewUI>();

            if (gamePresenter == null)
                gamePresenter = FindObjectOfType<GamePresenter>();
        }

        public void InitializeGame()
        {
            if (_isInitialized)
            {
                Debug.LogWarning("[GameController] Game already initialized!");
                return;
            }

            Debug.Log("[GameController] Initializing Game Dev Clicker...");

            // Wait for core systems to initialize
            if (!WaitForCoreSystemsReady())
            {
                Debug.LogError("[GameController] Core systems not ready!");
                return;
            }

            // Initialize UI systems
            InitializeUISystem();

            // Connect systems
            ConnectSystems();

            // Start game loop
            StartGameLoop();

            _isInitialized = true;
            Debug.Log("[GameController] Game Dev Clicker initialized successfully!");
        }

        private bool WaitForCoreSystemsReady()
        {
            // Check if all required singletons are ready
            bool gameManagerReady = GameManager.Instance != null && GameManager.Instance.IsInitialized;
            bool gameModelReady = GameModel.Instance != null;
            bool saveManagerReady = SaveManager.Instance != null;
            bool upgradeManagerReady = UpgradeManager.Instance != null;
            bool unlockManagerReady = UnlockManager.Instance != null;

            if (!gameManagerReady)
            {
                Debug.LogError("[GameController] GameManager not ready!");
                return false;
            }

            if (!gameModelReady)
            {
                Debug.LogError("[GameController] GameModel not ready!");
                return false;
            }

            if (!saveManagerReady)
            {
                Debug.LogError("[GameController] SaveManager not ready!");
                return false;
            }

            if (!upgradeManagerReady)
            {
                Debug.LogError("[GameController] UpgradeManager not ready!");
                return false;
            }

            if (!unlockManagerReady)
            {
                Debug.LogError("[GameController] UnlockManager not ready!");
                return false;
            }
            
            // Initialize additional systems
            InitializeGameSystems();

            return true;
        }
        
        private void InitializeGameSystems()
        {
            // Initialize Statistics System
            if (StatisticsSystem.Instance != null)
            {
                Debug.Log("[GameController] Statistics System initialized");
            }
            
            // Initialize Achievement System
            if (AchievementSystem.Instance != null)
            {
                Debug.Log("[GameController] Achievement System initialized");
            }
            
            // Initialize Offline Progression System
            if (OfflineProgressionSystem.Instance != null)
            {
                Debug.Log("[GameController] Offline Progression System initialized");
            }
        }

        private void InitializeUISystem()
        {
            // Initialize UI components
            if (gameViewUI == null)
            {
                Debug.LogError("[GameController] GameViewUI not found!");
                return;
            }

            if (gamePresenter == null)
            {
                Debug.LogError("[GameController] GamePresenter not found!");
                return;
            }

            // Click effects are handled directly in GameViewUI

            Debug.Log("[GameController] UI System initialized");
        }

        private void ConnectSystems()
        {
            // Project system integration
            if (ProjectSystem.Instance != null)
            {
                // Connect project progress to experience gains
                GameEvents.OnClickPerformed += (moneyGained, expGained) => 
                {
                    if (ProjectSystem.Instance.IsUnlocked)
                    {
                        ProjectSystem.Instance.AddProgress(expGained);
                    }
                };

                GameEvents.OnExperienceChanged += (experience) =>
                {
                    // Auto income exp also contributes to projects
                    if (ProjectSystem.Instance.IsUnlocked)
                    {
                        float autoExp = GameModel.Instance.AutoExp;
                        if (autoExp > 0)
                        {
                            ProjectSystem.Instance.AddProgress(autoExp * Time.deltaTime);
                        }
                    }
                };
            }

            // Unlock system integration
            if (UnlockManager.Instance != null)
            {
                // Ensure unlock checks are performed when needed
                GameEvents.OnLevelUp += (level) => UnlockManager.Instance.CheckAllUnlocks();
                GameEvents.OnStageUnlocked += (stage) => UnlockManager.Instance.CheckAllUnlocks();
            }

            Debug.Log("[GameController] Systems connected");
        }

        private void StartGameLoop()
        {
            // Load existing save data or create new game
            if (SaveManager.Instance.HasSavedGame())
            {
                SaveManager.Instance.LoadGame();
                Debug.Log("[GameController] Loaded existing save");
            }
            else
            {
                Debug.Log("[GameController] Starting new game");
            }

            // Start the game
            if (GameManager.Instance.CurrentState != GameState.Playing)
            {
                GameManager.Instance.ChangeGameState(GameState.Playing);
            }

            // Refresh UI to show current state
            if (gamePresenter != null)
            {
                gamePresenter.RefreshUI();
            }

            Debug.Log("[GameController] Game loop started");
        }

        #region Public API

        public void SaveGame()
        {
            SaveManager.Instance?.SaveGame();
        }

        public void LoadGame()
        {
            SaveManager.Instance?.LoadGame();
            gamePresenter?.RefreshUI();
        }

        public void ResetGame()
        {
            SaveManager.Instance?.DeleteSave();
            
            // Restart the scene or reinitialize
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
            );
        }

        public void PauseGame()
        {
            GameManager.Instance?.PauseGame();
        }

        public void ResumeGame()
        {
            GameManager.Instance?.ResumeGame();
        }

        public void SetGameSpeed(float speed)
        {
            GameManager.Instance?.SetGameSpeed(speed);
        }

        #endregion

        #region Debug Methods

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void DebugAddMoney(long amount)
        {
            GameModel.Instance?.AddMoney(amount);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void DebugAddExperience(long amount)
        {
            GameModel.Instance?.AddExperience(amount);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void DebugUnlockFeature(string featureName)
        {
            UnlockManager.Instance?.DebugUnlockFeature(featureName);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void DebugCompleteProject()
        {
            ProjectSystem.Instance?.DebugCompleteCurrentProject();
        }

        #endregion

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                SaveGame();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && Application.isMobilePlatform)
            {
                SaveGame();
            }
        }

        private void OnDestroy()
        {
            SaveGame();
        }

        #region Editor Support

#if UNITY_EDITOR
        [Header("Editor Tools")]
        [SerializeField] private bool showDebugInfo = true;

        private void OnGUI()
        {
            if (!showDebugInfo || !Application.isPlaying) return;

            GUILayout.BeginArea(new Rect(10, 10, 300, 400));
            GUILayout.BeginVertical("box");
            
            GUILayout.Label("Game Dev Clicker Debug", EditorStyles.boldLabel);
            
            if (!_isInitialized)
            {
                GUILayout.Label("Status: Not Initialized", EditorStyles.helpBox);
                if (GUILayout.Button("Initialize"))
                {
                    InitializeGame();
                }
            }
            else
            {
                GUILayout.Label("Status: Running", EditorStyles.helpBox);
                
                if (GameModel.Instance != null)
                {
                    GUILayout.Label($"Money: {NumberFormatter.FormatCurrency(GameModel.Instance.Money)}");
                    GUILayout.Label($"Experience: {NumberFormatter.Format(GameModel.Instance.Experience)}");
                    GUILayout.Label($"Level: {GameModel.Instance.PlayerLevel}");
                    GUILayout.Label($"Stage: {GameModel.Instance.CurrentStage}");
                }

                GUILayout.Space(10);
                
                if (GUILayout.Button("Add 1000 Money"))
                    DebugAddMoney(1000);
                
                if (GUILayout.Button("Add 1000 EXP"))
                    DebugAddExperience(1000);
                
                if (GUILayout.Button("Unlock Money"))
                    DebugUnlockFeature("money");
                
                if (GUILayout.Button("Complete Project"))
                    DebugCompleteProject();

                GUILayout.Space(10);
                
                if (GUILayout.Button("Save Game"))
                    SaveGame();
                
                if (GUILayout.Button("Reset Game"))
                    ResetGame();
            }
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private static class EditorStyles
        {
            public static GUIStyle boldLabel = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
            public static GUIStyle helpBox = new GUIStyle(GUI.skin.box) { padding = new RectOffset(5, 5, 5, 5) };
        }
#endif

        #endregion
    }
}