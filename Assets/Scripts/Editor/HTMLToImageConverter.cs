using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.IO;

namespace GameDevClicker.Game.Managers
{
    public class HTMLToImageConverter : EditorWindow
    {
        [MenuItem("Tools/HTML Background/HTML to Image Converter Guide")]
        public static void ShowConverterGuide()
        {
            HTMLToImageConverter window = GetWindow<HTMLToImageConverter>();
            window.titleContent = new GUIContent("HTML to Image Converter");
            window.Show();
        }
        
        void OnGUI()
        {
            GUILayout.Label("HTML to Image Converter", EditorStyles.boldLabel);
            
            EditorGUILayout.Space();
            
            EditorGUILayout.HelpBox(
                "Since WebView is not supported on Windows Standalone, you need to convert your HTML files to images.\n\n" +
                "Here are several methods to convert HTML to images:", 
                MessageType.Info
            );
            
            EditorGUILayout.Space();
            
            GUILayout.Label("Method 1: Browser Screenshot", EditorStyles.boldLabel);
            EditorGUILayout.TextArea(
                "1. Open your HTML file in a web browser\n" +
                "2. Set browser window to 800x600 (to match your game scene size)\n" +
                "3. Take a screenshot (F12 > Screenshot, or browser dev tools)\n" +
                "4. Save as PNG and import to Unity as sprites", 
                GUILayout.Height(60)
            );
            
            EditorGUILayout.Space();
            
            GUILayout.Label("Method 2: Online HTML to Image Tools", EditorStyles.boldLabel);
            EditorGUILayout.TextArea(
                "• htmlcsstoimage.com\n" +
                "• htmltoimage.io\n" +
                "• Convert your HTML files to PNG images\n" +
                "• Download and import to Unity", 
                GUILayout.Height(60)
            );
            
            EditorGUILayout.Space();
            
            GUILayout.Label("Method 3: Automated (Advanced)", EditorStyles.boldLabel);
            EditorGUILayout.TextArea(
                "• Use headless Chrome via command line\n" +
                "• Puppeteer (Node.js)\n" +
                "• wkhtmltoimage tool\n" +
                "• Batch convert all HTML files", 
                GUILayout.Height(60)
            );
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Open HTML Folder", GUILayout.Height(30)))
            {
                string htmlPath = Path.Combine(Application.dataPath, "StreamingAssets", "HTML");
                if (Directory.Exists(htmlPath))
                {
                    EditorUtility.RevealInFinder(htmlPath);
                }
                else
                {
                    EditorUtility.DisplayDialog("Folder Not Found", 
                        "HTML folder not found. Run 'Copy HTML Files from Source' first.", "OK");
                }
            }
            
            if (GUILayout.Button("Create Sprites Folder", GUILayout.Height(30)))
            {
                string spritesPath = Path.Combine(Application.dataPath, "Art", "Backgrounds", "StageSprites");
                if (!Directory.Exists(spritesPath))
                {
                    Directory.CreateDirectory(spritesPath);
                    AssetDatabase.Refresh();
                    EditorUtility.DisplayDialog("Folder Created", 
                        "Created folder: Assets/Art/Backgrounds/StageSprites\n\nPlace your converted PNG images here.", "OK");
                }
                EditorUtility.RevealInFinder(spritesPath);
            }
            
            EditorGUILayout.Space();
            
            EditorGUILayout.HelpBox(
                "After converting HTML files to images:\n\n" +
                "1. Import PNG files to Unity\n" +
                "2. Set Texture Type to 'Sprite (2D and UI)'\n" +
                "3. Assign sprites to HTMLBackgroundManagerUniversal component\n" +
                "4. The system will automatically use sprites instead of WebView", 
                MessageType.Warning
            );
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Setup Universal Background Manager", GUILayout.Height(40)))
            {
                SetupUniversalManager();
            }
        }
        
        void SetupUniversalManager()
        {
            // Find existing manager and suggest replacement
            var existingManager = Object.FindObjectOfType<HTMLBackgroundManager>();
            if (existingManager != null)
            {
                bool replace = EditorUtility.DisplayDialog("Replace Existing Manager?", 
                    "Found existing HTMLBackgroundManager. Replace with Universal version?\n\n" +
                    "Universal version works on all platforms with automatic fallback.", 
                    "Replace", "Cancel");
                
                if (replace)
                {
                    // Copy settings and replace
                    GameObject go = existingManager.gameObject;
                    var oldData = existingManager.stageData;
                    var oldPanel = existingManager.backgroundPanel;
                    var oldCamera = existingManager.uiCamera;
                    
                    DestroyImmediate(existingManager);
                    
                    var newManager = go.AddComponent<HTMLBackgroundManagerUniversal>();
                    newManager.backgroundPanel = oldPanel;
                    newManager.uiCamera = oldCamera;
                    
                    // Try to find background image
                    var backgroundImg = go.GetComponentInChildren<Image>();
                    if (backgroundImg != null)
                    {
                        newManager.backgroundImage = backgroundImg;
                    }
                    
                    Debug.Log("Replaced HTMLBackgroundManager with Universal version!");
                }
            }
            else
            {
                // Create new universal manager
                GameObject managerGO = new GameObject("HTMLBackgroundManagerUniversal");
                var manager = managerGO.AddComponent<HTMLBackgroundManagerUniversal>();
                
                Debug.Log("Created new HTMLBackgroundManagerUniversal. Don't forget to assign UI references!");
            }
        }
    }
}