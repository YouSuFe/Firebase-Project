using System;
using System.Collections;
using System.Threading.Tasks;
using Firebase.Auth;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

/// <summary>
/// Controls the Profile scene UI.
/// Handles profile display, edit mode toggling,
/// and updating the user's display name.
/// </summary>
public sealed class ProfileUIController : MonoBehaviour
{
    #region Serialized Fields

    [Header("View Mode UI")]
    [SerializeField, Tooltip("Image component used to display the user's profile photo.")]
    private Image profileImage;

    [SerializeField, Tooltip("Text displaying the user's current display name.")]
    private TMP_Text displayNameText;

    [SerializeField, Tooltip("Button used to open the display name edit panel.")]
    private Button editButton;

    [SerializeField, Tooltip("Text displaying the user's email address.")]
    private TMP_Text emailText;

    [SerializeField, Tooltip("Text displaying the account creation date.")]
    private TMP_Text createdDateText;

    [SerializeField, Tooltip("Text displaying the user's last login date.")]
    private TMP_Text lastLoginDateText;

    [SerializeField, Tooltip("Button used to log the user out.")]
    private Button logoutButton;


    [Header("Edit Mode UI")]
    [SerializeField, Tooltip("Panel containing display name edit UI. Disabled by default.")]
    private GameObject editPanel;

    [SerializeField, Tooltip("Input field for entering a new display name.")]
    private TMP_InputField displayNameInput;

    [SerializeField, Tooltip("Button used to save the edited display name.")]
    private Button saveButton;

    [SerializeField, Tooltip("Button used to cancel editing and close the edit panel.")]
    private Button cancelButton;


    [Header("Assets")]
    [SerializeField, Tooltip("Default avatar sprite used when the user has no profile photo.")]
    private Sprite defaultAvatarSprite;

    #endregion

    #region Private Fields
    private UserProfileRepository profileRepository;
    private UserProfileData currentProfile;
    private bool isSaving;
    private Coroutine imageLoadCoroutine;
    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        // Subscribe button events
        editButton.onClick.AddListener(OnEditButtonClicked);
        saveButton.onClick.AddListener(OnSaveEditClicked);
        cancelButton.onClick.AddListener(OnCancelEditClicked);
        logoutButton.onClick.AddListener(OnLogoutClicked);

        editButton.interactable = false;
        editPanel.SetActive(false);
    }

    private void Start()
    {
        _ = InitializeAsync();
    }

    private void OnDestroy()
    {
        // Unsubscribe button events
        editButton.onClick.RemoveListener(OnEditButtonClicked);
        saveButton.onClick.RemoveListener(OnSaveEditClicked);
        cancelButton.onClick.RemoveListener(OnCancelEditClicked);
        logoutButton.onClick.RemoveListener(OnLogoutClicked);

        if (imageLoadCoroutine != null)
            StopCoroutine(imageLoadCoroutine);
    }

    #endregion

    #region Profile Loading

    private async Task InitializeAsync()
    {
        profileRepository = new UserProfileRepository();
        await LoadProfileAsync();
    }

    /// <summary>
    /// Loads the current user's profile from Firestore and binds it to the UI.
    /// </summary>
    private async Task LoadProfileAsync()
    {
        LoadingService.Instance.Show();

        try
        {
            currentProfile = await profileRepository.GetCurrentUserProfileAsync();

            if (currentProfile == null)
                throw new Exception("User profile not found.");

            BindProfileToUI(currentProfile);
            editButton.interactable = true;
        }
        catch (Exception exception)
        {
            Debug.LogError($"[Profile] Failed to load profile: {exception}");

            PopupService.Instance.ShowConfirmation(
                PopupType.Error,
                "Profile Error",
                "Failed to load your profile.\nWould you like to retry?",
                onConfirm: async () => await LoadProfileAsync(),
                onCancel: OnLogoutClicked
            );
        }
        finally
        {
            LoadingService.Instance.Hide();
        }
    }

    #endregion

    #region UI Binding

    /// <summary>
    /// Applies user profile data to all view-mode UI elements.
    /// </summary>
    private void BindProfileToUI(UserProfileData profile)
    {
        displayNameText.text = profile.displayName;
        emailText.text = profile.email;

        createdDateText.text = FormatDate(profile.CreatedAtUtc);
        lastLoginDateText.text = FormatDate(profile.LastLoginAtUtc);

        LoadProfileImage(profile.photoUrl);
    }

    /// <summary>
    /// Formats a DateTime value as dd/MM/yy with time on a new line.
    /// </summary>
    private string FormatDate(DateTime dateTime)
    {
        return dateTime.ToString("dd/MM/yy\nHH:mm");
    }

    #endregion

    #region Profile Image

    // Loads profile image from URL or uses default avatar.
    private void LoadProfileImage(string photoUrl)
    {
        if (string.IsNullOrWhiteSpace(photoUrl))
        {
            profileImage.sprite = defaultAvatarSprite;
            return;
        }

        imageLoadCoroutine = StartCoroutine(LoadImageFromUrlCoroutine(photoUrl));
    }

    // Downloads image from URL and applies it to the profile image.
    private IEnumerator LoadImageFromUrlCoroutine(string url)
    {
        using UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"[Profile] Failed to load profile image: {request.error}");
            profileImage.sprite = defaultAvatarSprite;
            yield break;
        }

        Texture2D texture = DownloadHandlerTexture.GetContent(request);
        profileImage.sprite = Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f)
        );
    }

    #endregion

    #region Edit Mode

    /// <summary>
    /// Opens the edit panel and pre-fills the display name input.
    /// </summary>
    public void OnEditButtonClicked()
    {
        if (currentProfile == null)
            return;

        displayNameInput.text = currentProfile.displayName;
        editPanel.SetActive(true);
    }

    /// <summary>
    /// Cancels display name editing and closes the edit panel.
    /// </summary>
    public void OnCancelEditClicked()
    {
        displayNameInput.text = currentProfile.displayName;
        editPanel.SetActive(false);
    }

    /// <summary>
    /// Saves the edited display name to Firestore and updates the UI.
    /// </summary>
    public async void OnSaveEditClicked()
    {
        if (isSaving)
            return;

        string newDisplayName = displayNameInput.text.Trim();

        if (string.IsNullOrEmpty(newDisplayName))
        {
            PopupService.Instance.ShowError(
                "Invalid Name",
                "Display name cannot be empty.");
            return;
        }

        isSaving = true;
        LoadingService.Instance.Show();

        try
        {
            await profileRepository.UpdateDisplayNameAsync(newDisplayName);

            currentProfile.displayName = newDisplayName;
            displayNameText.text = newDisplayName;

            editPanel.SetActive(false);
        }
        catch (Exception exception)
        {
            Debug.LogError($"[Profile] Failed to update display name: {exception}");

            PopupService.Instance.ShowError(
                "Update Failed",
                "Failed to update display name. Please try again.");
        }
        finally
        {
            isSaving = false;
            LoadingService.Instance.Hide();
        }
    }

    #endregion

    #region Logout

    /// <summary>
    /// Logs out the current user and clears persisted authentication state.
    /// Scene routing is handled by FirebaseBootstrapper.
    /// </summary>
    public void OnLogoutClicked()
    {
        RememberMeUtility.Clear();
        FirebaseAuth.DefaultInstance.SignOut();

        if (GoogleAuthService.Instance != null)
            GoogleAuthService.Instance.ClearGoogleSession();
    }

    #endregion
}
