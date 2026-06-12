using System;
using UnityEditor;
using UnityEngine;

namespace CrazyGames.WindowComponents
{
    public static class ExportOptimizations
    {
        // Cached styles — GUIStyle objects were being allocated on every OnGUI frame.
        // Lazily initialized so EditorStyles is ready when first accessed.
        private static GUIStyle _okStyle;
        private static GUIStyle _failStyle;
        private static GUIStyle _infoStyle;
        private static GUIStyle _labelStyle;
        private static GUIStyle _additionalInfoStyle;

        private static GUIStyle OkStyle => _okStyle ??= new GUIStyle
        {
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.green }
        };

        private static GUIStyle FailStyle => _failStyle ??= new GUIStyle
        {
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.red }
        };

        private static GUIStyle InfoStyle => _infoStyle ??= new GUIStyle
        {
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.1618f, 0.5568f, 1f) }
        };

        // Label and AdditionalInfo styles reference EditorStyles.label which can change with
        // theme/skin, so we rebuild them if the text color has drifted.
        private static Color _lastLabelColor;

        private static GUIStyle LabelStyle
        {
            get
            {
                var currentColor = EditorStyles.label.normal.textColor;
                if (_labelStyle == null || _lastLabelColor != currentColor)
                {
                    _lastLabelColor = currentColor;
                    _labelStyle = new GUIStyle { normal = { textColor = currentColor } };
                    _additionalInfoStyle = new GUIStyle
                    {
                        fontSize = 11,
                        wordWrap = true,
                        normal = { textColor = currentColor }
                    };
                }
                return _labelStyle;
            }
        }

        private static GUIStyle AdditionalInfoStyle
        {
            get
            {
                _ = LabelStyle; // ensure rebuilt if color changed
                return _additionalInfoStyle;
            }
        }

        public static void RenderGUI()
        {
            if (typeof(PlayerSettings.WebGL).GetProperty("compressionFormat") != null)
            {
                var compressionOk = PlayerSettings.WebGL.compressionFormat == WebGLCompressionFormat.Brotli;
                RenderFixableItem("Brotli compression", compressionOk,
                    () => { PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli; });
            }

            if (typeof(PlayerSettings.WebGL).GetProperty("nameFilesAsHashes") != null)
            {
                var nameAsHashesOk = PlayerSettings.WebGL.nameFilesAsHashes;
                RenderFixableItem("Name file as hashes", nameAsHashesOk,
                    () => { PlayerSettings.WebGL.nameFilesAsHashes = true; });
            }

            if (typeof(PlayerSettings.WebGL).GetProperty("exceptionSupport") != null)
            {
                var exceptionsOk = PlayerSettings.WebGL.exceptionSupport ==
                                   WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
                RenderFixableItem("Exception support", exceptionsOk,
                    () => { PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly; },
                    "The \"Fix\" button sets exception support to \"Explicitly thrown exceptions only\". You can choose \"None\" in Player Settings for better performance, but first of all read about it on our developer documentation.");
            }

            if (typeof(PlayerSettings).GetProperty("stripEngineCode") != null)
            {
                var stripEngineCodeOk = PlayerSettings.stripEngineCode;
                RenderFixableItem("Strip engine code", stripEngineCodeOk,
                    () => { PlayerSettings.stripEngineCode = true; },
                    "To decrease the bundle size even more, you can select Medium or High stripping from Player Settings, but first of all read about them on our developer documentation.");
            }

#if UNITY_2020 || UNITY_2021 || UNITY_2022 || UNITY_2023_1_OR_NEWER
            if (UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline != null)
            {
                RenderInfoItem(
                    "If you are using URP but don't use post-processing we recommend disabling them. This will reduce approximately 1mb from your final build size. Check our tips on the link below for more info.");
            }
#endif

#if UNITY_2021 || UNITY_2022 || UNITY_2023_1_OR_NEWER
            // Unity is currently missing an API for accessing the GraphicsSettings preloaded shaders,
            // so these need to be read from a serialized object.
            var serializedGraphicsSettings = new SerializedObject(UnityEngine.Rendering.GraphicsSettings.GetGraphicsSettings());
            var preloadedShadersCount = serializedGraphicsSettings.FindProperty("m_PreloadedShaders").arraySize;
            if (preloadedShadersCount > 0)
            {
                RenderInfoItem(
                    $"Your project is preloading {preloadedShadersCount} shader(s). On WebGL, preloading shaders may considerably slow down the loading of the game.");
            }
#endif

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Read more tips on our developer documentation"))
                Application.OpenURL("https://docs.crazygames.com/sdk/unity/resources/export-tips/");
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Renders OK/FAIL indicator, option name, and optional "Fix" button.
        /// </summary>
        /// <param name="optionName">Display name of the option.</param>
        /// <param name="ok">Whether the export option already has the correct value.</param>
        /// <param name="fixAction">Called when the Fix button is clicked.</param>
        /// <param name="additionalInfo">Optional extra information shown below the label.</param>
        private static void RenderFixableItem(string optionName, bool ok, Action fixAction,
            string additionalInfo = null)
        {
            EditorGUILayout.BeginVertical();
            GUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(ok ? "OK" : "FAIL", ok ? OkStyle : FailStyle, GUILayout.Width(35));
            GUILayout.Label(optionName, LabelStyle);
            GUILayout.FlexibleSpace();

            if (!ok && GUILayout.Button("Fix"))
                fixAction();

            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(additionalInfo))
            {
                GUILayout.Space(5);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(35);
                GUILayout.Label(additionalInfo, AdditionalInfoStyle);
                EditorGUILayout.EndHorizontal();
            }

            GUILayout.Space(10);
            EditorGUILayout.EndVertical();
        }

        private static void RenderInfoItem(string info)
        {
            EditorGUILayout.BeginVertical();
            GUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("INFO", InfoStyle, GUILayout.Width(35));
            GUILayout.Label(info, AdditionalInfoStyle);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);
            EditorGUILayout.EndVertical();
        }
    }
}