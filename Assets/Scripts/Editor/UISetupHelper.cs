using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GameDevClicker.Game.Views;

namespace GameDevClicker.Editor
{
    /// <summary>
    /// Helper tool to quickly set up UI connections for the upgrade system
    /// </summary>
    public class UISetupHelper : EditorWindow
    {
        [MenuItem("Game Tools/UI Setup Helper")]
        public static void ShowWindow()
        {
            GetWindow<UISetupHelper>("UI Setup Helper");
        }

        private void OnGUI()
        {
            GUILayout.Label("UI Setup Helper", EditorStyles.boldLabel);
            GUILayout.Label("Quick fixes for common UI setup issues", EditorStyles.helpBox);
            GUILayout.Space(10);

            EditorGUILayout.LabelField("Common Issues & Fixes:", EditorStyles.boldLabel);
            
            GUILayout.Space(5);
            
            if (GUILayout.Button("Fix Tab Button Texts"))
            {
                FixTabButtonTexts();
            }
            EditorGUILayout.HelpBox("Sets Skills/Equipment/Team text on tab buttons", MessageType.Info);
            
            GUILayout.Space(5);
            
            if (GUILayout.Button("Find & Assign ScrollView Content"))
            {
                FindAndAssignScrollViewContent();
            }
            EditorGUILayout.HelpBox("Automatically finds ScrollView Content and assigns as Upgrade List Parent", MessageType.Info);
            
            GUILayout.Space(5);
            
            if (GUILayout.Button("Create Basic Upgrade Item Prefab"))
            {
                CreateBasicUpgradeItemPrefab();
            }
            EditorGUILayout.HelpBox("Creates a basic UpgradeItemUI prefab if one doesn't exist", MessageType.Info);
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("Auto-Setup Everything (Attempt)", GUILayout.Height(30)))
            {
                AutoSetupEverything();
            }
            EditorGUILayout.HelpBox("Attempts to automatically set up all UI connections. Check console for results.", MessageType.Warning);
        }

        private void FixTabButtonTexts()
        {
            var gameViewUI = FindObjectOfType<GameViewUI>();
            if (gameViewUI == null)
            {
                Debug.LogError("[UISetupHelper] GameViewUI not found in scene!");
                return;
            }

            bool wasFixed = false;

            // Use reflection to access private fields
            var skillsButton = GetPrivateField(gameViewUI, "skillsTabButton") as Button;
            var equipmentButton = GetPrivateField(gameViewUI, "equipmentTabButton") as Button;
            var teamButton = GetPrivateField(gameViewUI, "teamTabButton") as Button;

            if (skillsButton != null)
            {
                var text = skillsButton.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null) { text.text = "Skills"; wasFixed = true; }
            }

            if (equipmentButton != null)
            {
                var text = equipmentButton.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null) { text.text = "Equipment"; wasFixed = true; }
            }

            if (teamButton != null)
            {
                var text = teamButton.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null) { text.text = "Team"; wasFixed = true; }
            }

            if (wasFixed)
            {
                EditorUtility.SetDirty(gameViewUI);
                Debug.Log("[UISetupHelper] ✅ Fixed tab button texts");
            }
            else
            {
                Debug.LogWarning("[UISetupHelper] ⚠️ Could not find tab buttons or text components");
            }
        }

        private void FindAndAssignScrollViewContent()
        {
            var gameViewUI = FindObjectOfType<GameViewUI>();
            if (gameViewUI == null)
            {
                Debug.LogError("[UISetupHelper] GameViewUI not found in scene!");
                return;
            }

            // Find ScrollViews in the scene
            ScrollRect[] scrollViews = FindObjectsOfType<ScrollRect>();
            Transform bestContent = null;

            foreach (var scrollView in scrollViews)
            {
                if (scrollView.content != null)
                {
                    // Look for Content that might be for upgrades
                    if (scrollView.name.ToLower().Contains("upgrade") || 
                        scrollView.content.name.ToLower().Contains("upgrade") ||
                        scrollView.content.name.ToLower().Contains("content"))
                    {
                        bestContent = scrollView.content;
                        break;
                    }
                }
            }

            if (bestContent != null)
            {
                // Use reflection to set the private field
                SetPrivateField(gameViewUI, "upgradeListParent", bestContent);
                EditorUtility.SetDirty(gameViewUI);
                Debug.Log($"[UISetupHelper] ✅ Assigned ScrollView Content: {bestContent.name}");
                
                // Make sure it has a layout component
                if (bestContent.GetComponent<VerticalLayoutGroup>() == null && 
                    bestContent.GetComponent<GridLayoutGroup>() == null)
                {
                    bestContent.gameObject.AddComponent<VerticalLayoutGroup>();
                    Debug.Log("[UISetupHelper] ✅ Added VerticalLayoutGroup to Content");
                }
            }
            else
            {
                Debug.LogWarning("[UISetupHelper] ⚠️ Could not find suitable ScrollView Content. Create a ScrollView with Content first.");
            }
        }

        private void CreateBasicUpgradeItemPrefab()
        {
            // Check if prefab already exists
            string[] existingPrefabs = AssetDatabase.FindAssets("t:Prefab UpgradeItem");
            if (existingPrefabs.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(existingPrefabs[0]);
                var existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (existingPrefab.GetComponent<UpgradeItemUI>() != null)
                {
                    Debug.Log($"[UISetupHelper] ✅ UpgradeItemUI prefab already exists: {existingPrefab.name}");
                    return;
                }
            }

            // Create new prefab
            GameObject prefabRoot = new GameObject("UpgradeItemUI_Prefab");
            
            // Add UpgradeItemUI component
            var upgradeItemUI = prefabRoot.AddComponent<UpgradeItemUI>();
            
            // Create UI structure
            var canvas = FindObjectOfType<Canvas>();
            prefabRoot.transform.SetParent(canvas?.transform, false);
            
            // Add RectTransform and basic layout
            var rectTransform = prefabRoot.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(300, 80);
            
            // Background Image
            var background = prefabRoot.AddComponent<Image>();
            background.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            
            // Create Button
            GameObject buttonObj = new GameObject("UpgradeButton");
            buttonObj.transform.SetParent(prefabRoot.transform, false);
            var button = buttonObj.AddComponent<Button>();
            var buttonRect = buttonObj.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.7f, 0.1f);
            buttonRect.anchorMax = new Vector2(0.95f, 0.9f);
            buttonRect.offsetMin = buttonRect.offsetMax = Vector2.zero;
            
            // Button background
            var buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = new Color(0.6f, 0.15f, 0.69f); // Purple
            
            // Button text
            GameObject buttonTextObj = new GameObject("ButtonText");
            buttonTextObj.transform.SetParent(buttonObj.transform, false);
            var buttonText = buttonTextObj.AddComponent<TextMeshProUGUI>();
            buttonText.text = "Buy";
            buttonText.fontSize = 14;
            buttonText.alignment = TextAlignmentOptions.Center;
            buttonText.color = Color.white;
            var buttonTextRect = buttonTextObj.GetComponent<RectTransform>();
            buttonTextRect.anchorMin = Vector2.zero;
            buttonTextRect.anchorMax = Vector2.one;
            buttonTextRect.offsetMin = buttonTextRect.offsetMax = Vector2.zero;
            
            // Name text
            GameObject nameObj = new GameObject("NameText");
            nameObj.transform.SetParent(prefabRoot.transform, false);
            var nameText = nameObj.AddComponent<TextMeshProUGUI>();
            nameText.text = "Upgrade Name";
            nameText.fontSize = 16;
            nameText.fontStyle = FontStyles.Bold;
            nameText.color = Color.white;
            var nameRect = nameObj.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0.05f, 0.5f);
            nameRect.anchorMax = new Vector2(0.65f, 0.9f);
            nameRect.offsetMin = nameRect.offsetMax = Vector2.zero;
            
            // Description text
            GameObject descObj = new GameObject("DescriptionText");
            descObj.transform.SetParent(prefabRoot.transform, false);
            var descText = descObj.AddComponent<TextMeshProUGUI>();
            descText.text = "Upgrade description here";
            descText.fontSize = 12;
            descText.color = new Color(0.8f, 0.8f, 0.8f);
            var descRect = descObj.GetComponent<RectTransform>();
            descRect.anchorMin = new Vector2(0.05f, 0.1f);
            descRect.anchorMax = new Vector2(0.65f, 0.5f);
            descRect.offsetMin = descRect.offsetMax = Vector2.zero;
            
            // Level text
            GameObject levelObj = new GameObject("LevelText");
            levelObj.transform.SetParent(prefabRoot.transform, false);
            var levelText = levelObj.AddComponent<TextMeshProUGUI>();
            levelText.text = "Level 1";
            levelText.fontSize = 10;
            levelText.color = Color.yellow;
            var levelRect = levelObj.GetComponent<RectTransform>();
            levelRect.anchorMin = new Vector2(0.7f, 0.7f);
            levelRect.anchorMax = new Vector2(0.95f, 0.9f);
            levelRect.offsetMin = levelRect.offsetMax = Vector2.zero;
            
            // Assign references using reflection
            SetPrivateField(upgradeItemUI, "upgradeNameText", nameText);
            SetPrivateField(upgradeItemUI, "upgradeDescText", descText);
            SetPrivateField(upgradeItemUI, "upgradeLevelText", levelText);
            SetPrivateField(upgradeItemUI, "upgradeButton", button);
            SetPrivateField(upgradeItemUI, "buttonText", buttonText);
            SetPrivateField(upgradeItemUI, "backgroundImage", background);
            
            // Save as prefab
            string prefabPath = "Assets/UI/Prefabs/UpgradeItemUI_Generated.prefab";
            string directory = System.IO.Path.GetDirectoryName(prefabPath);
            if (!AssetDatabase.IsValidFolder(directory))
            {
                System.IO.Directory.CreateDirectory(directory.Replace("Assets/", Application.dataPath + "/"));
                AssetDatabase.Refresh();
            }
            
            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            DestroyImmediate(prefabRoot);
            
            Debug.Log($"[UISetupHelper] ✅ Created UpgradeItemUI prefab: {prefabPath}");
            
            // Auto-assign to GameViewUI
            var gameViewUI = FindObjectOfType<GameViewUI>();
            if (gameViewUI != null)
            {
                SetPrivateField(gameViewUI, "upgradeItemPrefab", savedPrefab);
                EditorUtility.SetDirty(gameViewUI);
                Debug.Log("[UISetupHelper] ✅ Auto-assigned prefab to GameViewUI");
            }
        }

        private void AutoSetupEverything()
        {
            Debug.Log("[UISetupHelper] === Starting Auto-Setup ===");
            
            FixTabButtonTexts();
            FindAndAssignScrollViewContent();
            CreateBasicUpgradeItemPrefab();
            
            Debug.Log("[UISetupHelper] === Auto-Setup Complete ===");
            Debug.Log("[UISetupHelper] ℹ️ Test your game now - upgrade UI should work!");
        }

        private object GetPrivateField(object obj, string fieldName)
        {
            var field = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return field?.GetValue(obj);
        }

        private void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(obj, value);
        }
    }
}