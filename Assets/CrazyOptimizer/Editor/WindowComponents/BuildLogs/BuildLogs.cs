using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using CrazyGames;
using CrazyGames.TreeLib;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace CrazyOptimizer.Editor.WindowComponents.BuildLogs
{
    public class BuildLogs
    {
        private static MultiColumnHeaderState _multiColumnHeaderState;
        private static BuildLogTree _buildLogTree;
        private static bool _isAnalyzing;
        private static string _errorMessage;
        private static bool _includeFilesFromPackages;

        // Cached error label style — avoids allocating a new GUIStyle every OnGUI frame
        private static GUIStyle _errorLabelStyle;

        private static GUIStyle ErrorLabelStyle =>
            _errorLabelStyle ??= new GUIStyle
            {
                wordWrap = true,
                normal = { textColor = Color.red }
            };

        public static void RenderGUI()
        {
            var rect = EditorGUILayout.BeginVertical(GUILayout.MinHeight(300));
            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginVertical();
            GUILayout.Label("Press \"Analyze build logs\" button, but be sure the project was built at least once on this machine.");
            GUILayout.Label("Press it again when you need to refresh the data.");
            EditorGUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            _buildLogTree?.OnGUI(rect);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(_isAnalyzing ? "Analyzing..." : "Analyze build logs", GUILayout.Width(200)))
            {
                AnalyzeBuildLogs();
            }

            var originalValue = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 160;
            _includeFilesFromPackages = EditorGUILayout.Toggle("Include files from Packages", _includeFilesFromPackages);
            EditorGUIUtility.labelWidth = originalValue;
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Open Editor.log", GUILayout.Width(200)))
            {
                var processName = Application.platform == RuntimePlatform.OSXEditor ? "open" : "notepad.exe";
                Process.Start(processName, GetEditorLogPath());
            }

            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(_errorMessage))
                GUILayout.Label(_errorMessage, ErrorLabelStyle);

            EditorGUILayout.Space(5);

            GUILayout.Label(
                "This utility analyzes the Build Report from the Editor.log file. It will display all the files included in your final build, and the memory they occupy. You can use this utility to detect more opportunities to decrease the final build size. There may be textures that still occupy a lot of memory, uncompressed sounds, or stuff forgotten in the Resources folders that gets included in the build.",
                EditorStyles.wordWrappedLabel);
        }

        private static string GetEditorLogPath()
        {
            if (Application.platform == RuntimePlatform.OSXEditor)
            {
                var personalPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                return Path.Combine(personalPath, "Library", "Logs", "Unity", "Editor.log");
            }

            var localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localAppDataPath, "Unity", "Editor", "Editor.log");
        }

        // Return the contents of the Editor.log file.
        // The original file may be locked by Unity, so we copy it first.
        private static string GetEditorLog()
        {
            var originalEditorLogPath = GetEditorLogPath();

            // Place the temp copy next to the original (same directory, cross-platform safe)
            var tempEditorLogPath = Path.Combine(
                Path.GetDirectoryName(originalEditorLogPath)!,
                "EditorCrazyGamesTemp.log");

            File.Copy(originalEditorLogPath, tempEditorLogPath, overwrite: true);
            try
            {
                return File.ReadAllText(tempEditorLogPath);
            }
            finally
            {
                // Always clean up the temp file even if reading fails
                File.Delete(tempEditorLogPath);
            }
        }

        static void AnalyzeBuildLogs()
        {
            _isAnalyzing = true;
            _errorMessage = string.Empty;
            OptimizerWindow.EditorWindowInstance?.Repaint();

            try
            {
                string editorLogStr;
                try
                {
                    editorLogStr = GetEditorLog();
                }
                catch (Exception e)
                {
                    _errorMessage = "Failed to read Editor.log file, check console for more details.";
                    Debug.LogError(e);
                    return;
                }

                const string buildReportHeader = "Build Report";
                const string separator = "----------------------";
                var splitMarker = $"{separator}{Environment.NewLine}{buildReportHeader}{Environment.NewLine}";
                var buildReportStr = editorLogStr
                    .Split(new[] { splitMarker }, StringSplitOptions.None)
                    .Last();

                if (!buildReportStr.StartsWith("Uncompressed usage by category"))
                {
                    _errorMessage =
                        "Failed to find Build Report in the Editor.log file. Please be sure the project was recently built on this machine. If the error persists, feel free to contact us.";
                    return;
                }

                // Use an index pointer instead of repeated RemoveAt(0) to avoid O(n²) list shifting
                var buildReportLines = buildReportStr.Split(
                    new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

                int lineIndex = 0;

                // Skip lines until we reach the asset list header
                while (lineIndex < buildReportLines.Length &&
                       !buildReportLines[lineIndex].StartsWith("Used Assets and files from the Resources folder"))
                {
                    lineIndex++;
                }

                lineIndex++; // skip the header line itself

                // Build the tree from the asset lines
                var treeElements = new List<BuildLogTreeItem>();
                var idIncrement = 0;
                treeElements.Add(new BuildLogTreeItem("Root", -1, idIncrement, 0, "", 0, ""));

                while (lineIndex < buildReportLines.Length &&
                       !buildReportLines[lineIndex].StartsWith("------------"))
                {
                    var line = buildReportLines[lineIndex++];
                    // Line format: " 0.1 kb\t 0.0% Packages/com.unity.timeline/..."

                    var splitLine = line.Replace("\t", " ").Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                    if (splitLine.Length < 4)
                        continue; // malformed line — skip gracefully

                    if (!float.TryParse(splitLine[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var size))
                        continue;

                    var sizeUnit = splitLine[1];

                    if (!float.TryParse(splitLine[2].Replace("%", ""), NumberStyles.Float,
                            CultureInfo.InvariantCulture, out var sizePercentage))
                        continue;

                    // Everything after the first 3 tokens is the path
                    var path = string.Join(" ", splitLine, 3, splitLine.Length - 3);

                    if (path.StartsWith("Packages/") && !_includeFilesFromPackages)
                        continue;

                    treeElements.Add(new BuildLogTreeItem("BuildLogLine", 0, ++idIncrement, size, sizeUnit,
                        sizePercentage, path));
                }

                var treeModel = new TreeModel<BuildLogTreeItem>(treeElements);
                var treeViewState = new TreeViewState();

                // Preserve existing column widths/sort state across re-analyses; only create once
                _multiColumnHeaderState ??= new MultiColumnHeaderState(new[]
                {
                    // When adding a column, update SortIfNeeded and CellGUI too
                    new MultiColumnHeaderState.Column { headerContent = new GUIContent("Size"),   width = 80,  minWidth = 60,  canSort = true },
                    new MultiColumnHeaderState.Column { headerContent = new GUIContent("Size %"), width = 60,  minWidth = 40,  canSort = true },
                    new MultiColumnHeaderState.Column { headerContent = new GUIContent("Path"),   width = 300, minWidth = 200, canSort = true },
                });

                _buildLogTree = new BuildLogTree(treeViewState, new MultiColumnHeader(_multiColumnHeaderState), treeModel);
            }
            finally
            {
                // Always reset the analyzing flag — even on early returns due to parse errors
                _isAnalyzing = false;
                OptimizerWindow.EditorWindowInstance?.Repaint();
            }
        }
    }
}