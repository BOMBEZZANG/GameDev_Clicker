using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GameDevClicker.Core.Managers;
using GameDevClicker.Game.Systems;
using GameDevClicker.Data;
using TMPro;

namespace GameDevClicker.Game
{
    public class BalanceTestController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI playerDataText;
        [SerializeField] private TextMeshProUGUI upgradesText;
        [SerializeField] private TextMeshProUGUI projectsText;
        [SerializeField] private Button loadDataButton;
        [SerializeField] private Button testClickButton;
        [SerializeField] private Button autoIncomeButton;
        
        [Header("Test Settings")]
        [SerializeField] private int testPlayerLevel = 1;
        [SerializeField] private long testPlayerExp = 0;
        [SerializeField] private long testPlayerMoney = 1000;
        [SerializeField] private int testStage = 1;
        
        [Header("Simulation")]
        [SerializeField] private bool runSimulation = false;
        [SerializeField] private float simulationSpeed = 1f;
        
        private BalanceManager _balanceManager;
        private BalanceIntegrationSystem _integrationSystem;
        private CSVLoader _csvLoader;
        
        private float _autoIncomeTimer = 0f;
        private bool _autoIncomeActive = false;
        private bool _isInitialized = false;
        
        private void Awake()
        {
            // Prevent destruction during scene changes if needed
            Debug.Log("[BalanceTestController] Awake called");
        }
        
        private void Start()
        {
            Debug.Log("[BalanceTestController] Start called");
            StartCoroutine(DelayedInitialization());
        }
        
        private IEnumerator DelayedInitialization()
        {
            // Wait a frame to ensure all singletons are initialized
            yield return null;
            
            InitializeTestController();
            SetupUI();
            
            // Auto-load data after initialization
            yield return new WaitForSeconds(0.5f);
            LoadAndTestData();
        }
        
        private void InitializeTestController()
        {
            Debug.Log("[BalanceTestController] Initializing test controller");
            
            // Get or create CSVLoader
            _csvLoader = CSVLoader.Instance;
            if (_csvLoader == null)
            {
                Debug.LogError("[BalanceTestController] Failed to get CSVLoader instance!");
                UpdateStatus("ERROR: Failed to initialize CSVLoader!");
                return;
            }
            
            // Get or create BalanceManager
            _balanceManager = BalanceManager.Instance;
            if (_balanceManager == null)
            {
                Debug.LogError("[BalanceTestController] Failed to get BalanceManager instance!");
                UpdateStatus("ERROR: Failed to initialize BalanceManager!");
                return;
            }
            
            // Get or add BalanceIntegrationSystem
            _integrationSystem = GetComponent<BalanceIntegrationSystem>();
            if (_integrationSystem == null)
            {
                Debug.Log("[BalanceTestController] Adding BalanceIntegrationSystem component");
                _integrationSystem = gameObject.AddComponent<BalanceIntegrationSystem>();
            }
            
            // Subscribe to events
            if (_integrationSystem != null)
            {
                _integrationSystem.OnUpgradePurchased += OnUpgradePurchased;
                _integrationSystem.OnLevelUp += OnLevelUp;
                _integrationSystem.OnStageUnlocked += OnStageUnlocked;
                _integrationSystem.OnProjectCompleted += OnProjectCompleted;
            }
            
            _isInitialized = true;
            Debug.Log("[BalanceTestController] Initialization complete");
        }
        
        private void SetupUI()
        {
            Debug.Log("[BalanceTestController] Setting up UI");
            
            if (loadDataButton != null)
            {
                loadDataButton.onClick.RemoveAllListeners();
                loadDataButton.onClick.AddListener(LoadAndTestData);
                var buttonText = loadDataButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText == null)
                {
                    var regularText = loadDataButton.GetComponentInChildren<Text>();
                    if (regularText != null)
                    {
                        regularText.text = "Load CSV Data";
                    }
                }
                else
                {
                    buttonText.text = "Load CSV Data";
                }
            }
            else
            {
                Debug.LogWarning("[BalanceTestController] Load Data Button is not assigned!");
            }
            
            if (testClickButton != null)
            {
                testClickButton.onClick.RemoveAllListeners();
                testClickButton.onClick.AddListener(SimulateClick);
                var buttonText = testClickButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText == null)
                {
                    var regularText = testClickButton.GetComponentInChildren<Text>();
                    if (regularText != null)
                    {
                        regularText.text = "Test Click";
                    }
                }
                else
                {
                    buttonText.text = "Test Click";
                }
            }
            else
            {
                Debug.LogWarning("[BalanceTestController] Test Click Button is not assigned!");
            }
            
            if (autoIncomeButton != null)
            {
                autoIncomeButton.onClick.RemoveAllListeners();
                autoIncomeButton.onClick.AddListener(ToggleAutoIncome);
                var buttonText = autoIncomeButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText == null)
                {
                    var regularText = autoIncomeButton.GetComponentInChildren<Text>();
                    if (regularText != null)
                    {
                        regularText.text = "Start Auto";
                    }
                }
                else
                {
                    buttonText.text = "Start Auto";
                }
            }
            else
            {
                Debug.LogWarning("[BalanceTestController] Auto Income Button is not assigned!");
            }
            
            UpdateStatus("UI Setup Complete - Ready to load data");
        }
        
        private void LoadAndTestData()
        {
            if (!_isInitialized)
            {
                UpdateStatus("System not initialized yet!");
                return;
            }
            
            StartCoroutine(LoadDataCoroutine());
        }
        
        private IEnumerator LoadDataCoroutine()
        {
            UpdateStatus("Loading CSV data...");
            Debug.Log("[BalanceTestController] Starting CSV data load");
            
            // Ensure managers exist
            if (_balanceManager == null)
            {
                _balanceManager = BalanceManager.Instance;
                if (_balanceManager == null)
                {
                    UpdateStatus("ERROR: BalanceManager not found!");
                    yield break;
                }
            }
            
            // Load the CSV data
            _balanceManager.LoadBalanceData();
            
            // Wait for loading to complete
            yield return new WaitForSeconds(0.5f);
            
            if (_balanceManager.IsDataLoaded)
            {
                UpdateStatus("Data loaded successfully!");
                Debug.Log("[BalanceTestController] CSV data loaded successfully");
                
                // Initialize the integration system with test data
                if (_integrationSystem != null)
                {
                    _integrationSystem.SetPlayerData(testPlayerLevel, testPlayerExp, testPlayerMoney, testStage);
                }
                
                DisplayLoadedData();
                UpdatePlayerDisplay();
            }
            else
            {
                UpdateStatus("Failed to load data! Check console for errors.");
                Debug.LogError("[BalanceTestController] Failed to load CSV data");
            }
        }
        
        private void DisplayLoadedData()
        {
            if (!_balanceManager.IsDataLoaded) 
            {
                UpdateStatus("No data loaded yet!");
                return;
            }
            
            var balanceData = _balanceManager.CurrentBalanceData;
            
            if (balanceData == null)
            {
                UpdateStatus("Balance data is null!");
                return;
            }
            
            string info = $"Loaded Data Summary:\n";
            info += $"Upgrades: {balanceData.Upgrades?.Count ?? 0}\n";
            info += $"Levels: {balanceData.Levels?.Count ?? 0}\n";
            info += $"Projects: {balanceData.Projects?.Count ?? 0}\n";
            info += $"Stages: {balanceData.Stages?.Count ?? 0}\n";
            
            UpdateStatus(info);
            
            DisplayAvailableUpgrades();
            DisplayAvailableProjects();
        }
        
        private void DisplayAvailableUpgrades()
        {
            if (upgradesText == null) 
            {
                Debug.LogWarning("[BalanceTestController] Upgrades Text is not assigned!");
                return;
            }
            
            if (_integrationSystem == null)
            {
                upgradesText.text = "Integration System not initialized";
                return;
            }
            
            var availableUpgrades = _integrationSystem.GetAvailableUpgrades();
            
            string upgradeInfo = "Available Upgrades:\n";
            int displayCount = 0;
            
            foreach (var upgrade in availableUpgrades)
            {
                if (displayCount >= 5) break;
                
                string name = _balanceManager.GetLocalizedUpgradeName(upgrade.upgradeId);
                string desc = _balanceManager.GetLocalizedUpgradeDescription(upgrade.upgradeId);
                float price = _balanceManager.CalculateUpgradePrice(upgrade.upgradeId, 0);
                
                upgradeInfo += $"• {name}: {desc}\n";
                upgradeInfo += $"  Price: {FormatNumber(price)} {upgrade.currencyType}\n";
                
                displayCount++;
            }
            
            if (availableUpgrades.Count > 5)
            {
                upgradeInfo += $"... and {availableUpgrades.Count - 5} more\n";
            }
            
            if (availableUpgrades.Count == 0)
            {
                upgradeInfo += "No upgrades available\n";
            }
            
            upgradesText.text = upgradeInfo;
        }
        
        private void DisplayAvailableProjects()
        {
            if (projectsText == null) 
            {
                Debug.LogWarning("[BalanceTestController] Projects Text is not assigned!");
                return;
            }
            
            if (_integrationSystem == null)
            {
                projectsText.text = "Integration System not initialized";
                return;
            }
            
            var availableProjects = _integrationSystem.GetAvailableProjects();
            
            string projectInfo = "Available Projects:\n";
            
            foreach (var project in availableProjects)
            {
                string name = _balanceManager.GetLocalizedProjectName(project.projectId);
                projectInfo += $"• {name}\n";
                projectInfo += $"  Reward: {FormatNumber(project.baseReward)} | Time: {project.completionTime}s\n";
            }
            
            if (availableProjects.Count == 0)
            {
                projectInfo += "No projects available at current level\n";
            }
            
            projectsText.text = projectInfo;
        }
        
        private void UpdatePlayerDisplay()
        {
            if (playerDataText == null) 
            {
                Debug.LogWarning("[BalanceTestController] Player Data Text is not assigned!");
                return;
            }
            
            if (_integrationSystem == null)
            {
                playerDataText.text = "Integration System not initialized";
                return;
            }
            
            string playerInfo = "Player Stats:\n";
            playerInfo += $"Level: {testPlayerLevel}\n";
            playerInfo += $"Experience: {FormatNumber(testPlayerExp)}\n";
            playerInfo += $"Money: {FormatNumber(testPlayerMoney)}\n";
            playerInfo += $"Stage: {testStage}\n\n";
            
            playerInfo += "Calculated Values:\n";
            playerInfo += $"Click Value: {_integrationSystem.CalculateClickValue():F1} EXP\n";
            playerInfo += $"Money/Click: {_integrationSystem.CalculateMoneyPerClick():F1}\n";
            playerInfo += $"Auto Income: {_integrationSystem.CalculateAutoIncome():F1}/s\n";
            playerInfo += $"Auto EXP: {_integrationSystem.CalculateAutoExp():F1}/s\n";
            
            playerDataText.text = playerInfo;
        }
        
        private void SimulateClick()
        {
            if (!_isInitialized || _integrationSystem == null)
            {
                UpdateStatus("System not initialized!");
                return;
            }
            
            float expGain = _integrationSystem.CalculateClickValue();
            float moneyGain = _integrationSystem.CalculateMoneyPerClick();
            
            testPlayerExp += (long)expGain;
            testPlayerMoney += (long)moneyGain;
            
            _integrationSystem.AddExperience((long)expGain);
            _integrationSystem.AddMoney((long)moneyGain);
            
            UpdatePlayerDisplay();
            
            UpdateStatus($"Click! +{expGain:F1} EXP, +{moneyGain:F1} Money");
        }
        
        private void ToggleAutoIncome()
        {
            if (!_isInitialized)
            {
                UpdateStatus("System not initialized!");
                return;
            }
            
            _autoIncomeActive = !_autoIncomeActive;
            
            if (autoIncomeButton != null)
            {
                var buttonText = autoIncomeButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = _autoIncomeActive ? "Stop Auto" : "Start Auto";
                }
                else
                {
                    var regularText = autoIncomeButton.GetComponentInChildren<Text>();
                    if (regularText != null)
                    {
                        regularText.text = _autoIncomeActive ? "Stop Auto" : "Start Auto";
                    }
                }
            }
            
            UpdateStatus(_autoIncomeActive ? "Auto income started" : "Auto income stopped");
        }
        
        private void Update()
        {
            if (!_isInitialized) return;
            
            if (_autoIncomeActive && _integrationSystem != null)
            {
                _autoIncomeTimer += Time.deltaTime * simulationSpeed;
                
                if (_autoIncomeTimer >= 1f)
                {
                    _autoIncomeTimer -= 1f;
                    
                    float autoMoney = _integrationSystem.CalculateAutoIncome();
                    float autoExp = _integrationSystem.CalculateAutoExp();
                    
                    if (autoMoney > 0)
                    {
                        testPlayerMoney += (long)autoMoney;
                        _integrationSystem.AddMoney((long)autoMoney);
                    }
                    
                    if (autoExp > 0)
                    {
                        testPlayerExp += (long)autoExp;
                        _integrationSystem.AddExperience((long)autoExp);
                    }
                    
                    UpdatePlayerDisplay();
                }
            }
            
            if (runSimulation)
            {
                RunBalanceSimulation();
            }
        }
        
        private void RunBalanceSimulation()
        {
            if (Input.GetKeyDown(KeyCode.U))
            {
                TryPurchaseRandomUpgrade();
            }
            
            if (Input.GetKeyDown(KeyCode.P))
            {
                TryStartRandomProject();
            }
            
            if (Input.GetKeyDown(KeyCode.R))
            {
                ResetTestData();
            }
        }
        
        private void TryPurchaseRandomUpgrade()
        {
            if (_integrationSystem == null) return;
            
            var availableUpgrades = _integrationSystem.GetAvailableUpgrades();
            if (availableUpgrades.Count > 0)
            {
                var randomUpgrade = availableUpgrades[Random.Range(0, availableUpgrades.Count)];
                
                if (_integrationSystem.TryPurchaseUpgrade(randomUpgrade.upgradeId))
                {
                    UpdateStatus($"Purchased: {randomUpgrade.nameEn}");
                    DisplayAvailableUpgrades();
                    UpdatePlayerDisplay();
                }
                else
                {
                    UpdateStatus($"Cannot afford: {randomUpgrade.nameEn}");
                }
            }
        }
        
        private void TryStartRandomProject()
        {
            if (_integrationSystem == null) return;
            
            var availableProjects = _integrationSystem.GetAvailableProjects();
            if (availableProjects.Count > 0)
            {
                var randomProject = availableProjects[Random.Range(0, availableProjects.Count)];
                
                if (_integrationSystem.TryStartProject(randomProject.projectId))
                {
                    UpdateStatus($"Started project: {randomProject.nameEn}");
                }
            }
        }
        
        private void ResetTestData()
        {
            testPlayerLevel = 1;
            testPlayerExp = 0;
            testPlayerMoney = 1000;
            testStage = 1;
            
            if (_integrationSystem != null)
            {
                _integrationSystem.SetPlayerData(testPlayerLevel, testPlayerExp, testPlayerMoney, testStage);
            }
            
            UpdateStatus("Test data reset");
            UpdatePlayerDisplay();
            DisplayAvailableUpgrades();
            DisplayAvailableProjects();
        }
        
        private void OnUpgradePurchased(string upgradeId, int level)
        {
            Debug.Log($"[BalanceTest] Upgrade purchased: {upgradeId} to level {level}");
        }
        
        private void OnLevelUp(int newLevel)
        {
            testPlayerLevel = newLevel;
            UpdateStatus($"LEVEL UP! Now level {newLevel}");
            UpdatePlayerDisplay();
            DisplayAvailableUpgrades();
        }
        
        private void OnStageUnlocked(int newStage)
        {
            testStage = newStage;
            UpdateStatus($"NEW STAGE UNLOCKED! Stage {newStage}");
            DisplayAvailableProjects();
        }
        
        private void OnProjectCompleted(string projectId)
        {
            if (_balanceManager != null)
            {
                var project = _balanceManager.GetProject(projectId);
                if (project != null)
                {
                    UpdateStatus($"Project completed: {project.nameEn}");
                    testPlayerMoney += project.baseReward;
                    UpdatePlayerDisplay();
                }
            }
        }
        
        private void UpdateStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = $"[{System.DateTime.Now:HH:mm:ss}] {message}";
            }
            else
            {
                Debug.LogWarning("[BalanceTestController] Status Text is not assigned!");
            }
            
            Debug.Log($"[BalanceTest] {message}");
        }
        
        private string FormatNumber(float number)
        {
            if (number >= 1000000000)
                return $"{number / 1000000000:F2}B";
            if (number >= 1000000)
                return $"{number / 1000000:F2}M";
            if (number >= 1000)
                return $"{number / 1000:F2}K";
            return number.ToString("F0");
        }
        
        private void OnDestroy()
        {
            Debug.Log("[BalanceTestController] OnDestroy called");
            
            if (_integrationSystem != null)
            {
                _integrationSystem.OnUpgradePurchased -= OnUpgradePurchased;
                _integrationSystem.OnLevelUp -= OnLevelUp;
                _integrationSystem.OnStageUnlocked -= OnStageUnlocked;
                _integrationSystem.OnProjectCompleted -= OnProjectCompleted;
            }
            
            if (loadDataButton != null)
            {
                loadDataButton.onClick.RemoveAllListeners();
            }
            
            if (testClickButton != null)
            {
                testClickButton.onClick.RemoveAllListeners();
            }
            
            if (autoIncomeButton != null)
            {
                autoIncomeButton.onClick.RemoveAllListeners();
            }
        }
        
        private void OnApplicationQuit()
        {
            Debug.Log("[BalanceTestController] Application quitting");
        }
    }
}