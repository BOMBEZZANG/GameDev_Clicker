using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace GameDevClicker.Editor
{
    /// <summary>
    /// Automatically captures and saves debug logs to a text file
    /// </summary>
    public class DebugLogExporter : EditorWindow
    {
        private static List<LogEntry> logEntries = new List<LogEntry>();
        private static bool isCapturing = false;
        private Vector2 scrollPosition;
        private bool autoSave = true;
        private bool captureInfo = true;
        private bool captureWarnings = true;
        private bool captureErrors = true;
        private string filterKeyword = "";
        
        private class LogEntry
        {
            public string message;
            public string stackTrace;
            public LogType type;
            public DateTime timestamp;
            
            public LogEntry(string message, string stackTrace, LogType type)
            {
                this.message = message;
                this.stackTrace = stackTrace;
                this.type = type;
                this.timestamp = DateTime.Now;
            }
        }
        
        [MenuItem("Game Tools/Debug Log Exporter")]
        public static void ShowWindow()
        {
            GetWindow<DebugLogExporter>("Debug Log Exporter");
        }
        
        private void OnEnable()
        {
            // Subscribe to Unity's log message received event
            Application.logMessageReceived += HandleLog;
            isCapturing = true;
        }
        
        private void OnDisable()
        {
            // Unsubscribe when window is closed
            Application.logMessageReceived -= HandleLog;
            isCapturing = false;
            
            // Auto-save on close if enabled
            if (autoSave && logEntries.Count > 0)
            {
                SaveLogsToFile();
            }
        }
        
        private void OnGUI()
        {
            GUILayout.Label("Debug Log Exporter", EditorStyles.boldLabel);
            
            // Status
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"Status: {(isCapturing ? "📝 Capturing" : "⏸️ Paused")}");
            GUILayout.Label($"Logs: {logEntries.Count}");
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(10);
            
            // Settings
            GUILayout.Label("Capture Settings", EditorStyles.boldLabel);
            captureInfo = EditorGUILayout.Toggle("Capture Info", captureInfo);
            captureWarnings = EditorGUILayout.Toggle("Capture Warnings", captureWarnings);
            captureErrors = EditorGUILayout.Toggle("Capture Errors", captureErrors);
            autoSave = EditorGUILayout.Toggle("Auto-Save on Close", autoSave);
            
            GUILayout.Space(10);
            
            // Filter
            GUILayout.Label("Filter", EditorStyles.boldLabel);
            filterKeyword = EditorGUILayout.TextField("Keyword Filter:", filterKeyword);
            
            GUILayout.Space(10);
            
            // Actions
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Save to File", GUILayout.Height(30)))
            {
                SaveLogsToFile();
            }
            
            if (GUILayout.Button("Save Filtered", GUILayout.Height(30)))
            {
                SaveFilteredLogsToFile();
            }
            
            if (GUILayout.Button("Clear Logs", GUILayout.Height(30)))
            {
                ClearLogs();
            }
            
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(5);
            
            // Quick save buttons
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Save Upgrade Logs"))
            {
                SaveSpecificLogs("Upgrade", "upgrade_debug");
            }
            
            if (GUILayout.Button("Save GamePresenter Logs"))
            {
                SaveSpecificLogs("[GamePresenter]", "presenter_debug");
            }
            
            if (GUILayout.Button("Save All Errors"))
            {
                SaveErrorLogs();
            }
            
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(10);
            
            // Log preview
            GUILayout.Label("Log Preview (Recent 50):", EditorStyles.boldLabel);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(300));
            
            int startIndex = Mathf.Max(0, logEntries.Count - 50);
            for (int i = startIndex; i < logEntries.Count; i++)
            {
                var entry = logEntries[i];
                
                // Apply filter
                if (!string.IsNullOrEmpty(filterKeyword) && !entry.message.Contains(filterKeyword))
                    continue;
                
                // Set color based on log type
                GUI.color = GetLogColor(entry.type);
                EditorGUILayout.TextArea($"[{entry.timestamp:HH:mm:ss}] {entry.message}", GUILayout.ExpandWidth(true));
                GUI.color = Color.white;
            }
            
            EditorGUILayout.EndScrollView();
            
            // Status bar
            EditorGUILayout.HelpBox($"Logs are automatically saved to: {GetSaveDirectory()}", MessageType.Info);
        }
        
        private static void HandleLog(string logString, string stackTrace, LogType type)
        {
            if (!isCapturing) return;
            
            var window = GetWindow<DebugLogExporter>(false, "", false);
            
            // Check if we should capture this type
            if (type == LogType.Log && !window.captureInfo) return;
            if (type == LogType.Warning && !window.captureWarnings) return;
            if (type == LogType.Error && !window.captureErrors) return;
            
            logEntries.Add(new LogEntry(logString, stackTrace, type));
            
            // Auto-save every 100 logs
            if (logEntries.Count % 100 == 0 && window.autoSave)
            {
                window.SaveLogsToFile(true); // Silent save
            }
        }
        
        private void SaveLogsToFile(bool silent = false)
        {
            if (logEntries.Count == 0)
            {
                if (!silent) EditorUtility.DisplayDialog("No Logs", "No logs to save!", "OK");
                return;
            }
            
            string directory = GetSaveDirectory();
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string fileName = $"debug_log_{timestamp}.txt";
            string filePath = Path.Combine(directory, fileName);
            
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=== DEBUG LOG EXPORT ===");
            sb.AppendLine($"Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Total Entries: {logEntries.Count}");
            sb.AppendLine($"Unity Version: {Application.unityVersion}");
            sb.AppendLine("=" + new string('=', 50));
            sb.AppendLine();
            
            foreach (var entry in logEntries)
            {
                sb.AppendLine($"[{entry.timestamp:HH:mm:ss.fff}] [{entry.type}]");
                sb.AppendLine(entry.message);
                
                if (entry.type == LogType.Error || entry.type == LogType.Exception)
                {
                    if (!string.IsNullOrEmpty(entry.stackTrace))
                    {
                        sb.AppendLine("Stack Trace:");
                        sb.AppendLine(entry.stackTrace);
                    }
                }
                
                sb.AppendLine("-" + new string('-', 40));
            }
            
            File.WriteAllText(filePath, sb.ToString());
            
            if (!silent)
            {
                EditorUtility.DisplayDialog("Log Saved", $"Debug log saved to:\n{filePath}", "OK");
                EditorUtility.RevealInFinder(filePath);
            }
            
            Debug.Log($"[DebugLogExporter] Saved {logEntries.Count} log entries to: {filePath}");
        }
        
        private void SaveFilteredLogsToFile()
        {
            if (string.IsNullOrEmpty(filterKeyword))
            {
                EditorUtility.DisplayDialog("No Filter", "Please enter a filter keyword!", "OK");
                return;
            }
            
            var filteredLogs = logEntries.FindAll(e => e.message.Contains(filterKeyword));
            
            if (filteredLogs.Count == 0)
            {
                EditorUtility.DisplayDialog("No Matches", $"No logs containing '{filterKeyword}' found!", "OK");
                return;
            }
            
            string directory = GetSaveDirectory();
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string fileName = $"debug_log_{filterKeyword}_{timestamp}.txt";
            string filePath = Path.Combine(directory, fileName);
            
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"=== FILTERED DEBUG LOG (Keyword: {filterKeyword}) ===");
            sb.AppendLine($"Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Filtered Entries: {filteredLogs.Count} / {logEntries.Count}");
            sb.AppendLine("=" + new string('=', 50));
            sb.AppendLine();
            
            foreach (var entry in filteredLogs)
            {
                sb.AppendLine($"[{entry.timestamp:HH:mm:ss.fff}] [{entry.type}]");
                sb.AppendLine(entry.message);
                sb.AppendLine("-" + new string('-', 40));
            }
            
            File.WriteAllText(filePath, sb.ToString());
            
            EditorUtility.DisplayDialog("Filtered Log Saved", $"Saved {filteredLogs.Count} filtered entries to:\n{filePath}", "OK");
            EditorUtility.RevealInFinder(filePath);
        }
        
        private void SaveSpecificLogs(string keyword, string filePrefix)
        {
            var specificLogs = logEntries.FindAll(e => e.message.Contains(keyword));
            
            if (specificLogs.Count == 0)
            {
                EditorUtility.DisplayDialog("No Logs", $"No logs containing '{keyword}' found!", "OK");
                return;
            }
            
            string directory = GetSaveDirectory();
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string fileName = $"{filePrefix}_{timestamp}.txt";
            string filePath = Path.Combine(directory, fileName);
            
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"=== {keyword.ToUpper()} DEBUG LOG ===");
            sb.AppendLine($"Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Entries: {specificLogs.Count}");
            sb.AppendLine("=" + new string('=', 50));
            sb.AppendLine();
            
            foreach (var entry in specificLogs)
            {
                sb.AppendLine($"[{entry.timestamp:HH:mm:ss.fff}]");
                sb.AppendLine(entry.message);
                sb.AppendLine();
            }
            
            File.WriteAllText(filePath, sb.ToString());
            
            EditorUtility.DisplayDialog("Log Saved", $"Saved {specificLogs.Count} entries to:\n{filePath}", "OK");
            EditorUtility.RevealInFinder(filePath);
        }
        
        private void SaveErrorLogs()
        {
            var errorLogs = logEntries.FindAll(e => e.type == LogType.Error || e.type == LogType.Exception);
            
            if (errorLogs.Count == 0)
            {
                EditorUtility.DisplayDialog("No Errors", "No error logs found!", "OK");
                return;
            }
            
            string directory = GetSaveDirectory();
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string fileName = $"error_log_{timestamp}.txt";
            string filePath = Path.Combine(directory, fileName);
            
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=== ERROR LOG ===");
            sb.AppendLine($"Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Error Count: {errorLogs.Count}");
            sb.AppendLine("=" + new string('=', 50));
            sb.AppendLine();
            
            foreach (var entry in errorLogs)
            {
                sb.AppendLine($"[{entry.timestamp:HH:mm:ss.fff}] [{entry.type}]");
                sb.AppendLine(entry.message);
                if (!string.IsNullOrEmpty(entry.stackTrace))
                {
                    sb.AppendLine("Stack Trace:");
                    sb.AppendLine(entry.stackTrace);
                }
                sb.AppendLine("=" + new string('=', 50));
            }
            
            File.WriteAllText(filePath, sb.ToString());
            
            EditorUtility.DisplayDialog("Error Log Saved", $"Saved {errorLogs.Count} errors to:\n{filePath}", "OK");
            EditorUtility.RevealInFinder(filePath);
        }
        
        private void ClearLogs()
        {
            if (EditorUtility.DisplayDialog("Clear Logs", $"Clear all {logEntries.Count} log entries?", "Clear", "Cancel"))
            {
                logEntries.Clear();
                Debug.Log("[DebugLogExporter] Logs cleared");
            }
        }
        
        private string GetSaveDirectory()
        {
            return Path.Combine(Application.dataPath, "..", "DebugLogs");
        }
        
        private Color GetLogColor(LogType type)
        {
            switch (type)
            {
                case LogType.Error:
                case LogType.Exception:
                    return new Color(1f, 0.3f, 0.3f);
                case LogType.Warning:
                    return new Color(1f, 0.8f, 0.3f);
                default:
                    return Color.white;
            }
        }
    }
}