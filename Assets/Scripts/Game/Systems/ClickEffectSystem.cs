using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;
using GameDevClicker.Core.Patterns;
using GameDevClicker.Core.Utilities;

namespace GameDevClicker.Game.Systems
{
    public class ClickEffectSystem : Singleton<ClickEffectSystem>
    {
        [Header("Effect Configuration")]
        [SerializeField] private float effectDuration = 1f;
        [SerializeField] private float effectFloatDistance = 70f;
        [SerializeField] private Color normalEffectColor = Color.yellow;
        [SerializeField] private Color criticalEffectColor = Color.red;
        [SerializeField] private int maxActiveEffects = 10;

        [Header("Critical Effect")]
        [SerializeField] private float criticalScaleMultiplier = 1.5f;
        [SerializeField] private float criticalDurationMultiplier = 1.2f;

        [Header("Particle Effects")]
        [SerializeField] private int particleCount = 5;
        [SerializeField] private float particleSpread = 50f;
        [SerializeField] private float particleLifetime = 2f;

        private Queue<VisualElement> _effectPool = new Queue<VisualElement>();
        private List<ClickEffectInstance> _activeEffects = new List<ClickEffectInstance>();
        private VisualElement _effectContainer;

        protected override void Awake()
        {
            base.Awake();
            InitializeEffectSystem();
        }

        private void Start()
        {
            SubscribeToEvents();
        }

        private void InitializeEffectSystem()
        {
            // Pre-create effect pool
            for (int i = 0; i < maxActiveEffects; i++)
            {
                var effect = CreateEffectElement();
                effect.style.display = DisplayStyle.None;
                _effectPool.Enqueue(effect);
            }
        }

        private void SubscribeToEvents()
        {
            GameEvents.OnClickPerformed += OnClickPerformed;
            GameEvents.OnCriticalClick += OnCriticalClick;
        }

        public void SetEffectContainer(VisualElement container)
        {
            _effectContainer = container;
            
            // Add pooled effects to container
            foreach (var effect in _effectPool)
            {
                _effectContainer.Add(effect);
            }
        }

        #region Effect Creation

        private VisualElement CreateEffectElement()
        {
            var effect = new Label();
            effect.AddToClassList("click-effect");
            effect.style.position = Position.Absolute;
            effect.style.fontSize = 20;
            effect.style.color = normalEffectColor;
            effect.style.unityFontStyleAndWeight = FontStyle.Bold;
            effect.style.unityTextAlign = TextAnchor.MiddleCenter;
            effect.style.textShadow = new TextShadow
            {
                offset = new Vector2(2, 2),
                blurRadius = 4,
                color = new Color(0, 0, 0, 0.3f)
            };
            
            return effect;
        }

        private VisualElement GetEffectFromPool()
        {
            if (_effectPool.Count > 0)
            {
                return _effectPool.Dequeue();
            }
            
            // If pool is empty, reuse oldest active effect
            if (_activeEffects.Count > 0)
            {
                var oldestEffect = _activeEffects[0];
                ReturnEffectToPool(oldestEffect.element);
                _activeEffects.RemoveAt(0);
                return GetEffectFromPool();
            }
            
            // Fallback: create new effect
            var newEffect = CreateEffectElement();
            _effectContainer?.Add(newEffect);
            return newEffect;
        }

        private void ReturnEffectToPool(VisualElement effect)
        {
            effect.style.display = DisplayStyle.None;
            effect.style.opacity = 1f;
            effect.style.scale = Vector2.one;
            _effectPool.Enqueue(effect);
        }

        #endregion

        #region Event Handlers

        private void OnClickPerformed(float moneyGained, float expGained)
        {
            Vector2 position = GetRandomClickPosition();
            CreateClickEffect(position, moneyGained, expGained, false);
        }

        private void OnCriticalClick(Vector2 position)
        {
            CreateParticleEffect(position);
        }

        #endregion

        #region Effect Management

        public void CreateClickEffect(Vector2 position, float moneyGained, float expGained, bool isCritical = false)
        {
            if (_effectContainer == null) return;

            var effect = GetEffectFromPool();
            var label = effect as Label;
            
            if (label == null) return;

            // Set effect text
            string effectText = FormatEffectText(moneyGained, expGained);
            label.text = effectText;

            // Set position
            effect.style.left = position.x - 30;
            effect.style.top = position.y - 20;
            effect.style.display = DisplayStyle.Flex;

            // Apply critical effect styling
            if (isCritical)
            {
                effect.style.color = criticalEffectColor;
                effect.style.scale = Vector2.one * criticalScaleMultiplier;
                effect.style.fontSize = 24;
            }
            else
            {
                effect.style.color = normalEffectColor;
                effect.style.scale = Vector2.one;
                effect.style.fontSize = 20;
            }

            // Create effect instance for animation tracking
            var effectInstance = new ClickEffectInstance
            {
                element = effect,
                startTime = Time.time,
                duration = isCritical ? effectDuration * criticalDurationMultiplier : effectDuration,
                startPosition = position,
                isCritical = isCritical
            };

            _activeEffects.Add(effectInstance);
            StartCoroutine(AnimateEffect(effectInstance));
        }

        private string FormatEffectText(float moneyGained, float expGained)
        {
            if (moneyGained > 0)
            {
                return $"+{NumberFormatter.Format((long)expGained)} ⭐\n+{NumberFormatter.Format((long)moneyGained)} 💰";
            }
            else
            {
                return $"+{NumberFormatter.Format((long)expGained)} ⭐";
            }
        }

        private IEnumerator AnimateEffect(ClickEffectInstance effectInstance)
        {
            var element = effectInstance.element;
            var startPos = effectInstance.startPosition;
            var duration = effectInstance.duration;
            
            float elapsedTime = 0f;
            
            while (elapsedTime < duration)
            {
                float normalizedTime = elapsedTime / duration;
                
                // Float upward animation
                float yOffset = Mathf.Lerp(0, -effectFloatDistance, normalizedTime);
                element.style.top = startPos.y + yOffset;
                
                // Fade out animation
                float opacity = Mathf.Lerp(1f, 0f, normalizedTime);
                element.style.opacity = opacity;
                
                // Slight scale animation for critical hits
                if (effectInstance.isCritical)
                {
                    float scale = Mathf.Lerp(criticalScaleMultiplier, criticalScaleMultiplier * 0.8f, normalizedTime);
                    element.style.scale = Vector2.one * scale;
                }
                
                elapsedTime += Time.unscaledDeltaTime;
                yield return null;
            }
            
            // Animation complete - return to pool
            _activeEffects.Remove(effectInstance);
            ReturnEffectToPool(element);
        }

        #endregion

        #region Particle Effects

        public void CreateParticleEffect(Vector2 position)
        {
            if (_effectContainer == null) return;

            for (int i = 0; i < particleCount; i++)
            {
                StartCoroutine(CreateSingleParticle(position));
            }
        }

        private IEnumerator CreateSingleParticle(Vector2 centerPosition)
        {
            var particle = new VisualElement();
            particle.style.position = Position.Absolute;
            particle.style.width = 4;
            particle.style.height = 4;
            particle.style.backgroundColor = new Color(1f, 1f, 1f, 0.5f);
            particle.style.borderTopLeftRadius = 2;
            particle.style.borderTopRightRadius = 2;
            particle.style.borderBottomLeftRadius = 2;
            particle.style.borderBottomRightRadius = 2;
            
            // Random position around click point
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float distance = Random.Range(10f, particleSpread);
            Vector2 startPos = centerPosition + new Vector2(
                Mathf.Cos(angle) * distance,
                Mathf.Sin(angle) * distance
            );
            
            particle.style.left = startPos.x;
            particle.style.top = startPos.y;
            
            _effectContainer.Add(particle);
            
            float elapsedTime = 0f;
            Vector2 velocity = new Vector2(
                Random.Range(-50f, 50f),
                Random.Range(-100f, -50f)
            );
            
            while (elapsedTime < particleLifetime)
            {
                float normalizedTime = elapsedTime / particleLifetime;
                
                // Physics-like movement
                Vector2 currentPos = startPos + velocity * elapsedTime;
                currentPos.y += 0.5f * 98f * elapsedTime * elapsedTime; // Gravity
                
                particle.style.left = currentPos.x;
                particle.style.top = currentPos.y;
                
                // Fade out
                float opacity = Mathf.Lerp(0.5f, 0f, normalizedTime);
                particle.style.backgroundColor = new Color(1f, 1f, 1f, opacity);
                
                elapsedTime += Time.unscaledDeltaTime;
                yield return null;
            }
            
            particle.RemoveFromHierarchy();
        }

        #endregion

        #region Utility Methods

        private Vector2 GetRandomClickPosition()
        {
            // Return a position within the click zone area
            // This could be enhanced to get actual click zone bounds
            return new Vector2(
                Random.Range(-50f, 50f),
                Random.Range(-50f, 50f)
            );
        }

        public void ClearAllEffects()
        {
            foreach (var effect in _activeEffects)
            {
                StopCoroutine(AnimateEffect(effect));
                ReturnEffectToPool(effect.element);
            }
            _activeEffects.Clear();
        }

        public void SetEffectColor(Color normalColor, Color criticalColor)
        {
            normalEffectColor = normalColor;
            criticalEffectColor = criticalColor;
        }

        public void SetEffectDuration(float duration)
        {
            effectDuration = Mathf.Max(0.1f, duration);
        }

        #endregion

        #region Configuration Methods

        [System.Serializable]
        public class EffectSettings
        {
            public float duration = 1f;
            public float floatDistance = 70f;
            public Color normalColor = Color.yellow;
            public Color criticalColor = Color.red;
            public int maxActiveEffects = 10;
        }

        public void ApplySettings(EffectSettings settings)
        {
            effectDuration = settings.duration;
            effectFloatDistance = settings.floatDistance;
            normalEffectColor = settings.normalColor;
            criticalEffectColor = settings.criticalColor;
            maxActiveEffects = settings.maxActiveEffects;
        }

        #endregion

        protected override void OnDestroy()
        {
            GameEvents.OnClickPerformed -= OnClickPerformed;
            GameEvents.OnCriticalClick -= OnCriticalClick;
            ClearAllEffects();
            base.OnDestroy();
        }

        private class ClickEffectInstance
        {
            public VisualElement element;
            public float startTime;
            public float duration;
            public Vector2 startPosition;
            public bool isCritical;
        }
    }
}