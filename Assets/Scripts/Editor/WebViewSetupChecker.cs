using UnityEngine;
using UnityEditor;
using System.IO;
using GameDevClicker.Game.Managers;

[InitializeOnLoad]
public class WebViewSetupChecker
{
    static WebViewSetupChecker()
    {
        EditorApplication.delayCall += CheckWebViewSetup;
    }
    
    static void CheckWebViewSetup()
    {
        // Check if WebViewObject.cs exists in Plugins
        string webViewPath = Path.Combine(Application.dataPath, "Plugins", "WebViewObject.cs");
        
        if (!File.Exists(webViewPath))
        {
            Debug.LogWarning("[WebView Setup] WebViewObject.cs not found in Assets/Plugins/. Use 'Tools > HTML Background > Setup WebView Plugin' to install it.");
        }
        else
        {
            Debug.Log("[WebView Setup] WebViewObject.cs found. HTML Background system should work properly.");
        }
    }
    
    [MenuItem("Tools/HTML Background/Setup WebView Plugin")]
    public static void SetupWebViewPlugin()
    {
        string sourcePluginPath = Path.Combine(Application.dataPath, "..", "unity-webview", "plugins", "WebViewObject.cs");
        string destPluginPath = Path.Combine(Application.dataPath, "Plugins");
        
        // Create Plugins directory if it doesn't exist
        if (!Directory.Exists(destPluginPath))
        {
            Directory.CreateDirectory(destPluginPath);
        }
        
        // Copy WebViewObject.cs
        if (File.Exists(sourcePluginPath))
        {
            File.Copy(sourcePluginPath, Path.Combine(destPluginPath, "WebViewObject.cs"), true);
            
            // Copy platform-specific plugins
            string distPath = Path.Combine(Application.dataPath, "..", "unity-webview", "dist", "package-nofragment", "Assets", "Plugins");
            if (Directory.Exists(distPath))
            {
                CopyDirectory(distPath, destPluginPath);
            }
            
            AssetDatabase.Refresh();
            Debug.Log("WebView plugin setup completed!");
        }
        else
        {
            Debug.LogError($"WebViewObject.cs not found at: {sourcePluginPath}");
            Debug.LogError("Make sure the unity-webview folder exists in your project root.");
        }
    }
    
    [MenuItem("Tools/HTML Background/Check Setup Status")]
    public static void CheckSetupStatus()
    {
        Debug.Log("=== HTML Background Setup Status ===");
        
        // Check WebView Plugin
        string webViewPath = Path.Combine(Application.dataPath, "Plugins", "WebViewObject.cs");
        Debug.Log($"WebViewObject.cs: {(File.Exists(webViewPath) ? "✓ Found" : "✗ Missing")}");
        
        // Check StreamingAssets
        string streamingPath = Path.Combine(Application.dataPath, "StreamingAssets", "HTML");
        bool hasHTML = Directory.Exists(streamingPath) && Directory.GetFiles(streamingPath, "*.html").Length > 0;
        Debug.Log($"HTML Files: {(hasHTML ? "✓ Found" : "✗ Missing")}");
        
        // Check Manager Script
        bool hasManager = Object.FindObjectOfType<HTMLBackgroundManager>() != null;
        bool hasUniversalManager = Object.FindObjectOfType<GameDevClicker.Game.Managers.HTMLBackgroundManagerUniversal>() != null;
        Debug.Log($"HTMLBackgroundManager: {(hasManager ? "✓ Found in scene" : "✗ Not in scene")}");
        Debug.Log($"HTMLBackgroundManagerUniversal: {(hasUniversalManager ? "✓ Found in scene" : "✗ Not in scene")}");
        
        if (File.Exists(webViewPath) && hasHTML)
        {
            Debug.Log("✓ HTML Background system is ready to use!");
        }
        else
        {
            Debug.Log("⚠ Setup incomplete. Use the Tools menu to complete setup.");
        }
    }
    
    static void CopyDirectory(string sourceDir, string destDir)
    {
        try
        {
            // Copy all files
            foreach (string file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
            {
                string relativePath = Path.GetRelativePath(sourceDir, file);
                string destFile = Path.Combine(destDir, relativePath);
                
                // Create directory if it doesn't exist
                string destFileDir = Path.GetDirectoryName(destFile);
                if (!Directory.Exists(destFileDir))
                {
                    Directory.CreateDirectory(destFileDir);
                }
                
                File.Copy(file, destFile, true);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error copying directory: {e.Message}");
        }
    }
}