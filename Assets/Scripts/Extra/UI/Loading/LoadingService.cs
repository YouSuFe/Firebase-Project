using UnityEngine;

/// <summary>
/// Global service for controlling the loading overlay.
/// </summary>
public sealed class LoadingService : MonoBehaviour
{
    #region Singleton
    public static LoadingService Instance { get; private set; }
    #endregion

    #region Serialized Fields

    [Header("Loading Canvas")]
    [SerializeField, Tooltip("Reference to the loading controller root object.")]
    private GameObject loadingPanel;

    #endregion

    #region Private Fields
    private int loadingCounter;
    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    #endregion

    #region Public API

    /// <summary>
    /// Shows the loading overlay.
    /// Supports nested loading calls safely.
    /// </summary>
    public void Show()
    {
        loadingCounter++;
        UpdateState();
    }

    /// <summary>
    /// Hides the loading overlay.
    /// Overlay is hidden only when all Show calls are matched with Hide.
    /// </summary>
    public void Hide()
    {
        loadingCounter = Mathf.Max(loadingCounter - 1, 0);
        UpdateState();
    }

    #endregion

    #region Private Methods

    // Updates the loading panel active state.
    private void UpdateState()
    {
        loadingPanel.SetActive(loadingCounter > 0);
    }

    #endregion
}
