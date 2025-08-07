using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GameDevClicker.Game
{
    /// <summary>
    /// Simple script to ensure buttons are properly configured for clicking
    /// Attach this to any GameObject and call FixButtons() in Start or manually
    /// </summary>
    public class ButtonClickFix : MonoBehaviour
    {
        [Header("Auto-fix on Start")]
        [SerializeField] private bool autoFixOnStart = true;
        
        [Header("Manual Button References")]
        [SerializeField] private Button[] buttonsToFix;
        
        private void Start()
        {
            if (autoFixOnStart)
            {
                FixAllButtonsInScene();
            }
        }
        
        [ContextMenu("Fix All Buttons in Scene")]
        public void FixAllButtonsInScene()
        {
            Debug.Log("[ButtonClickFix] Fixing all buttons in scene...");
            
            // Find all buttons in the scene
            Button[] allButtons = FindObjectsOfType<Button>();
            
            foreach (Button button in allButtons)
            {
                FixButton(button);
            }
            
            // Ensure we have EventSystem
            EnsureEventSystem();
            
            // Ensure Canvas has GraphicRaycaster
            EnsureGraphicRaycaster();
            
            Debug.Log($"[ButtonClickFix] Fixed {allButtons.Length} buttons");
        }
        
        [ContextMenu("Fix Manual Button References")]
        public void FixManualButtons()
        {
            if (buttonsToFix == null || buttonsToFix.Length == 0)
            {
                Debug.LogWarning("[ButtonClickFix] No buttons assigned to fix!");
                return;
            }
            
            Debug.Log("[ButtonClickFix] Fixing manually assigned buttons...");
            
            foreach (Button button in buttonsToFix)
            {
                if (button != null)
                {
                    FixButton(button);
                }
            }
            
            EnsureEventSystem();
            EnsureGraphicRaycaster();
            
            Debug.Log($"[ButtonClickFix] Fixed {buttonsToFix.Length} manual buttons");
        }
        
        private void FixButton(Button button)
        {
            if (button == null) return;
            
            // Ensure button has Image component
            Image buttonImage = button.GetComponent<Image>();
            if (buttonImage == null)
            {
                buttonImage = button.gameObject.AddComponent<Image>();
                buttonImage.color = new Color(0.3f, 0.5f, 0.8f, 1f);
                Debug.Log($"[ButtonClickFix] Added Image component to {button.name}");
            }
            
            // Enable raycast target on the image
            buttonImage.raycastTarget = true;
            
            // Link the image as the target graphic
            button.targetGraphic = buttonImage;
            
            // Ensure button is interactable
            button.interactable = true;
            
            // Set up button colors for visual feedback
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.3f, 0.5f, 0.8f, 1f);
            colors.highlightedColor = new Color(0.4f, 0.6f, 0.9f, 1f);
            colors.pressedColor = new Color(0.2f, 0.4f, 0.7f, 1f);
            colors.selectedColor = new Color(0.3f, 0.5f, 0.8f, 1f);
            colors.disabledColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
            button.colors = colors;
            
            // Ensure text components don't block raycast
            TextMeshProUGUI tmpText = button.GetComponentInChildren<TextMeshProUGUI>();
            if (tmpText != null)
            {
                tmpText.raycastTarget = false;
            }
            
            Text regularText = button.GetComponentInChildren<Text>();
            if (regularText != null)
            {
                regularText.raycastTarget = false;
            }
            
            // Ensure proper RectTransform setup
            RectTransform rectTransform = button.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                // Make sure the button has a reasonable size if it's too small
                if (rectTransform.sizeDelta.x < 10 || rectTransform.sizeDelta.y < 10)
                {
                    rectTransform.sizeDelta = new Vector2(150, 40);
                    Debug.Log($"[ButtonClickFix] Adjusted size for {button.name}");
                }
            }
            
            Debug.Log($"[ButtonClickFix] Fixed button: {button.name}");
        }
        
        private void EnsureEventSystem()
        {
            UnityEngine.EventSystems.EventSystem eventSystem = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
            if (eventSystem == null)
            {
                GameObject eventSystemGO = new GameObject("EventSystem");
                eventSystem = eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                Debug.Log("[ButtonClickFix] Created EventSystem");
            }
            else
            {
                Debug.Log("[ButtonClickFix] EventSystem already exists");
            }
        }
        
        private void EnsureGraphicRaycaster()
        {
            Canvas[] canvases = FindObjectsOfType<Canvas>();
            foreach (Canvas canvas in canvases)
            {
                GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
                if (raycaster == null)
                {
                    raycaster = canvas.gameObject.AddComponent<GraphicRaycaster>();
                    Debug.Log($"[ButtonClickFix] Added GraphicRaycaster to Canvas: {canvas.name}");
                }
            }
        }
        
        [ContextMenu("Debug Button Info")]
        public void DebugButtonInfo()
        {
            Button[] buttons = FindObjectsOfType<Button>();
            Debug.Log($"[ButtonClickFix] Found {buttons.Length} buttons in scene:");
            
            foreach (Button button in buttons)
            {
                Image image = button.GetComponent<Image>();
                string info = $"Button: {button.name} | ";
                info += $"Interactable: {button.interactable} | ";
                info += $"Has Image: {image != null} | ";
                info += $"Raycast Target: {(image != null ? image.raycastTarget.ToString() : "N/A")} | ";
                info += $"Target Graphic: {(button.targetGraphic != null ? button.targetGraphic.name : "None")}";
                
                Debug.Log($"[ButtonClickFix] {info}");
            }
            
            // Check EventSystem
            UnityEngine.EventSystems.EventSystem eventSystem = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
            Debug.Log($"[ButtonClickFix] EventSystem exists: {eventSystem != null}");
            
            // Check GraphicRaycaster
            Canvas[] canvases = FindObjectsOfType<Canvas>();
            Debug.Log($"[ButtonClickFix] Found {canvases.Length} canvases:");
            foreach (Canvas canvas in canvases)
            {
                GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
                Debug.Log($"[ButtonClickFix] Canvas '{canvas.name}' has GraphicRaycaster: {raycaster != null}");
            }
        }
    }
}