using System;
using UnityEngine;
using GameDevClicker.Core.Patterns;

namespace GameDevClicker.Core.Managers
{
    public class GameManager : Singleton<GameManager>
    {
        [Header("Game State")]
        [SerializeField] private GameState _currentState = GameState.MainMenu;
        [SerializeField] private bool _isPaused = false;
        [SerializeField] private float _gameSpeed = 1f;

        [Header("Game Settings")]
        [SerializeField] private bool _autoSaveEnabled = true;
        [SerializeField] private float _autoSaveInterval = 30f;

        private float _timeSinceLastSave = 0f;
        private float _sessionStartTime;
        private bool _isInitialized = false;

        public GameState CurrentState => _currentState;
        public bool IsPaused => _isPaused;
        public float GameSpeed => _gameSpeed;
        public bool IsInitialized => _isInitialized;
        public float SessionTime => Time.time - _sessionStartTime;

        public event Action<GameState> OnGameStateChanged;
        public event Action<bool> OnPauseStateChanged;
        public event Action OnGameInitialized;

        protected override void Awake()
        {
            base.Awake();
            InitializeGame();
        }

        private void InitializeGame()
        {
            _sessionStartTime = Time.time;
            Application.targetFrameRate = 60;
            
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            
            InitializeBalanceData();
            
            _isInitialized = true;
            OnGameInitialized?.Invoke();
            
            Debug.Log("[GameManager] Game initialized successfully");
        }
        
        private void InitializeBalanceData()
        {
            if (BalanceManager.Instance != null)
            {
                // BalanceManager auto-loads with autoLoadOnStart = true
                // No need to manually call LoadBalanceData() here
                Debug.Log("[GameManager] Balance data initialization started");
            }
        }

        private void Start()
        {
            if (SaveManager.Instance != null && SaveManager.Instance.HasSavedGame())
            {
                SaveManager.Instance.LoadGame();
                ChangeGameState(GameState.Playing);
            }
            else
            {
                ChangeGameState(GameState.MainMenu);
            }
        }

        private void Update()
        {
            if (_currentState != GameState.Playing || _isPaused)
                return;

            HandleAutoSave();
        }

        private void HandleAutoSave()
        {
            if (!_autoSaveEnabled)
                return;

            _timeSinceLastSave += Time.deltaTime;

            if (_timeSinceLastSave >= _autoSaveInterval)
            {
                if (SaveManager.Instance != null)
                {
                    SaveManager.Instance.SaveGame();
                }
                _timeSinceLastSave = 0f;
            }
        }

        public void ChangeGameState(GameState newState)
        {
            if (_currentState == newState)
                return;

            GameState previousState = _currentState;
            _currentState = newState;

            OnGameStateChanged?.Invoke(newState);

            Debug.Log($"[GameManager] Game state changed: {previousState} -> {newState}");

            HandleStateTransition(previousState, newState);
        }

        private void HandleStateTransition(GameState from, GameState to)
        {
            switch (to)
            {
                case GameState.MainMenu:
                    Time.timeScale = 1f;
                    break;
                    
                case GameState.Playing:
                    Time.timeScale = _gameSpeed;
                    break;
                    
                case GameState.GameOver:
                    Time.timeScale = 0f;
                    break;
            }
        }

        public void PauseGame()
        {
            if (_isPaused || _currentState != GameState.Playing)
                return;

            _isPaused = true;
            Time.timeScale = 0f;
            OnPauseStateChanged?.Invoke(true);
            
            Debug.Log("[GameManager] Game paused");
        }

        public void ResumeGame()
        {
            if (!_isPaused)
                return;

            _isPaused = false;
            Time.timeScale = _gameSpeed;
            OnPauseStateChanged?.Invoke(false);
            
            Debug.Log("[GameManager] Game resumed");
        }

        public void TogglePause()
        {
            if (_isPaused)
                ResumeGame();
            else
                PauseGame();
        }

        public void SetGameSpeed(float speed)
        {
            _gameSpeed = Mathf.Clamp(speed, 0.1f, 5f);
            
            if (_currentState == GameState.Playing && !_isPaused)
            {
                Time.timeScale = _gameSpeed;
            }
            
            Debug.Log($"[GameManager] Game speed set to: {_gameSpeed}x");
        }

        public void StartNewGame()
        {
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.DeleteSave();
            }
            ChangeGameState(GameState.Playing);
        }

        public void QuitGame()
        {
            if (_currentState == GameState.Playing && SaveManager.Instance != null)
            {
                SaveManager.Instance.SaveGame();
            }

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && _currentState == GameState.Playing && SaveManager.Instance != null)
            {
                SaveManager.Instance.SaveGame();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && _currentState == GameState.Playing && Application.isMobilePlatform && SaveManager.Instance != null)
            {
                SaveManager.Instance.SaveGame();
            }
        }

        protected override void OnDestroy()
        {
            if (_currentState == GameState.Playing && SaveManager.Instance != null)
            {
                SaveManager.Instance.SaveGame();
            }
            
            base.OnDestroy();
        }
    }

    public enum GameState
    {
        MainMenu,
        Playing,
        Paused,
        GameOver,
        Loading
    }
}