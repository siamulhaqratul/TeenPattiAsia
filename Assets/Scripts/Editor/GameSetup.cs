using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TeenPattiAsia.Editor
{
    public static class GameSetup
    {
        [MenuItem("Tools/Setup 60% Bottom UI")]
        [System.Obsolete]

        public static void SetupBottomUI()
        {
            // 1. Get or Load the current scene
            var scene = EditorSceneManager.GetActiveScene();

            // 2. Find Canvas in the scene

            Canvas canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                // If no canvas exists, create one
                GameObject canvasGO = new("Canvas");
                canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGO.AddComponent<CanvasScaler>();
                canvasGO.AddComponent<GraphicRaycaster>();
                Undo.RegisterCreatedObjectUndo(canvasGO, "Create Canvas");
            }

            // 3. Configure CanvasScaler
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                Undo.RecordObject(scaler, "Configure Canvas Scaler");
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0f; // Match width to keep fixed width aspect correct
            }

            // 4. Find or Create GameContainer
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
                gameContainerGO.transform.SetParent(canvas.transform, false);
                Undo.RegisterCreatedObjectUndo(gameContainerGO, "Create Game Container");
            }

            // 5. Setup DynamicViewport and RectMask2D for GameContainer
            DynamicViewport viewport = gameContainerGO.GetComponent<DynamicViewport>();
            if (viewport == null)
            {
                viewport = gameContainerGO.AddComponent<DynamicViewport>();
                Undo.RegisterCreatedObjectUndo(viewport, "Add DynamicViewport Component");
            }


            RectMask2D mask = gameContainerGO.GetComponent<RectMask2D>();
            if (mask == null)
            {
                mask = gameContainerGO.AddComponent<RectMask2D>();
                Undo.RegisterCreatedObjectUndo(mask, "Add RectMask2D Component");
            }

            viewport.UpdateLayout();

            // 6. Add an Image for visualization
            Image img = gameContainerGO.GetComponent<Image>();
            if (img == null)
            {
                img = gameContainerGO.AddComponent<Image>();
                // Semi-transparent dark color for testing
                img.color = new Color(0.1f, 0.1f, 0.1f, 1f);
            }

            // 7. Make sure EventSystem is present
            if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject esGO = new("EventSystem");
                esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                Undo.RegisterCreatedObjectUndo(esGO, "Create EventSystem");
            }

            // Mark the scene as dirty so the user can save it
            EditorSceneManager.MarkSceneDirty(scene);


            Debug.Log("Game Setup complete! Bottom 60% layout initialized with fixed width 1080.");
        }
    }
}
