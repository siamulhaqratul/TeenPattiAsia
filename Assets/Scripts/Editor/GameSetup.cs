using TeenPattiAsia.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TeenPattiAsia.Editor
{
    public static class GameSetup
    {
        // "%" in a MenuItem path is Unity's shortcut prefix (Ctrl/Cmd), so "Setup % Bottom UI"
        // would register as Ctrl+B. Use a plain name to avoid accidental shortcut binding.
        [MenuItem("Tools/Setup Bottom UI (50%)")]
        public static void SetupBottomUI()
        {
            // Group all operations into a single undoable action.
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Setup Bottom UI");
            int undoGroup = Undo.GetCurrentGroup();

            // 1. Find or create Canvas.
#if UNITY_2023_1_OR_NEWER
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
#else
            Canvas canvas = Object.FindObjectOfType<Canvas>();
#endif
            if (canvas == null)
            {
                GameObject canvasGO = new("Canvas");
                Undo.RegisterCreatedObjectUndo(canvasGO, "Create Canvas");
                canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGO.AddComponent<CanvasScaler>();
                canvasGO.AddComponent<GraphicRaycaster>();
            }

            // 2. Configure CanvasScaler.
            if (canvas.TryGetComponent<CanvasScaler>(out var scaler))
            {
                Undo.RecordObject(scaler, "Configure Canvas Scaler");
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0f; // Match width for fixed-width aspect.
            }

            // 3. Find or create GameContainer.
            Transform gameContainerTransform = canvas.transform.Find("GameContainer");
            GameObject gameContainerGO;
            if (gameContainerTransform != null)
            {
                gameContainerGO = gameContainerTransform.gameObject;
                Undo.RecordObject(gameContainerGO.transform, "Update Game Container");
            }
            else
            {
                gameContainerGO = new GameObject("GameContainer");
                Undo.RegisterCreatedObjectUndo(gameContainerGO, "Create Game Container");
                gameContainerGO.transform.SetParent(canvas.transform, false);
            }

            // 4. Ensure DynamicViewport (also guarantees RectTransform + RectMask2D via [RequireComponent]).
            if (!gameContainerGO.TryGetComponent<DynamicViewport>(out var viewport))
            {
                viewport = Undo.AddComponent<DynamicViewport>(gameContainerGO);
            }
            viewport.UpdateLayout();

            // 5. Add a semi-transparent Image for editor visualization.
            if (!gameContainerGO.TryGetComponent<Image>(out var img))
            {
                img = Undo.AddComponent<Image>(gameContainerGO);
                img.color = new Color(0.1f, 0.1f, 0.1f, 1f);
                img.raycastTarget = false;
            }

            // 6. Ensure EventSystem is present.
#if UNITY_2023_1_OR_NEWER
            bool hasEventSystem = Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null;
#else
            bool hasEventSystem = Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() != null;
#endif
            if (!hasEventSystem)
            {
                GameObject esGO = new("EventSystem");
                Undo.RegisterCreatedObjectUndo(esGO, "Create EventSystem");
                esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            // Collapse all recorded operations into a single undo step.
            Undo.CollapseUndoOperations(undoGroup);

            // Mark the scene dirty so the user is prompted to save.
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            Debug.Log("[GameSetup] Bottom UI setup complete — 60% height, 1080px fixed width.");
        }
    }
}
