using UnityEngine;
using UnityEditor;
using GameDevClicker.Game.Effects;

namespace GameDevClicker.Editor
{
    /// <summary>
    /// Helper tool to set up ClickEffectManager in the scene
    /// </summary>
    public class ClickEffectSetup : EditorWindow
    {
        [MenuItem("Tools/Game Dev Clicker/Setup Click Effects")]
        public static void ShowWindow()
        {
            GetWindow<ClickEffectSetup>("Click Effect Setup");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("Click Effect Setup", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            GUILayout.Label("This will create a ClickEffectManager in your scene if one doesn't exist.");
            GUILayout.Space(5);
            
            if (GUILayout.Button("Setup Click Effect Manager", GUILayout.Height(40)))
            {
                SetupClickEffectManager();
            }
            
            GUILayout.Space(10);
            GUILayout.Label("Features:", EditorStyles.helpBox);
            GUILayout.Label("• Floating text for EXP and Gold gains");
            GUILayout.Label("• Object pooling for performance");
            GUILayout.Label("• Multiple animation styles");
            GUILayout.Label("• Combo and special effects");
            GUILayout.Label("• Automatic canvas detection");
        }
        
        private void SetupClickEffectManager()
        {
            // Check if ClickEffectManager already exists
            ClickEffectManager existing = FindObjectOfType<ClickEffectManager>();
            if (existing != null)
            {
                EditorUtility.DisplayDialog("Info", "ClickEffectManager already exists in the scene!", "OK");
                Selection.activeGameObject = existing.gameObject;
                return;
            }
            
            // Find main canvas
            Canvas mainCanvas = FindObjectOfType<Canvas>();
            if (mainCanvas == null)
            {
                EditorUtility.DisplayDialog("Error", "No Canvas found in scene! Please create a Canvas first.", "OK");
                return;
            }
            
            // Create ClickEffectManager GameObject
            GameObject managerObj = new GameObject("ClickEffectManager");
            ClickEffectManager manager = managerObj.AddComponent<ClickEffectManager>();
            
            // Set up the effect container
            GameObject containerObj = new GameObject("ClickEffectContainer");
            containerObj.transform.SetParent(mainCanvas.transform, false);
            
            // Configure container RectTransform
            RectTransform containerRect = containerObj.AddComponent<RectTransform>();
            containerRect.anchorMin = Vector2.zero;
            containerRect.anchorMax = Vector2.one;
            containerRect.sizeDelta = Vector2.zero;
            containerRect.anchoredPosition = Vector2.zero;
            
            // Configure manager (using reflection to set private fields if needed)
            var effectContainerField = typeof(ClickEffectManager).GetField("effectContainer", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (effectContainerField != null)
            {
                effectContainerField.SetValue(manager, containerObj.transform);
            }
            
            var canvasField = typeof(ClickEffectManager).GetField("effectCanvas", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (canvasField != null)
            {
                canvasField.SetValue(manager, mainCanvas);
            }
            
            // Select the created manager
            Selection.activeGameObject = managerObj;
            
            // Mark scene as dirty
            EditorUtility.SetDirty(managerObj);
            EditorUtility.SetDirty(containerObj);
            
            Debug.Log("ClickEffectManager setup completed!");
            EditorUtility.DisplayDialog("Success", "ClickEffectManager has been set up successfully!", "OK");
        }
    }
}