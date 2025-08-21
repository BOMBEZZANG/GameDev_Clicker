using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GameDevClicker.Game.Effects
{
    /// <summary>
    /// Handles the animation and display of floating text effects (like +100 Gold, +50 EXP)
    /// </summary>
    public class FloatingText : MonoBehaviour
    {
        [Header("Components")]
        private TextMeshProUGUI textComponent;
        private CanvasGroup canvasGroup;
        private RectTransform rectTransform;
        
        [Header("Animation Settings")]
        [SerializeField] private float animationDuration = 1.5f;
        [SerializeField] private float moveDistance = 100f;
        [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.Linear(0, 1, 1, 0);
        [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.Linear(0, 1, 1, 1);
        
        private Coroutine animationCoroutine;
        private System.Action<FloatingText> onComplete;
        
        private void Awake()
        {
            // Get or create components
            rectTransform = GetComponent<RectTransform>();
            if (rectTransform == null)
                rectTransform = gameObject.AddComponent<RectTransform>();
            
            textComponent = GetComponent<TextMeshProUGUI>();
            if (textComponent == null)
                textComponent = gameObject.AddComponent<TextMeshProUGUI>();
            
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            
            // Configure text defaults
            textComponent.alignment = TextAlignmentOptions.Center;
            textComponent.fontSize = 24;
            textComponent.fontStyle = FontStyles.Bold;
        }
        
        /// <summary>
        /// Initialize and start the floating text animation
        /// </summary>
        public void Show(string text, Vector2 startPosition, Color color, float fontSize = 24f, System.Action<FloatingText> onCompleteCallback = null)
        {
            // Set text and appearance
            textComponent.text = text;
            textComponent.color = color;
            textComponent.fontSize = fontSize;
            
            // Set initial position
            rectTransform.anchoredPosition = startPosition;
            
            // Store callback
            onComplete = onCompleteCallback;
            
            // Stop any existing animation
            if (animationCoroutine != null)
            {
                StopCoroutine(animationCoroutine);
            }
            
            // Start animation
            gameObject.SetActive(true);
            animationCoroutine = StartCoroutine(AnimateText());
        }
        
        private IEnumerator AnimateText()
        {
            Vector2 startPos = rectTransform.anchoredPosition;
            Vector2 endPos = startPos + Vector2.up * moveDistance;
            
            float elapsed = 0f;
            
            while (elapsed < animationDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / animationDuration;
                
                // Update position
                float moveT = moveCurve.Evaluate(t);
                rectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, moveT);
                
                // Update alpha
                canvasGroup.alpha = fadeCurve.Evaluate(t);
                
                // Update scale
                float scale = scaleCurve.Evaluate(t);
                rectTransform.localScale = Vector3.one * scale;
                
                yield return null;
            }
            
            // Hide and callback
            gameObject.SetActive(false);
            onComplete?.Invoke(this);
        }
        
        /// <summary>
        /// Configure animation curves for different effect styles
        /// </summary>
        public void SetAnimationStyle(FloatingTextStyle style)
        {
            switch (style)
            {
                case FloatingTextStyle.Default:
                    moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
                    fadeCurve = AnimationCurve.Linear(0, 1, 1, 0);
                    scaleCurve = AnimationCurve.Linear(0, 1, 1, 1);
                    animationDuration = 1.5f;
                    moveDistance = 100f;
                    break;
                    
                case FloatingTextStyle.Bounce:
                    moveCurve = AnimationCurve.EaseInOut(0, 0, 0.5f, 1.2f);
                    moveCurve.AddKey(1, 1);
                    fadeCurve = AnimationCurve.Linear(0, 1, 0.7f, 1);
                    fadeCurve.AddKey(1, 0);
                    scaleCurve = AnimationCurve.Linear(0, 1.2f, 0.3f, 1);
                    scaleCurve.AddKey(1, 0.9f);
                    animationDuration = 1.2f;
                    moveDistance = 80f;
                    break;
                    
                case FloatingTextStyle.Critical:
                    moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
                    fadeCurve = AnimationCurve.Linear(0, 1, 0.8f, 1);
                    fadeCurve.AddKey(1, 0);
                    scaleCurve = AnimationCurve.Linear(0, 1.5f, 0.2f, 1.2f);
                    scaleCurve.AddKey(1, 1);
                    animationDuration = 1.8f;
                    moveDistance = 120f;
                    break;
                    
                case FloatingTextStyle.Quick:
                    moveCurve = AnimationCurve.Linear(0, 0, 1, 1);
                    fadeCurve = AnimationCurve.Linear(0, 1, 0.5f, 1);
                    fadeCurve.AddKey(1, 0);
                    scaleCurve = AnimationCurve.Linear(0, 1, 1, 1);
                    animationDuration = 0.8f;
                    moveDistance = 60f;
                    break;
            }
        }
        
        public void Reset()
        {
            if (animationCoroutine != null)
            {
                StopCoroutine(animationCoroutine);
                animationCoroutine = null;
            }
            
            canvasGroup.alpha = 1f;
            rectTransform.localScale = Vector3.one;
            gameObject.SetActive(false);
        }
    }
    
    public enum FloatingTextStyle
    {
        Default,
        Bounce,
        Critical,
        Quick
    }
}