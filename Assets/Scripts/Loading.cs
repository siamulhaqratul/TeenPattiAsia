using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Loading : MonoBehaviour
{
    public enum LoadingDisplayMode
    {
        AlwaysShow,            // Show loading screen on every scene load
        OncePerSession,        // Show only on the first load of a session (resets on app restart)
        OnceEver              // Show only on the very first run of the game (persists in PlayerPrefs)
    }

    #region Variables
    public static Loading Instance;

    [Header("UI References")]
    [SerializeField] private Image _fillMask = null;
    [Tooltip("The main loading screen container GameObject")]
    public GameObject loadingImage;

    [Header("Settings")]
    [SerializeField, Tooltip("Loading Time In Seconds"), Range(0.1f, 10f)]

    private float _loadingTime = 1.0f;

    [SerializeField, Tooltip("How often the loading screen should be shown")]
    private LoadingDisplayMode _displayMode = LoadingDisplayMode.OncePerSession;

    private static bool _hasLoadedThisSession = false;
    private float _originalVolume = 1f;
    #endregion

    #region Unity Methods

    private void Awake()
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
        {
            StartCoroutine(StartLoadingProcess());
        }
        else
        {
            BypassLoading();
        }
    }

    #endregion

    #region Helper Methods

    private bool ShouldShowLoading()
    {
        switch (_displayMode)
        {
            case LoadingDisplayMode.AlwaysShow:
                return true;

            case LoadingDisplayMode.OncePerSession:
                return !_hasLoadedThisSession;

            case LoadingDisplayMode.OnceEver:
                return PlayerPrefs.GetInt("HasLaunchedBefore", 0) == 0;

            default:
                return true;
        }
    }

    private void BypassLoading()
    {
        if (loadingImage != null)

        {
            loadingImage.SetActive(false);
        }
        // Do not force/overwrite AudioListener.volume here to avoid disrupting current audio states.
    }

    private IEnumerator StartLoadingProcess()
    {
        // Cache current volume before muting to respect system/user settings
        _originalVolume = AudioListener.volume;
        AudioListener.volume = 0f;

        if (loadingImage != null)

        {
            loadingImage.SetActive(true);
        }

        if (_fillMask != null)

        {
            _fillMask.fillAmount = 0f;
        }

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, _loadingTime); // Avoid division by zero

        if (_fillMask != null)
        {
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
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        if (loadingImage != null)

        {
            loadingImage.SetActive(false);
        }

        // Restore original audio volume
        AudioListener.volume = _originalVolume;

        // Record that we have successfully loaded at least once
        _hasLoadedThisSession = true;
        if (_displayMode == LoadingDisplayMode.OnceEver)
        {
            PlayerPrefs.SetInt("HasLaunchedBefore", 1);
            PlayerPrefs.Save();
        }
    }

    #endregion
}
