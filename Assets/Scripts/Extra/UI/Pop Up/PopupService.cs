using UnityEngine;

/// <summary>
/// Centralized service for displaying popups.
/// </summary>
public sealed class PopupService : MonoBehaviour
{
    #region Singleton
    public static PopupService Instance { get; private set; }
    #endregion

    #region Serialized Fields

    [SerializeField, Tooltip("Popup controller reference.")]
    private PopupController popupController;

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
    /// Shows an informational popup.
    /// </summary>
    public void ShowInfo(string title, string message)
    {
        if (popupController == null)
        {
            Debug.LogError("PopupController is not assigned in PopupService.");
            return;
        }

        popupController.Show(
            PopupType.Information,
            title,
            message,
            null);
    }

    /// <summary>
    /// Shows a warning popup.
    /// </summary>
    public void ShowWarning(string title, string message)
    {
        if (popupController == null)
        {
            Debug.LogError("PopupController is not assigned in PopupService.");
            return;
        }

        popupController.Show(
            PopupType.Warning,
            title,
            message,
            null);
    }

    /// <summary>
    /// Shows an error popup.
    /// </summary>
    public void ShowError(string title, string message)
    {
        if (popupController == null)
        {
            Debug.LogError("PopupController is not assigned in PopupService.");
            return;
        }

        popupController.Show(
            PopupType.Error,
            title,
            message,
            null);
    }

    /// <summary>
    /// Shows a confirmation popup with custom callbacks.
    /// </summary>
    public void ShowConfirmation(
        PopupType type,
        string title,
        string message,
        System.Action onConfirm,
        System.Action onCancel)
    {
        if (popupController == null)
        {
            Debug.LogError("PopupController is not assigned in PopupService.");
            return;
        }

        popupController.Show(
            type,
            title,
            message,
            onConfirm,
            onCancel);
    }

    #endregion
}
