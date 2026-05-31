using UnityEngine;
using UnityEngine.UI;

namespace TeenPattiAsia.Game
{
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(RectMask2D))]
    public class DynamicViewport : MonoBehaviour
    {
        [Tooltip("Percentage of the screen height the game viewport should occupy (0.0 to 1.0)")]
        [Range(0f, 1f)]
        [SerializeField] private float heightPercentage = 0.5f;

        [Tooltip("Fixed width of the game viewport in canvas units")]
        [SerializeField] private float fixedWidth = 1080f;

        // Cached in Awake — never null at runtime because [RequireComponent] guarantees it.
        private RectTransform _rectTransform;

        protected void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            UpdateLayout();
        }

        protected void OnValidate()
        {
            // OnValidate can run before Awake in the editor, so guard with null check.
            if (_rectTransform == null)
                _rectTransform = GetComponent<RectTransform>();

            UpdateLayout();
        }

        // [ExecuteAlways] + Update() fires every editor frame.
        // Only run in editor while not playing; at runtime layout is static unless explicitly changed.
        protected void Update()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                UpdateLayout();
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
            if (_rectTransform == null) return;

            // Anchor at bottom-center: Y spans from 0 → heightPercentage of parent.
            _rectTransform.anchorMin = new Vector2(0.5f, 0f);
            _rectTransform.anchorMax = new Vector2(0.5f, heightPercentage);

            // Pivot at bottom-center so anchoredPosition = zero means flush to bottom.
            _rectTransform.pivot = new Vector2(0.5f, 0f);

            // Fixed width; height is driven entirely by the Y anchors.
            _rectTransform.sizeDelta = new Vector2(fixedWidth, 0f);

            // Reset relative position to be centered horizontally at the bottom.
            _rectTransform.anchoredPosition = Vector2.zero;
        }
    }
}
