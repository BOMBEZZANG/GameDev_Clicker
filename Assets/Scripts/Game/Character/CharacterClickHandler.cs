using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace GameDevClicker.Game.Character
{
    /// <summary>
    /// Handles click effects for individual characters
    /// Attach this to each character GameObject (CH1_1_0, CH2_1_0, etc.)
    /// </summary>
    public class CharacterClickHandler : MonoBehaviour
    {
        [Header("Character Info")]
        [SerializeField] private int characterStage = 1;
        [SerializeField] private int characterIndex = 0;
        [SerializeField] private bool isActive = true;
        
        [Header("Effect Settings")]
        [SerializeField] private float bounceScale = 1.2f;
        [SerializeField] private float bounceDuration = 0.3f;
        [SerializeField] private AnimationCurve bounceCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        [Header("Highlight Settings")]
        [SerializeField] private Color highlightColor = new Color(1f, 1f, 0.5f, 1f);
        [SerializeField] private float highlightDuration = 0.2f;
        [SerializeField] private bool useHighlight = true;
        
        [Header("Components")]
        [SerializeField] private Image characterImage;
        [SerializeField] private SpriteRenderer characterSprite;
        [SerializeField] private CanvasGroup canvasGroup;
        
        private Vector3 originalScale;
        private Color originalColor;
        private Coroutine currentEffectCoroutine;
        private bool isAnimating = false;
        
        public int Stage => characterStage;
        public int Index => characterIndex;
        public bool IsActive 
        { 
            get => isActive;
            set => isActive = value;
        }
        
        private void Awake()
        {
            // Store original values
            originalScale = transform.localScale;
            
            // Try to find components if not assigned
            if (characterImage == null)
                characterImage = GetComponent<Image>();
            if (characterSprite == null)
                characterSprite = GetComponent<SpriteRenderer>();
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
                
            // Store original color
            if (characterImage != null)
                originalColor = characterImage.color;
            else if (characterSprite != null)
                originalColor = characterSprite.color;
            else
                originalColor = Color.white;
        }
        
        private void OnEnable()
        {
            // Register with CharacterManager when enabled
            var manager = CharacterManager.Instance;
            if (manager != null)
            {
                manager.RegisterCharacter(this);
            }
        }
        
        private void OnDisable()
        {
            // Unregister from CharacterManager when disabled
            var manager = CharacterManager.Instance;
            if (manager != null)
            {
                manager.UnregisterCharacter(this);
            }
            
            // Stop any ongoing animations
            if (currentEffectCoroutine != null)
            {
                StopCoroutine(currentEffectCoroutine);
                ResetToOriginal();
            }
        }
        
        /// <summary>
        /// Triggers the click effect on this character
        /// </summary>
        public void OnClicked()
        {
            if (!isActive || isAnimating) return;
            
            // Stop any current effect
            if (currentEffectCoroutine != null)
            {
                StopCoroutine(currentEffectCoroutine);
            }
            
            // Start new effect
            currentEffectCoroutine = StartCoroutine(PlayClickEffect());
        }
        
        private IEnumerator PlayClickEffect()
        {
            isAnimating = true;
            
            // Start bounce and highlight simultaneously
            StartCoroutine(BounceEffect());
            if (useHighlight)
            {
                StartCoroutine(HighlightEffect());
            }
            
            // Wait for the longer animation to complete
            yield return new WaitForSeconds(Mathf.Max(bounceDuration, highlightDuration));
            
            isAnimating = false;
            currentEffectCoroutine = null;
        }
        
        private IEnumerator BounceEffect()
        {
            float elapsed = 0;
            
            while (elapsed < bounceDuration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / bounceDuration;
                float curveValue = bounceCurve.Evaluate(progress);
                
                // Calculate scale based on curve
                float scaleMultiplier = 1f + (bounceScale - 1f) * Mathf.Sin(curveValue * Mathf.PI);
                transform.localScale = originalScale * scaleMultiplier;
                
                yield return null;
            }
            
            transform.localScale = originalScale;
        }
        
        private IEnumerator HighlightEffect()
        {
            float elapsed = 0;
            
            while (elapsed < highlightDuration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / highlightDuration;
                
                // Pulse the highlight
                Color targetColor = Color.Lerp(originalColor, highlightColor, Mathf.Sin(progress * Mathf.PI));
                
                if (characterImage != null)
                    characterImage.color = targetColor;
                else if (characterSprite != null)
                    characterSprite.color = targetColor;
                    
                yield return null;
            }
            
            // Reset to original color
            if (characterImage != null)
                characterImage.color = originalColor;
            else if (characterSprite != null)
                characterSprite.color = originalColor;
        }
        
        private void ResetToOriginal()
        {
            transform.localScale = originalScale;
            
            if (characterImage != null)
                characterImage.color = originalColor;
            else if (characterSprite != null)
                characterSprite.color = originalColor;
                
            isAnimating = false;
        }
        
        /// <summary>
        /// Sets up the character handler with stage info
        /// </summary>
        public void Initialize(int stage, int index)
        {
            characterStage = stage;
            characterIndex = index;
            gameObject.name = $"CH{stage}_{index}_0";
        }
        
        [ContextMenu("Test Click Effect")]
        public void TestClickEffect()
        {
            OnClicked();
        }
    }
}