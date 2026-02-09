using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Represents popup message types.
/// Determines visual styling such as title color.
/// </summary>
public enum PopupType
{
    Error,
    Warning,
    Information
}



/// <summary>
/// Controls a reusable popup UI with confirm and cancel actions.
/// Automatically clears callbacks when hidden to prevent stale actions.
/// </summary>
public sealed class PopupController : MonoBehaviour
{
    #region Serialized Fields

    [Header("UI References")]
    [SerializeField, Tooltip("Popup title text.")]
    private TMP_Text titleText;

    [SerializeField, Tooltip("Popup message text.")]
    private TMP_Text messageText;

    [SerializeField, Tooltip("Confirm button.")]
    private Button confirmButton;

    [SerializeField, Tooltip("Cancel/Close button.")]
    private Button cancelButton;

    [Header("Title Colors")]
    [SerializeField, Tooltip("Title color for error popups.")]
    private Color errorTitleColor = Color.red;

    [SerializeField, Tooltip("Title color for warning popups.")]
    private Color warningTitleColor = Color.yellow;

    [SerializeField, Tooltip("Title color for information popups.")]
    private Color informationTitleColor = Color.gray;

    #endregion

    #region Private Fields
    private Action confirmCallback;
    private Action cancelCallback;
    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        confirmButton.onClick.AddListener(OnConfirmClicked);
        cancelButton.onClick.AddListener(OnCancelClicked);
    }

    private void OnDestroy()
    {
        confirmButton.onClick.RemoveListener(OnConfirmClicked);
        cancelButton.onClick.RemoveListener(OnCancelClicked);
    }

    #endregion

    #region Public API

    /// <summary>
    /// Shows the popup with specified content, type, and callbacks.
    /// </summary>
    /// <param name="type">Popup message type.</param>
    /// <param name="title">Popup title text.</param>
    /// <param name="message">Popup message text.</param>
    /// <param name="onConfirm">Confirm button callback.</param>
    /// <param name="onCancel">Cancel button callback (optional).</param>
    public void Show(
        PopupType type,
        string title,
        string message,
        Action onConfirm,
        Action onCancel = null)
    {
        titleText.text = title;
        titleText.color = GetTitleColor(type);

        messageText.text = message;

        confirmCallback = onConfirm;
        cancelCallback = onCancel;

        cancelButton.gameObject.SetActive(onCancel != null);

        gameObject.SetActive(true);
    }

    /// <summary>
    /// Hides the popup.
    /// </summary>
    public void Hide()
    {
        confirmCallback = null;
        cancelCallback = null;

        gameObject.SetActive(false);
    }

    #endregion

    #region Button Handlers

    private void OnConfirmClicked()
    {
        confirmCallback?.Invoke();
        Hide();
    }

    private void OnCancelClicked()
    {
        cancelCallback?.Invoke();
        Hide();
    }

    #endregion

    #region Styling

    // Returns the title color based on popup type.
    private Color GetTitleColor(PopupType type)
    {
        return type switch
        {
            PopupType.Error => errorTitleColor,
            PopupType.Warning => warningTitleColor,
            PopupType.Information => informationTitleColor,
            _ => informationTitleColor
        };
    }

    #endregion
}
