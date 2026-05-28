using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class DynamicViewport : MonoBehaviour
{
    [Tooltip("Percentage of the screen height the game viewport should occupy (0.0 to 1.0)")]
    [Range(0f, 1f)]
    [SerializeField] private float heightPercentage = 0.6f;

    [Tooltip("Fixed width of the game viewport in canvas units")]
    [SerializeField] private float fixedWidth = 1080f;

    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        UpdateLayout();
    }

    private void OnValidate()
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }
        UpdateLayout();
    }

    private void Update()
    {
        // Keep updated in editor when screen aspect ratio changes
        #if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UpdateLayout();
        }
        #endif
    }

    /// <summary>
    /// Programmatically changes the height percentage of the game container.
    /// Useful for responding to runtime socket/API events.
    /// </summary>
    public void SetHeightPercentage(float percentage)
    {
        heightPercentage = Mathf.Clamp01(percentage);
        UpdateLayout();
    }

    /// <summary>
    /// Programmatically changes the fixed width of the game container.
    /// </summary>
    public void SetFixedWidth(float width)
    {
        fixedWidth = Mathf.Max(0f, width);
        UpdateLayout();
    }

    /// <summary>
    /// Forces an update of the RectTransform to respect current heightPercentage and fixedWidth.
    /// </summary>
    public void UpdateLayout()
    {
        if (rectTransform == null) return;

        // Anchor at bottom-center (Min Y=0, Max Y=heightPercentage)
        rectTransform.anchorMin = new Vector2(0.5f, 0f);
        rectTransform.anchorMax = new Vector2(0.5f, heightPercentage);

        // Pivot at bottom-center
        rectTransform.pivot = new Vector2(0.5f, 0f);

        // Fixed width, height is driven entirely by Y anchors
        rectTransform.sizeDelta = new Vector2(fixedWidth, 0f);

        // Reset relative position to be centered at the bottom
        rectTransform.anchoredPosition = Vector2.zero;
    }
}
