using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GameDevClicker.Core.Patterns;

namespace GameDevClicker.Game.Character
{
    /// <summary>
    /// Manages all characters and distributes click events
    /// </summary>
    public class CharacterManager : Singleton<CharacterManager>
    {
        [Header("Character Management")]
        [SerializeField] private Transform charactersContainer;
        [SerializeField] private GameObject characterPrefab;
        [SerializeField] private int currentStage = 1;
        
        [Header("Click Distribution")]
        [SerializeField] private ClickDistributionMode distributionMode = ClickDistributionMode.All;
        [SerializeField] private bool randomizeClickTarget = false;
        
        [Header("Stage Configuration")]
        [SerializeField] private List<StageCharacterConfig> stageConfigs = new List<StageCharacterConfig>();
        
        private List<CharacterClickHandler> activeCharacters = new List<CharacterClickHandler>();
        private List<CharacterClickHandler> allCharacters = new List<CharacterClickHandler>();
        private int lastClickedIndex = 0;
        private bool isInitialized = false;
        
        public enum ClickDistributionMode
        {
            All,           // All characters react to clicks
            Random,        // Random character reacts
            Sequential,    // Characters react in sequence
            StageSpecific  // Only characters of current stage react
        }
        
        [System.Serializable]
        public class StageCharacterConfig
        {
            public int stage;
            public int characterCount;
            public Vector2[] characterPositions;
            public bool autoActivate = true;
        }
        
        protected override void Awake()
        {
            base.Awake();
            
            // Find characters container if not assigned
            if (charactersContainer == null)
            {
                var container = GameObject.Find("Characters");
                if (container != null)
                    charactersContainer = container.transform;
            }
            
            // Initialize existing characters
            InitializeExistingCharacters();
        }
        
        private void Start()
        {
            // Set up initial stage only if not already initialized
            if (!isInitialized)
            {
                SetupStage(currentStage);
                isInitialized = true;
            }
        }
        
        /// <summary>
        /// Registers a character with the manager
        /// </summary>
        public void RegisterCharacter(CharacterClickHandler character)
        {
            if (!allCharacters.Contains(character))
            {
                allCharacters.Add(character);
                Debug.Log($"[CharacterManager] Registered character: {character.name}");
            }
            
            // Add to active list if it matches current stage
            if (character.Stage == currentStage && character.IsActive)
            {
                if (!activeCharacters.Contains(character))
                {
                    activeCharacters.Add(character);
                }
            }
        }
        
        /// <summary>
        /// Unregisters a character from the manager
        /// </summary>
        public void UnregisterCharacter(CharacterClickHandler character)
        {
            allCharacters.Remove(character);
            activeCharacters.Remove(character);
            Debug.Log($"[CharacterManager] Unregistered character: {character.name}");
        }
        
        /// <summary>
        /// Handles click from the Click Zone
        /// </summary>
        public void OnClickZoneClicked()
        {
            if (activeCharacters.Count == 0)
            {
                Debug.LogWarning("[CharacterManager] No active characters to receive click");
                return;
            }
            
            switch (distributionMode)
            {
                case ClickDistributionMode.All:
                    TriggerAllCharacters();
                    break;
                    
                case ClickDistributionMode.Random:
                    TriggerRandomCharacter();
                    break;
                    
                case ClickDistributionMode.Sequential:
                    TriggerSequentialCharacter();
                    break;
                    
                case ClickDistributionMode.StageSpecific:
                    TriggerStageCharacters();
                    break;
            }
        }
        
        private void TriggerAllCharacters()
        {
            foreach (var character in activeCharacters)
            {
                character.OnClicked();
            }
        }
        
        private void TriggerRandomCharacter()
        {
            if (activeCharacters.Count > 0)
            {
                int randomIndex = Random.Range(0, activeCharacters.Count);
                activeCharacters[randomIndex].OnClicked();
            }
        }
        
        private void TriggerSequentialCharacter()
        {
            if (activeCharacters.Count > 0)
            {
                lastClickedIndex = (lastClickedIndex + 1) % activeCharacters.Count;
                activeCharacters[lastClickedIndex].OnClicked();
            }
        }
        
        private void TriggerStageCharacters()
        {
            var stageCharacters = activeCharacters.Where(c => c.Stage == currentStage).ToList();
            
            if (randomizeClickTarget && stageCharacters.Count > 0)
            {
                int randomIndex = Random.Range(0, stageCharacters.Count);
                stageCharacters[randomIndex].OnClicked();
            }
            else
            {
                foreach (var character in stageCharacters)
                {
                    character.OnClicked();
                }
            }
        }
        
        /// <summary>
        /// Sets up characters for a specific stage
        /// </summary>
        public void SetupStage(int stage)
        {
            currentStage = stage;
            Debug.Log($"[CharacterManager] Setting up stage {stage}");
            
            // Create a copy of the list to avoid modification during iteration
            var charactersCopy = new List<CharacterClickHandler>(allCharacters);
            
            // Deactivate all characters first
            foreach (var character in charactersCopy)
            {
                if (character != null)
                {
                    character.IsActive = false;
                    character.gameObject.SetActive(false);
                }
            }
            
            activeCharacters.Clear();
            
            // Find or create characters for this stage
            var stageConfig = stageConfigs.FirstOrDefault(c => c.stage == stage);
            if (stageConfig != null)
            {
                SetupStageCharacters(stageConfig);
            }
            else
            {
                // Default setup - activate existing character for this stage
                ActivateStageCharacters(stage);
            }
        }
        
        private void SetupStageCharacters(StageCharacterConfig config)
        {
            for (int i = 0; i < config.characterCount; i++)
            {
                // Find or create character
                string characterName = $"CH{config.stage}_{i + 1}_0";
                var characterObj = charactersContainer.Find(characterName);
                
                if (characterObj == null && characterPrefab != null)
                {
                    // Create new character if it doesn't exist
                    characterObj = Instantiate(characterPrefab, charactersContainer).transform;
                    characterObj.name = characterName;
                }
                
                if (characterObj != null)
                {
                    // Position character
                    if (config.characterPositions != null && i < config.characterPositions.Length)
                    {
                        characterObj.localPosition = config.characterPositions[i];
                    }
                    
                    // Get or add CharacterClickHandler
                    var handler = characterObj.GetComponent<CharacterClickHandler>();
                    if (handler == null)
                    {
                        handler = characterObj.gameObject.AddComponent<CharacterClickHandler>();
                    }
                    
                    // Initialize and activate
                    handler.Initialize(config.stage, i + 1);
                    handler.IsActive = config.autoActivate;
                    characterObj.gameObject.SetActive(config.autoActivate);
                    
                    if (config.autoActivate)
                    {
                        RegisterCharacter(handler);
                    }
                }
            }
        }
        
        private void ActivateStageCharacters(int stage)
        {
            // Create a copy to avoid modification during iteration
            var charactersCopy = new List<CharacterClickHandler>(allCharacters);
            
            // Look for existing characters with matching stage
            foreach (var character in charactersCopy)
            {
                if (character != null && character.Stage == stage)
                {
                    character.IsActive = true;
                    character.gameObject.SetActive(true);
                    
                    if (!activeCharacters.Contains(character))
                    {
                        activeCharacters.Add(character);
                    }
                }
            }
            
            // If no characters found, try to find by name pattern
            if (activeCharacters.Count == 0)
            {
                for (int i = 1; i <= 10; i++)
                {
                    string characterName = $"CH{stage}_{i}_0";
                    var characterObj = charactersContainer.Find(characterName);
                    
                    if (characterObj != null)
                    {
                        var handler = characterObj.GetComponent<CharacterClickHandler>();
                        if (handler == null)
                        {
                            handler = characterObj.gameObject.AddComponent<CharacterClickHandler>();
                            handler.Initialize(stage, i);
                        }
                        
                        handler.IsActive = true;
                        characterObj.gameObject.SetActive(true);
                        RegisterCharacter(handler);
                        
                        // Only activate the first character found for now
                        break;
                    }
                }
            }
        }
        
        private void InitializeExistingCharacters()
        {
            if (charactersContainer == null) return;
            
            // Find all existing character objects
            foreach (Transform child in charactersContainer)
            {
                // Check if name matches character pattern (CH#_#_0)
                if (System.Text.RegularExpressions.Regex.IsMatch(child.name, @"CH\d+_\d+_0"))
                {
                    var handler = child.GetComponent<CharacterClickHandler>();
                    if (handler == null)
                    {
                        handler = child.gameObject.AddComponent<CharacterClickHandler>();
                        
                        // Parse stage and index from name
                        var parts = child.name.Replace("CH", "").Replace("_", " ").Split(' ');
                        if (parts.Length >= 2)
                        {
                            int stage = int.Parse(parts[0]);
                            int index = int.Parse(parts[1]);
                            handler.Initialize(stage, index);
                        }
                    }
                    
                    RegisterCharacter(handler);
                }
            }
            
            Debug.Log($"[CharacterManager] Initialized {allCharacters.Count} existing characters");
        }
        
        /// <summary>
        /// Changes the click distribution mode
        /// </summary>
        public void SetDistributionMode(ClickDistributionMode mode)
        {
            distributionMode = mode;
            Debug.Log($"[CharacterManager] Distribution mode set to: {mode}");
        }
        
        /// <summary>
        /// Gets all active characters
        /// </summary>
        public List<CharacterClickHandler> GetActiveCharacters()
        {
            return new List<CharacterClickHandler>(activeCharacters);
        }
        
        /// <summary>
        /// Advances to the next stage
        /// </summary>
        public void NextStage()
        {
            SetupStage(currentStage + 1);
        }
        
        [ContextMenu("Test Click Distribution")]
        public void TestClickDistribution()
        {
            OnClickZoneClicked();
        }
    }
}