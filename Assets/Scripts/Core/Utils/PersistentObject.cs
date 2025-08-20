using UnityEngine;

namespace GameDevClicker.Core.Utils
{
    /// <summary>
    /// Simple component to ensure an object persists across scenes
    /// </summary>
    public class PersistentObject : MonoBehaviour
    {
        [Header("Persistence Settings")]
        [SerializeField] private bool dontDestroyOnLoad = true;
        [SerializeField] private bool preventDuplicates = true;
        [SerializeField] private string uniqueIdentifier = "";
        
        private void Awake()
        {
            if (dontDestroyOnLoad)
            {
                // Check for duplicates if enabled
                if (preventDuplicates)
                {
                    string identifier = string.IsNullOrEmpty(uniqueIdentifier) ? gameObject.name : uniqueIdentifier;
                    
                    // Look for existing objects with the same identifier
                    var existingObjects = FindObjectsOfType<PersistentObject>();
                    foreach (var obj in existingObjects)
                    {
                        if (obj != this && obj.gameObject != gameObject)
                        {
                            string objIdentifier = string.IsNullOrEmpty(obj.uniqueIdentifier) ? obj.gameObject.name : obj.uniqueIdentifier;
                            if (objIdentifier == identifier)
                            {
                                Debug.Log($"[PersistentObject] Destroying duplicate object: {gameObject.name}");
                                Destroy(gameObject);
                                return;
                            }
                        }
                    }
                }
                
                DontDestroyOnLoad(gameObject);
                Debug.Log($"[PersistentObject] Made {gameObject.name} persistent");
            }
        }
        
        private void Start()
        {
            // Move to DontDestroyOnLoad scene if not already there
            if (dontDestroyOnLoad && transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }
        }
    }
}