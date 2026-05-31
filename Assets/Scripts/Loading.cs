using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace TeenPattiAsia.Game
{
    public class Loading : MonoBehaviour
    {
        public enum LoadingDisplayMode
        {
            AlwaysShow,         // Show loading screen on every scene load
            OncePerSession,     // Show only on the first load of a session (resets on app restart)
            OnceEver            // Show only on the very first run of the game (persists in PlayerPrefs)
        }

        private const string k_HasLaunchedKey = "HasLaunchedBefore";

        #region Variables

        // Exposed as read-only property; setter is private to prevent external mutation.
        public static Loading Instance { get; private set; }

        [Header("UI References")]
        [SerializeField] private Image _fillMask = null;
        [Tooltip("The main loading screen container GameObject")]
        public GameObject loadingImage;

        [Header("Settings")]
        [SerializeField, Tooltip("Loading Time In Seconds"), Range(0.1f, 10f)]
        private float _loadingTime = 1.0f;

        [SerializeField, Tooltip("How often the loading screen should be shown")]
        private LoadingDisplayMode _displayMode = LoadingDisplayMode.OncePerSession;

        // Static state survives scene loads but resets on domain reload (editor play-mode).
        private static bool _hasLoadedThisSession;
        private float _originalVolume = 1f;

        #endregion

        #region Unity Methods

        protected void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            if (ShouldShowLoading())
                StartCoroutine(StartLoadingProcess());
            else
                BypassLoading();
        }

        #endregion

        #region Helper Methods

        private bool ShouldShowLoading()
        {
            return _displayMode switch
            {
                LoadingDisplayMode.AlwaysShow      => true,
                LoadingDisplayMode.OncePerSession  => !_hasLoadedThisSession,
                LoadingDisplayMode.OnceEver        => PlayerPrefs.GetInt(k_HasLaunchedKey, 0) == 0,
                _                                  => true,
            };
        }

        private void BypassLoading()
        {
            loadingImage?.SetActive(false);
            // Do not force/overwrite AudioListener.volume here to avoid disrupting current audio states.
        }

        private IEnumerator StartLoadingProcess()
        {
            // Cache current volume before muting to respect system/user settings.
            _originalVolume = AudioListener.volume;
            AudioListener.volume = 0f;

            loadingImage?.SetActive(true);

            // Avoid division by zero for very small loading times.
            float duration = Mathf.Max(0.01f, _loadingTime);

            if (_fillMask != null)
            {
                _fillMask.fillAmount = 0f;
                float elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    _fillMask.fillAmount = Mathf.Clamp01(elapsed / duration);
                    yield return null;
                }
                _fillMask.fillAmount = 1f;
            }
            else
            {
                yield return new WaitForSeconds(duration);
            }

            loadingImage?.SetActive(false);

            // Restore original audio volume.
            AudioListener.volume = _originalVolume;

            // Record that we have successfully loaded at least once.
            _hasLoadedThisSession = true;
            if (_displayMode == LoadingDisplayMode.OnceEver)
            {
                PlayerPrefs.SetInt(k_HasLaunchedKey, 1);
                PlayerPrefs.Save();
            }
        }

        #endregion
    }
}
