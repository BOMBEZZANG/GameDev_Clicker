using System.Collections.Generic;
using UnityEngine;
using GameDevClicker.Core.Patterns;

namespace GameDevClicker.Game.Effects
{
    /// <summary>
    /// Manages click sound effects for different character stages
    /// </summary>
    public class ClickSoundManager : Singleton<ClickSoundManager>
    {
        [Header("Audio Settings")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private float volume = 0.7f;
        [SerializeField] private bool randomizePitch = true;
        [SerializeField] private Vector2 pitchRange = new Vector2(0.9f, 1.1f);
        
        [Header("Stage Sound Mapping")]
        [SerializeField] private List<StageSoundMapping> stageSounds = new List<StageSoundMapping>();
        
        [System.Serializable]
        public class StageSoundMapping
        {
            [Tooltip("Stage identifier (e.g., 1 for CH1_1)")]
            public int stage;
            
            [Tooltip("Character description for reference")]
            public string characterDescription;
            
            [Tooltip("Sound clips for this stage (random selection if multiple)")]
            public AudioClip[] soundClips;
            
            [Tooltip("Volume multiplier for this stage")]
            [Range(0f, 2f)]
            public float volumeMultiplier = 1f;
        }
        
        private Dictionary<int, StageSoundMapping> soundMappings = new Dictionary<int, StageSoundMapping>();
        
        protected override void Awake()
        {
            base.Awake();
            InitializeAudioSource();
            BuildSoundMappings();
        }
        
        private void InitializeAudioSource()
        {
            if (audioSource == null)
            {
                audioSource = gameObject.GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
            }
            
            // Configure audio source
            audioSource.playOnAwake = false;
            audioSource.volume = volume;
            audioSource.spatialBlend = 0f; // 2D sound
            audioSource.priority = 128;
            
            Debug.Log($"[ClickSoundManager] AudioSource initialized with volume: {volume}");
        }
        
        private void BuildSoundMappings()
        {
            soundMappings.Clear();
            
            foreach (var mapping in stageSounds)
            {
                if (mapping.soundClips != null && mapping.soundClips.Length > 0)
                {
                    soundMappings[mapping.stage] = mapping;
                    Debug.Log($"[ClickSoundManager] Mapped stage {mapping.stage} ({mapping.characterDescription}) with {mapping.soundClips.Length} sound(s)");
                }
                else
                {
                    Debug.LogWarning($"[ClickSoundManager] Stage {mapping.stage} has no sound clips assigned!");
                }
            }
        }
        
        /// <summary>
        /// Play click sound for a specific stage
        /// </summary>
        /// <param name="stage">Stage number (e.g., 1 for CH1_1)</param>
        public void PlayClickSound(int stage)
        {
            if (!soundMappings.ContainsKey(stage))
            {
                Debug.LogWarning($"[ClickSoundManager] No sound mapping found for stage {stage}");
                return;
            }
            
            var mapping = soundMappings[stage];
            if (mapping.soundClips == null || mapping.soundClips.Length == 0)
            {
                Debug.LogWarning($"[ClickSoundManager] No sound clips available for stage {stage}");
                return;
            }
            
            // Select random sound clip if multiple available
            AudioClip clipToPlay = mapping.soundClips[Random.Range(0, mapping.soundClips.Length)];
            
            if (clipToPlay == null)
            {
                Debug.LogWarning($"[ClickSoundManager] Selected sound clip is null for stage {stage}");
                return;
            }
            
            // Calculate volume with stage multiplier
            float finalVolume = volume * mapping.volumeMultiplier;
            
            // Randomize pitch if enabled
            if (randomizePitch)
            {
                audioSource.pitch = Random.Range(pitchRange.x, pitchRange.y);
            }
            else
            {
                audioSource.pitch = 1f;
            }
            
            // Play the sound
            audioSource.PlayOneShot(clipToPlay, finalVolume);
            
            Debug.Log($"[ClickSoundManager] Playing sound for stage {stage} ({mapping.characterDescription}) - Volume: {finalVolume}, Pitch: {audioSource.pitch}");
        }
        
        /// <summary>
        /// Play click sound based on character stage and index
        /// </summary>
        /// <param name="stageString">Stage string like "CH1_1"</param>
        public void PlayClickSound(string stageString)
        {
            if (string.IsNullOrEmpty(stageString))
            {
                Debug.LogWarning("[ClickSoundManager] Stage string is null or empty");
                return;
            }
            
            // Extract stage number from string like "CH1_1" -> 1
            if (stageString.StartsWith("CH") && stageString.Contains("_"))
            {
                string[] parts = stageString.Split('_');
                if (parts.Length >= 2 && parts[0].StartsWith("CH"))
                {
                    string stageNumberString = parts[0].Substring(2); // Remove "CH"
                    if (int.TryParse(stageNumberString, out int stage))
                    {
                        PlayClickSound(stage);
                        return;
                    }
                }
            }
            
            Debug.LogWarning($"[ClickSoundManager] Could not parse stage from string: {stageString}");
        }
        
        /// <summary>
        /// Set global volume for click sounds
        /// </summary>
        public void SetVolume(float newVolume)
        {
            volume = Mathf.Clamp01(newVolume);
            if (audioSource != null)
            {
                audioSource.volume = volume;
            }
            Debug.Log($"[ClickSoundManager] Volume set to: {volume}");
        }
        
        /// <summary>
        /// Add or update sound mapping for a stage
        /// </summary>
        public void AddStageSound(int stage, string description, AudioClip[] clips, float volumeMultiplier = 1f)
        {
            var mapping = new StageSoundMapping
            {
                stage = stage,
                characterDescription = description,
                soundClips = clips,
                volumeMultiplier = volumeMultiplier
            };
            
            // Add to list if not exists
            bool found = false;
            for (int i = 0; i < stageSounds.Count; i++)
            {
                if (stageSounds[i].stage == stage)
                {
                    stageSounds[i] = mapping;
                    found = true;
                    break;
                }
            }
            
            if (!found)
            {
                stageSounds.Add(mapping);
            }
            
            // Update dictionary
            soundMappings[stage] = mapping;
            
            Debug.Log($"[ClickSoundManager] Added/Updated sound mapping for stage {stage}: {description}");
        }
        
        /// <summary>
        /// Test sound playback for a specific stage
        /// </summary>
        [ContextMenu("Test Stage 1 Sound")]
        public void TestStage1Sound()
        {
            PlayClickSound(1);
        }
        
        /// <summary>
        /// Get all configured stages
        /// </summary>
        public List<int> GetConfiguredStages()
        {
            return new List<int>(soundMappings.Keys);
        }
    }
}