using System;
using System.Collections.Generic;
using UnityEngine;
using GameDevClicker.Core.Patterns;

namespace GameDevClicker.Core.Managers
{
    public class ResourceManager : Singleton<ResourceManager>
    {
        [Header("Resource Configuration")]
        [SerializeField] private bool _useAddressables = false;
        [SerializeField] private ResourceSettings _settings;

        private Dictionary<string, UnityEngine.Object> _loadedResources = new Dictionary<string, UnityEngine.Object>();
        private Dictionary<string, int> _referenceCount = new Dictionary<string, int>();

        public event Action<string> OnResourceLoaded;
        public event Action<string> OnResourceUnloaded;
        public event Action<string, string> OnResourceLoadFailed;

        protected override void Awake()
        {
            base.Awake();
            InitializeResourceManager();
        }

        private void InitializeResourceManager()
        {
            if (_settings == null)
            {
                Debug.LogWarning("[ResourceManager] No ResourceSettings assigned. Creating default settings.");
                _settings = ScriptableObject.CreateInstance<ResourceSettings>();
            }

            Debug.Log("[ResourceManager] Resource Manager initialized");
        }

        public T LoadResource<T>(string resourcePath) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(resourcePath))
            {
                Debug.LogError("[ResourceManager] Resource path is null or empty");
                return null;
            }

            if (_loadedResources.TryGetValue(resourcePath, out UnityEngine.Object resource))
            {
                _referenceCount[resourcePath]++;
                return resource as T;
            }

            try
            {
                T loadedResource = Resources.Load<T>(resourcePath);
                
                if (loadedResource != null)
                {
                    _loadedResources[resourcePath] = loadedResource;
                    _referenceCount[resourcePath] = 1;
                    OnResourceLoaded?.Invoke(resourcePath);
                    
                    Debug.Log($"[ResourceManager] Loaded resource: {resourcePath}");
                    return loadedResource;
                }
                else
                {
                    string errorMessage = $"Resource not found at path: {resourcePath}";
                    Debug.LogError($"[ResourceManager] {errorMessage}");
                    OnResourceLoadFailed?.Invoke(resourcePath, errorMessage);
                    return null;
                }
            }
            catch (Exception e)
            {
                string errorMessage = $"Failed to load resource {resourcePath}: {e.Message}";
                Debug.LogError($"[ResourceManager] {errorMessage}");
                OnResourceLoadFailed?.Invoke(resourcePath, errorMessage);
                return null;
            }
        }

        public void UnloadResource(string resourcePath)
        {
            if (string.IsNullOrEmpty(resourcePath))
                return;

            if (!_referenceCount.ContainsKey(resourcePath))
            {
                Debug.LogWarning($"[ResourceManager] Attempted to unload resource that was never loaded: {resourcePath}");
                return;
            }

            _referenceCount[resourcePath]--;

            if (_referenceCount[resourcePath] <= 0)
            {
                _loadedResources.Remove(resourcePath);
                _referenceCount.Remove(resourcePath);
                
                OnResourceUnloaded?.Invoke(resourcePath);
                Debug.Log($"[ResourceManager] Unloaded resource: {resourcePath}");
            }
        }

        public void UnloadAllResources()
        {
            List<string> resourcesToUnload = new List<string>(_loadedResources.Keys);
            
            foreach (string resourcePath in resourcesToUnload)
            {
                _loadedResources.Remove(resourcePath);
                _referenceCount.Remove(resourcePath);
                OnResourceUnloaded?.Invoke(resourcePath);
            }

            _loadedResources.Clear();
            _referenceCount.Clear();

            Resources.UnloadUnusedAssets();
            GC.Collect();

            Debug.Log("[ResourceManager] All resources unloaded");
        }

        public void ForceGarbageCollection()
        {
            Resources.UnloadUnusedAssets();
            GC.Collect();
            Debug.Log("[ResourceManager] Forced garbage collection");
        }

        public bool IsResourceLoaded(string resourcePath)
        {
            return _loadedResources.ContainsKey(resourcePath);
        }

        public int GetReferenceCount(string resourcePath)
        {
            return _referenceCount.ContainsKey(resourcePath) ? _referenceCount[resourcePath] : 0;
        }

        public Dictionary<string, int> GetLoadedResourcesInfo()
        {
            return new Dictionary<string, int>(_referenceCount);
        }

        public void PreloadResources(string[] resourcePaths)
        {
            foreach (string path in resourcePaths)
            {
                LoadResource<UnityEngine.Object>(path);
            }
        }

        public Sprite LoadSprite(string spritePath)
        {
            return LoadResource<Sprite>(spritePath);
        }

        public AudioClip LoadAudioClip(string audioPath)
        {
            return LoadResource<AudioClip>(audioPath);
        }

        public GameObject LoadPrefab(string prefabPath)
        {
            return LoadResource<GameObject>(prefabPath);
        }

        public TextAsset LoadTextAsset(string textPath)
        {
            return LoadResource<TextAsset>(textPath);
        }

        public ScriptableObject LoadScriptableObject(string scriptableObjectPath)
        {
            return LoadResource<ScriptableObject>(scriptableObjectPath);
        }

        private void OnLowMemory()
        {
            Debug.LogWarning("[ResourceManager] Low memory warning received. Unloading unused assets.");
            Resources.UnloadUnusedAssets();
            GC.Collect();
        }

        protected override void OnDestroy()
        {
            UnloadAllResources();
            base.OnDestroy();
        }
    }

    [CreateAssetMenu(fileName = "ResourceSettings", menuName = "Game/Resource Settings")]
    public class ResourceSettings : ScriptableObject
    {
        [Header("General Settings")]
        public bool autoUnloadUnusedAssets = true;
        public float autoUnloadInterval = 60f;
        
        [Header("Memory Management")]
        public int maxLoadedResources = 100;
        public bool enableMemoryWarnings = true;
        
        [Header("Preload Resources")]
        public string[] preloadPaths = new string[0];
        
        [Header("Resource Paths")]
        public string spritesPath = "Sprites/";
        public string audioPath = "Audio/";
        public string prefabsPath = "Prefabs/";
        public string uiPath = "UI/";
        public string dataPath = "Data/";
    }
}