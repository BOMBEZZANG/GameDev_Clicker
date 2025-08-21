using UnityEngine;
using UnityEditor;
using GameDevClicker.Game.Effects;

namespace GameDevClicker.Editor
{
    /// <summary>
    /// Editor utility to help set up click sound mappings
    /// </summary>
    public class ClickSoundSetup : EditorWindow
    {
        private ClickSoundManager soundManager;
        
        [MenuItem("GameDev Clicker/Sound Setup/Click Sound Manager")]
        public static void ShowWindow()
        {
            GetWindow<ClickSoundSetup>("Click Sound Setup");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("Click Sound Manager Setup", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            // Find or create ClickSoundManager
            if (soundManager == null)
            {
                soundManager = FindObjectOfType<ClickSoundManager>();
            }
            
            if (soundManager == null)
            {
                EditorGUILayout.HelpBox("No ClickSoundManager found in the scene.", MessageType.Warning);
                if (GUILayout.Button("Create ClickSoundManager"))
                {
                    CreateClickSoundManager();
                }
                return;
            }
            
            EditorGUILayout.ObjectField("Sound Manager", soundManager, typeof(ClickSoundManager), true);
            GUILayout.Space(10);
            
            // Quick setup buttons
            GUILayout.Label("Quick Setup", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Setup CH1_1 (Keyboard Typing)"))
            {
                SetupKeyboardTypingSound();
            }
            
            if (GUILayout.Button("Add New Stage Sound"))
            {
                // This would open a more detailed setup window
                EditorUtility.DisplayDialog("Add New Stage", 
                    "Use the Inspector on the ClickSoundManager to add new stage sounds, or use the AddStageSound method in code.", 
                    "OK");
            }
            
            GUILayout.Space(10);
            
            // Info
            EditorGUILayout.HelpBox(
                "Instructions:\n" +
                "1. Create or import keyboard typing sound files\n" +
                "2. Assign them to Stage 1 in the Sound Manager\n" +
                "3. Characters with stage=1 will play these sounds when clicked\n\n" +
                "For CH1_1 character, set characterStage = 1 in CharacterClickHandler",
                MessageType.Info);
                
            if (GUILayout.Button("Select Sound Manager"))
            {
                Selection.activeObject = soundManager;
                EditorGUIUtility.PingObject(soundManager);
            }
        }
        
        private void CreateClickSoundManager()
        {
            GameObject managerObj = new GameObject("ClickSoundManager");
            soundManager = managerObj.AddComponent<ClickSoundManager>();
            
            // Set up some default values
            var serializedObject = new SerializedObject(soundManager);
            serializedObject.FindProperty("volume").floatValue = 0.7f;
            serializedObject.FindProperty("randomizePitch").boolValue = true;
            serializedObject.ApplyModifiedProperties();
            
            Debug.Log("[ClickSoundSetup] Created ClickSoundManager GameObject in scene");
            
            Selection.activeObject = managerObj;
            EditorGUIUtility.PingObject(managerObj);
        }
        
        private void SetupKeyboardTypingSound()
        {
            if (soundManager == null) return;
            
            // This is a placeholder - in practice, you'd load actual keyboard typing sounds
            EditorUtility.DisplayDialog("Keyboard Typing Setup", 
                "To complete the setup:\n\n" +
                "1. Import keyboard typing sound files (.wav, .mp3, etc.)\n" +
                "2. Select the ClickSoundManager in the scene\n" +
                "3. In the Inspector, expand 'Stage Sound Mapping'\n" +
                "4. Add a new element with:\n" +
                "   - Stage: 1\n" +
                "   - Character Description: 'Keyboard Typing (CH1_1)'\n" +
                "   - Sound Clips: Your keyboard typing sound files\n" +
                "   - Volume Multiplier: 1.0\n\n" +
                "The system will automatically play these sounds when CH1_1 character is clicked!",
                "Got it!");
                
            // Focus on the sound manager for easy setup
            Selection.activeObject = soundManager;
            EditorGUIUtility.PingObject(soundManager);
        }
    }
    
    /// <summary>
    /// Custom inspector for ClickSoundManager to make it easier to use
    /// </summary>
    [CustomEditor(typeof(ClickSoundManager))]
    public class ClickSoundManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            GUILayout.Space(10);
            
            ClickSoundManager manager = (ClickSoundManager)target;
            
            GUILayout.Label("Testing", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Test Stage 1 Sound (CH1_1)"))
            {
                if (Application.isPlaying)
                {
                    manager.PlayClickSound(1);
                }
                else
                {
                    EditorUtility.DisplayDialog("Test Sound", "Enter Play Mode to test sounds.", "OK");
                }
            }
            
            GUILayout.Space(5);
            
            if (GUILayout.Button("Open Click Sound Setup Window"))
            {
                ClickSoundSetup.ShowWindow();
            }
        }
    }
}