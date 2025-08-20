using UnityEngine;
using System.Collections;
using GameDevClicker.Core.Patterns;

namespace GameDevClicker.Core.Managers
{
    /// <summary>
    /// Ensures all core managers are properly initialized and persistent
    /// </summary>
    public class ManagerInitializer : MonoBehaviour
    {
        [Header("Manager Settings")]
        [SerializeField] private bool initializeOnAwake = true;
        [SerializeField] private bool makeManagersPersistent = true;
        [SerializeField] private float initializationDelay = 0.1f;
        
        private void Awake()
        {
            if (initializeOnAwake)
            {
                StartCoroutine(InitializeManagersDelayed());
            }
        }
        
        private IEnumerator InitializeManagersDelayed()
        {
            yield return new WaitForSeconds(initializationDelay);
            
            Debug.Log("[ManagerInitializer] Initializing core managers...");
            
            // Initialize CSVLoader first
            var csvLoader = CSVLoader.Instance;
            if (csvLoader != null)
            {
                if (makeManagersPersistent)
                {
                    DontDestroyOnLoad(csvLoader.gameObject);
                }
                Debug.Log("[ManagerInitializer] CSVLoader initialized");
            }
            else
            {
                Debug.LogWarning("[ManagerInitializer] Failed to initialize CSVLoader");
            }
            
            // Wait a bit more
            yield return new WaitForSeconds(0.1f);
            
            // Initialize BalanceManager
            var balanceManager = BalanceManager.Instance;
            if (balanceManager != null)
            {
                if (makeManagersPersistent)
                {
                    DontDestroyOnLoad(balanceManager.gameObject);
                }
                Debug.Log("[ManagerInitializer] BalanceManager initialized");
            }
            else
            {
                Debug.LogWarning("[ManagerInitializer] Failed to initialize BalanceManager");
            }
            
            // Initialize SaveManager
            var saveManager = SaveManager.Instance;
            if (saveManager != null)
            {
                if (makeManagersPersistent)
                {
                    DontDestroyOnLoad(saveManager.gameObject);
                }
                Debug.Log("[ManagerInitializer] SaveManager initialized");
            }
            
            // Initialize UpgradeManager
            var upgradeManager = UpgradeManager.Instance;
            if (upgradeManager != null)
            {
                if (makeManagersPersistent)
                {
                    DontDestroyOnLoad(upgradeManager.gameObject);
                }
                Debug.Log("[ManagerInitializer] UpgradeManager initialized");
            }
            
            Debug.Log("[ManagerInitializer] Core managers initialization complete!");
        }
        
        [ContextMenu("Force Initialize Managers")]
        public void ForceInitializeManagers()
        {
            StartCoroutine(InitializeManagersDelayed());
        }
        
        [ContextMenu("Check Manager Status")]
        public void CheckManagerStatus()
        {
            Debug.Log("=== MANAGER STATUS ===");
            Debug.Log($"CSVLoader: {(CSVLoader.Instance != null ? "✓ Active" : "✗ Missing")}");
            Debug.Log($"BalanceManager: {(BalanceManager.Instance != null ? "✓ Active" : "✗ Missing")}");
            Debug.Log($"SaveManager: {(SaveManager.Instance != null ? "✓ Active" : "✗ Missing")}");
            Debug.Log($"UpgradeManager: {(UpgradeManager.Instance != null ? "✓ Active" : "✗ Missing")}");
            Debug.Log("======================");
        }
    }
}