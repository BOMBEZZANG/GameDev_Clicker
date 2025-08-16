using UnityEngine;
using UnityEditor;
using System.IO;
using GameDevClicker.Game.Managers;

public class HTMLAssetProcessor : AssetPostprocessor
{
    private static readonly string HTML_SOURCE_PATH = @"C:\Projects\AssetCreate\project\input\html";
    private static readonly string STREAMING_ASSETS_PATH = "Assets/StreamingAssets/HTML";
    
    [MenuItem("Tools/HTML Background/Copy HTML Files from Source")]
    public static void CopyHTMLFilesFromSource()
    {
        if (!Directory.Exists(HTML_SOURCE_PATH))
        {
            Debug.LogError($"Source HTML directory not found: {HTML_SOURCE_PATH}");
            return;
        }
        
        // Ensure StreamingAssets/HTML directory exists
        if (!Directory.Exists(STREAMING_ASSETS_PATH))
        {
            Directory.CreateDirectory(STREAMING_ASSETS_PATH);
        }
        
        string[] htmlFiles = Directory.GetFiles(HTML_SOURCE_PATH, "*.html");
        int copiedCount = 0;
        
        foreach (string sourceFile in htmlFiles)
        {
            string fileName = Path.GetFileName(sourceFile);
            string destPath = Path.Combine(STREAMING_ASSETS_PATH, fileName);
            
            try
            {
                File.Copy(sourceFile, destPath, true);
                copiedCount++;
                Debug.Log($"Copied: {fileName}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to copy {fileName}: {e.Message}");
            }
        }
        
        AssetDatabase.Refresh();
        Debug.Log($"HTML file copy completed. {copiedCount}/{htmlFiles.Length} files copied to StreamingAssets.");
    }
    
    [MenuItem("Tools/HTML Background/Setup HTML Background Manager")]
    public static void SetupHTMLBackgroundManager()
    {
        // Find or create HTMLBackgroundManager in the scene
        HTMLBackgroundManager manager = Object.FindObjectOfType<HTMLBackgroundManager>();
        
        if (manager == null)
        {
            GameObject managerGO = new GameObject("HTMLBackgroundManager");
            manager = managerGO.AddComponent<HTMLBackgroundManager>();
            Debug.Log("Created HTMLBackgroundManager in scene");
        }
        
        // Auto-configure stage data based on available HTML files
        if (Directory.Exists(STREAMING_ASSETS_PATH))
        {
            string[] htmlFiles = Directory.GetFiles(STREAMING_ASSETS_PATH, "*.html");
            manager.stageData.Clear();
            
            foreach (string htmlFile in htmlFiles)
            {
                string fileName = Path.GetFileName(htmlFile);
                string stageName = Path.GetFileNameWithoutExtension(fileName);
                
                HTMLBackgroundManager.StageHTMLData stageData = new HTMLBackgroundManager.StageHTMLData
                {
                    stageName = stageName,
                    htmlFileName = fileName,
                    clickZones = new System.Collections.Generic.List<HTMLBackgroundManager.ClickZoneData>()
                };
                
                // Add some default click zones for common elements
                AddDefaultClickZones(stageData, stageName);
                
                manager.stageData.Add(stageData);
            }
            
            EditorUtility.SetDirty(manager);
            Debug.Log($"Configured {htmlFiles.Length} stages for HTMLBackgroundManager");
        }
    }
    
    private static void AddDefaultClickZones(HTMLBackgroundManager.StageHTMLData stageData, string stageName)
    {
        // Add default click zones based on stage type
        switch (stageName.ToLower())
        {
            case "stage1_indie_room":
                AddClickZone(stageData, "Laptop", new Vector2(0.35f, 0.4f), new Vector2(0.65f, 0.7f));
                AddClickZone(stageData, "Coffee", new Vector2(0.65f, 0.4f), new Vector2(0.75f, 0.6f));
                AddClickZone(stageData, "Cat", new Vector2(0.75f, 0.6f), new Vector2(0.9f, 0.8f));
                AddClickZone(stageData, "Bookshelf", new Vector2(0.8f, 0.2f), new Vector2(0.95f, 0.6f));
                break;
                
            case "stage2_mobile_dev":
                AddClickZone(stageData, "Phone", new Vector2(0.4f, 0.4f), new Vector2(0.6f, 0.7f));
                AddClickZone(stageData, "Tablet", new Vector2(0.2f, 0.3f), new Vector2(0.4f, 0.6f));
                break;
                
            case "stage3_pc_game_dev":
                AddClickZone(stageData, "PC", new Vector2(0.3f, 0.2f), new Vector2(0.7f, 0.8f));
                AddClickZone(stageData, "Monitor", new Vector2(0.25f, 0.1f), new Vector2(0.75f, 0.5f));
                break;
                
            default:
                // Add generic center click zone
                AddClickZone(stageData, "Center", new Vector2(0.3f, 0.3f), new Vector2(0.7f, 0.7f));
                break;
        }
    }
    
    private static void AddClickZone(HTMLBackgroundManager.StageHTMLData stageData, string zoneName, Vector2 topLeft, Vector2 bottomRight)
    {
        HTMLBackgroundManager.ClickZoneData clickZone = new HTMLBackgroundManager.ClickZoneData
        {
            zoneName = zoneName,
            topLeft = topLeft,
            bottomRight = bottomRight,
            onClickEvent = new UnityEngine.Events.UnityEvent()
        };
        
        stageData.clickZones.Add(clickZone);
    }
    
    [MenuItem("Tools/HTML Background/Open StreamingAssets Folder")]
    public static void OpenStreamingAssetsFolder()
    {
        string fullPath = Path.Combine(Application.dataPath, "StreamingAssets", "HTML");
        if (Directory.Exists(fullPath))
        {
            EditorUtility.RevealInFinder(fullPath);
        }
        else
        {
            Debug.LogWarning("StreamingAssets/HTML folder doesn't exist yet. Use 'Copy HTML Files from Source' first.");
        }
    }
}