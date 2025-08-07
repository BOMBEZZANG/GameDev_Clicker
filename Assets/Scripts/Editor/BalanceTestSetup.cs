using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using GameDevClicker.Game;

namespace GameDevClicker.Editor
{
    public class BalanceTestSetup : EditorWindow
    {
        [MenuItem("Tools/Balance Test/Setup Test Scene")]
        public static void SetupTestScene()
        {
            Debug.Log("[BalanceTestSetup] Setting up balance test scene...");
            
            // Create or find Canvas
            Canvas canvas = GameObject.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasGO = new GameObject("Canvas");
                canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGO.AddComponent<CanvasScaler>();
                canvasGO.AddComponent<GraphicRaycaster>();
            }
            
            // Create EventSystem if needed
            if (GameObject.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystemGO = new GameObject("EventSystem");
                eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
            
            // Create Balance Test Controller
            GameObject testController = GameObject.Find("BalanceTestController");
            if (testController == null)
            {
                testController = new GameObject("BalanceTestController");
            }
            
            BalanceTestController controller = testController.GetComponent<BalanceTestController>();
            if (controller == null)
            {
                controller = testController.AddComponent<BalanceTestController>();
            }
            
            // Create UI Panel
            GameObject panel = CreateOrFindChild(canvas.gameObject, "TestPanel");
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            if (panelRect == null) panelRect = panel.AddComponent<RectTransform>();
            
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = new Vector2(20, 20);
            panelRect.offsetMax = new Vector2(-20, -20);
            
            Image panelImage = panel.GetComponent<Image>();
            if (panelImage == null) panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0.2f, 0.2f, 0.2f, 0.95f);
            
            // Create Text Elements
            GameObject statusText = CreateTextElement(panel, "StatusText", new Vector2(400, 100), new Vector2(10, -10));
            GameObject playerDataText = CreateTextElement(panel, "PlayerDataText", new Vector2(380, 200), new Vector2(10, -120));
            GameObject upgradesText = CreateTextElement(panel, "UpgradesText", new Vector2(380, 200), new Vector2(400, -120));
            GameObject projectsText = CreateTextElement(panel, "ProjectsText", new Vector2(380, 150), new Vector2(10, -330));
            
            // Create Buttons
            GameObject loadButton = CreateButton(panel, "LoadDataButton", "Load CSV Data", new Vector2(150, 40), new Vector2(10, -490));
            GameObject clickButton = CreateButton(panel, "TestClickButton", "Test Click", new Vector2(150, 40), new Vector2(170, -490));
            GameObject autoButton = CreateButton(panel, "AutoIncomeButton", "Start Auto", new Vector2(150, 40), new Vector2(330, -490));
            
            // Assign references to the controller
            SerializedObject serializedController = new SerializedObject(controller);
            
            serializedController.FindProperty("statusText").objectReferenceValue = statusText.GetComponent<TextMeshProUGUI>();
            serializedController.FindProperty("playerDataText").objectReferenceValue = playerDataText.GetComponent<TextMeshProUGUI>();
            serializedController.FindProperty("upgradesText").objectReferenceValue = upgradesText.GetComponent<TextMeshProUGUI>();
            serializedController.FindProperty("projectsText").objectReferenceValue = projectsText.GetComponent<TextMeshProUGUI>();
            serializedController.FindProperty("loadDataButton").objectReferenceValue = loadButton.GetComponent<Button>();
            serializedController.FindProperty("testClickButton").objectReferenceValue = clickButton.GetComponent<Button>();
            serializedController.FindProperty("autoIncomeButton").objectReferenceValue = autoButton.GetComponent<Button>();
            
            serializedController.ApplyModifiedProperties();
            
            // Mark the scene as dirty
            EditorUtility.SetDirty(testController);
            EditorUtility.SetDirty(canvas.gameObject);
            
            Debug.Log("[BalanceTestSetup] Test scene setup complete!");
            Debug.Log("[BalanceTestSetup] You can now play the scene to test CSV loading.");
            
            // Select the test controller
            Selection.activeGameObject = testController;
            
            // Focus on the test controller in the hierarchy
            EditorGUIUtility.PingObject(testController);
        }
        
        private static GameObject CreateOrFindChild(GameObject parent, string name)
        {
            Transform child = parent.transform.Find(name);
            if (child != null)
                return child.gameObject;
            
            GameObject newChild = new GameObject(name);
            newChild.transform.SetParent(parent.transform, false);
            return newChild;
        }
        
        private static GameObject CreateTextElement(GameObject parent, string name, Vector2 size, Vector2 position)
        {
            GameObject textGO = CreateOrFindChild(parent, name);
            
            RectTransform rect = textGO.GetComponent<RectTransform>();
            if (rect == null) rect = textGO.AddComponent<RectTransform>();
            
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            
            TextMeshProUGUI text = textGO.GetComponent<TextMeshProUGUI>();
            if (text == null) text = textGO.AddComponent<TextMeshProUGUI>();
            
            text.text = name + " (Ready)";
            text.fontSize = 14;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.TopLeft;
            
            return textGO;
        }
        
        private static GameObject CreateButton(GameObject parent, string name, string buttonText, Vector2 size, Vector2 position)
        {
            GameObject buttonGO = CreateOrFindChild(parent, name);
            
            RectTransform rect = buttonGO.GetComponent<RectTransform>();
            if (rect == null) rect = buttonGO.AddComponent<RectTransform>();
            
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            
            // Add Image first (required for Button)
            Image buttonImage = buttonGO.GetComponent<Image>();
            if (buttonImage == null) buttonImage = buttonGO.AddComponent<Image>();
            buttonImage.color = new Color(0.3f, 0.5f, 0.8f, 1f);
            buttonImage.raycastTarget = true; // Essential for button clicks
            
            // Add Button component
            Button button = buttonGO.GetComponent<Button>();
            if (button == null) button = buttonGO.AddComponent<Button>();
            button.targetGraphic = buttonImage; // Link the image to the button
            button.interactable = true; // Ensure button is interactable
            
            // Set button colors for visual feedback
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.3f, 0.5f, 0.8f, 1f);
            colors.highlightedColor = new Color(0.4f, 0.6f, 0.9f, 1f);
            colors.pressedColor = new Color(0.2f, 0.4f, 0.7f, 1f);
            colors.selectedColor = new Color(0.3f, 0.5f, 0.8f, 1f);
            colors.disabledColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
            button.colors = colors;
            
            // Create text child
            GameObject textGO = CreateOrFindChild(buttonGO, "Text");
            RectTransform textRect = textGO.GetComponent<RectTransform>();
            if (textRect == null) textRect = textGO.AddComponent<RectTransform>();
            
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;
            
            TextMeshProUGUI text = textGO.GetComponent<TextMeshProUGUI>();
            if (text == null) text = textGO.AddComponent<TextMeshProUGUI>();
            
            text.text = buttonText;
            text.fontSize = 16;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false; // Text should not block button clicks
            
            return buttonGO;
        }
        
        [MenuItem("Tools/Balance Test/Fix Button Interactions")]
        public static void FixButtonInteractions()
        {
            Debug.Log("[BalanceTestSetup] Fixing button interactions...");
            
            // Find all buttons in the scene
            Button[] buttons = GameObject.FindObjectsOfType<Button>();
            
            foreach (Button button in buttons)
            {
                // Check if this is one of our test buttons
                if (button.name.Contains("LoadData") || button.name.Contains("TestClick") || button.name.Contains("AutoIncome"))
                {
                    // Ensure button has proper Image component
                    Image buttonImage = button.GetComponent<Image>();
                    if (buttonImage == null)
                    {
                        buttonImage = button.gameObject.AddComponent<Image>();
                        buttonImage.color = new Color(0.3f, 0.5f, 0.8f, 1f);
                    }
                    
                    // Enable raycast target
                    buttonImage.raycastTarget = true;
                    
                    // Link image to button
                    button.targetGraphic = buttonImage;
                    button.interactable = true;
                    
                    // Set proper button colors
                    ColorBlock colors = button.colors;
                    colors.normalColor = new Color(0.3f, 0.5f, 0.8f, 1f);
                    colors.highlightedColor = new Color(0.4f, 0.6f, 0.9f, 1f);
                    colors.pressedColor = new Color(0.2f, 0.4f, 0.7f, 1f);
                    button.colors = colors;
                    
                    // Ensure text doesn't block clicks
                    TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>();
                    if (text != null)
                    {
                        text.raycastTarget = false;
                    }
                    
                    Text regularText = button.GetComponentInChildren<Text>();
                    if (regularText != null)
                    {
                        regularText.raycastTarget = false;
                    }
                    
                    Debug.Log($"[BalanceTestSetup] Fixed button: {button.name}");
                    EditorUtility.SetDirty(button.gameObject);
                }
            }
            
            // Check EventSystem
            UnityEngine.EventSystems.EventSystem eventSystem = GameObject.FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
            if (eventSystem == null)
            {
                GameObject eventSystemGO = new GameObject("EventSystem");
                eventSystem = eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                Debug.Log("[BalanceTestSetup] Created missing EventSystem");
            }
            
            // Check GraphicRaycaster on Canvas
            Canvas canvas = GameObject.FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
                if (raycaster == null)
                {
                    raycaster = canvas.gameObject.AddComponent<GraphicRaycaster>();
                    Debug.Log("[BalanceTestSetup] Added GraphicRaycaster to Canvas");
                }
            }
            
            Debug.Log("[BalanceTestSetup] Button interaction fixes complete!");
        }
        
        [MenuItem("Tools/Balance Test/Check CSV Files")]
        public static void CheckCSVFiles()
        {
            string basePath = "Assets/GameData/CSV/Balancing/";
            string[] requiredFiles = {
                "Upgrades.csv",
                "Levels.csv",
                "Projects.csv",
                "Stage.csv",
                "DualCurrency.csv",
                "ExpectedTime.csv"
            };
            
            Debug.Log("[BalanceTestSetup] Checking CSV files...");
            bool allFilesExist = true;
            
            foreach (string file in requiredFiles)
            {
                string fullPath = basePath + file;
                if (System.IO.File.Exists(fullPath))
                {
                    Debug.Log($"✓ Found: {fullPath}");
                }
                else
                {
                    Debug.LogError($"✗ Missing: {fullPath}");
                    allFilesExist = false;
                }
            }
            
            if (allFilesExist)
            {
                Debug.Log("[BalanceTestSetup] All CSV files found!");
            }
            else
            {
                Debug.LogError("[BalanceTestSetup] Some CSV files are missing. Please check the file paths.");
            }
        }
        
        [MenuItem("Tools/Balance Test/Create Test Managers")]
        public static void CreateTestManagers()
        {
            // Create a GameObject to hold all managers if they don't exist
            GameObject managers = GameObject.Find("[Managers]");
            if (managers == null)
            {
                managers = new GameObject("[Managers]");
            }
            
            // Check for required singleton managers
            if (GameObject.FindObjectOfType<GameDevClicker.Core.Managers.GameManager>() == null)
            {
                GameObject gameManager = new GameObject("GameManager");
                gameManager.AddComponent<GameDevClicker.Core.Managers.GameManager>();
                gameManager.transform.SetParent(managers.transform);
                Debug.Log("[BalanceTestSetup] Created GameManager");
            }
            
            if (GameObject.FindObjectOfType<GameDevClicker.Core.Managers.SaveManager>() == null)
            {
                GameObject saveManager = new GameObject("SaveManager");
                saveManager.AddComponent<GameDevClicker.Core.Managers.SaveManager>();
                saveManager.transform.SetParent(managers.transform);
                Debug.Log("[BalanceTestSetup] Created SaveManager");
            }
            
            if (GameObject.FindObjectOfType<GameDevClicker.Core.Managers.BalanceManager>() == null)
            {
                GameObject balanceManager = new GameObject("BalanceManager");
                balanceManager.AddComponent<GameDevClicker.Core.Managers.BalanceManager>();
                balanceManager.transform.SetParent(managers.transform);
                Debug.Log("[BalanceTestSetup] Created BalanceManager");
            }
            
            Debug.Log("[BalanceTestSetup] All required managers are present");
        }
    }
}