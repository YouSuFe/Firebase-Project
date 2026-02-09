using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls login screen UI and authentication flow.
/// </summary>
public sealed class LoginUIController : MonoBehaviour
{
    #region Serialized Fields

    [Header("Panels")]
    [SerializeField, Tooltip("Sign In panel root.")]
    private GameObject signInPanel;

    [SerializeField, Tooltip("Sign Up panel root.")]
    private GameObject signUpPanel;

    [SerializeField, Tooltip("Forgot Password panel root.")]
    private GameObject forgotPasswordPanel;

    [Header("Sign In")]
    [SerializeField] private TMP_InputField signInEmailInput;
    [SerializeField] private TMP_InputField signInPasswordInput;
    [SerializeField] private Toggle rememberMeToggle;
    [SerializeField] private Button signInButton;
    [SerializeField] private Button googleSignInButton;
    [SerializeField] private Button openSignUpButton;
    [SerializeField] private Button openForgotPasswordButton;

    [Header("Sign Up")]
    [SerializeField] private TMP_InputField signUpEmailInput;
    [SerializeField] private TMP_InputField signUpUsernameInput;
    [SerializeField] private TMP_InputField signUpPasswordInput;
    [SerializeField] private TMP_InputField signUpConfirmPasswordInput;
    [SerializeField] private Button signUpButton;
    [SerializeField] private Button backToSignInButton;

    [Header("Forgot Password")]
    [SerializeField] private TMP_InputField forgotPasswordEmailInput;
    [SerializeField] private Button sendForgotPasswordButton;
    [SerializeField] private Button cancelForgotPasswordButton;

    #endregion

    #region Private Fields
    private AuthService authService;
    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        authService = new AuthService();
        authService.OnAuthError += HandleAuthError;

        // Load Remember Me preference
        rememberMeToggle.isOn = RememberMeUtility.IsRememberMeEnabled();

        // Button subscriptions
        signInButton.onClick.AddListener(OnSignInClicked);
        googleSignInButton.onClick.AddListener(OnGoogleSignInClicked);
        openSignUpButton.onClick.AddListener(OpenSignUp);
        openForgotPasswordButton.onClick.AddListener(OpenForgotPassword);

        signUpButton.onClick.AddListener(OnSignUpClicked);
        backToSignInButton.onClick.AddListener(OpenSignIn);

        sendForgotPasswordButton.onClick.AddListener(OnForgotPasswordClicked);
        cancelForgotPasswordButton.onClick.AddListener(OpenSignIn);

        OpenSignIn();
    }

    private void OnEnable()
    {
        if (GoogleAuthService.Instance != null)
            GoogleAuthService.Instance.OnAuthError += HandleAuthError;
    }

    private void OnDisable()
    {
        if (GoogleAuthService.Instance != null)
            GoogleAuthService.Instance.OnAuthError -= HandleAuthError;
    }

    private void OnDestroy()
    {
        authService.OnAuthError -= HandleAuthError;

        signInButton.onClick.RemoveListener(OnSignInClicked);
        googleSignInButton.onClick.RemoveListener(OnGoogleSignInClicked);
        openSignUpButton.onClick.RemoveListener(OpenSignUp);
        openForgotPasswordButton.onClick.RemoveListener(OpenForgotPassword);

        signUpButton.onClick.RemoveListener(OnSignUpClicked);
        backToSignInButton.onClick.RemoveListener(OpenSignIn);

        sendForgotPasswordButton.onClick.RemoveListener(OnForgotPasswordClicked);
        cancelForgotPasswordButton.onClick.RemoveListener(OpenSignIn);
    }

    #endregion

    #region Panel Switching

    public void OpenSignIn()
    {
        signInPanel.SetActive(true);
        signUpPanel.SetActive(false);
        forgotPasswordPanel.SetActive(false);
    }

    public void OpenSignUp()
    {
        signInPanel.SetActive(false);
        signUpPanel.SetActive(true);
        forgotPasswordPanel.SetActive(false);
    }

    public void OpenForgotPassword()
    {
        signInPanel.SetActive(false);
        signUpPanel.SetActive(false);
        forgotPasswordPanel.SetActive(true);
    }

    #endregion

    #region Sign In

    /// <summary>
    /// Signs in the user using email and password.
    /// Performs basic input validation before contacting Firebase.
    /// </summary>
    public async void OnSignInClicked()
    {
        if (string.IsNullOrWhiteSpace(signInEmailInput.text) ||
            string.IsNullOrWhiteSpace(signInPasswordInput.text))
        {
            PopupService.Instance.ShowError(
                "Missing Information",
                "Please enter both email and password.");
            return;
        }

        RememberMeUtility.SetRememberMe(rememberMeToggle.isOn);
        LoadingService.Instance.Show();

        try
        {
            await authService.SignInWithEmailAsync(
                signInEmailInput.text.Trim(),
                signInPasswordInput.text);
        }
        finally
        {
            LoadingService.Instance.Hide();
        }
    }

    /// <summary>
    /// Starts Google Sign-In flow.
    /// </summary>
    public async void OnGoogleSignInClicked()
    {
        RememberMeUtility.SetRememberMe(rememberMeToggle.isOn);

        LoadingService.Instance.Show();

        try
        {
            await GoogleAuthService.Instance.SignInWithGoogleAsync();
        }
        finally
        {
            LoadingService.Instance.Hide();
        }
    }

    #endregion

    #region Sign Up

    /// <summary>
    /// Handles the email/password sign-up button click.
    /// Validates input, performs Firebase sign-up, and shows
    /// a verification prompt only if sign-up succeeds.
    /// </summary>
    public async void OnSignUpClicked()
    {
        if (string.IsNullOrWhiteSpace(signUpEmailInput.text) ||
            string.IsNullOrWhiteSpace(signUpUsernameInput.text) ||
            string.IsNullOrWhiteSpace(signUpPasswordInput.text))
        {
            PopupService.Instance.ShowError(
                "Missing Information",
                "Email, username, and password are required.");
            return;
        }

        if (signUpPasswordInput.text != signUpConfirmPasswordInput.text)
        {
            PopupService.Instance.ShowError(
                "Sign Up Failed",
                "Passwords do not match.");
            return;
        }

        LoadingService.Instance.Show();
        AuthSessionContext.EndSignUp();
        AuthSessionContext.BeginSignUp();

        try
        {
            bool success = await authService.SignUpWithEmailAsync(
                signUpEmailInput.text.Trim(),
                signUpPasswordInput.text,
                signUpUsernameInput.text.Trim());

            if (!success)
                return;

            PopupService.Instance.ShowConfirmation(
                PopupType.Information,
                "Verification Email Sent",
                "Please verify your email address before logging in.\n\nCheck your inbox.",
                onConfirm: OpenSignIn,
                onCancel: OpenSignIn
            );

        }
        finally
        {
            AuthSessionContext.EndSignUp();
            LoadingService.Instance.Hide();
        }
    }

    #endregion

    #region Forgot Password

    /// <summary>
    /// Sends password reset email.
    /// Firebase intentionally does not reveal whether the email exists.
    /// </summary>
    public async void OnForgotPasswordClicked()
    {
        if (string.IsNullOrWhiteSpace(forgotPasswordEmailInput.text))
        {
            PopupService.Instance.ShowError(
                "Missing Information",
                "Please enter your email address.");
            return;
        }

        LoadingService.Instance.Show();

        try
        {
            await authService.SendPasswordResetEmailAsync(
                forgotPasswordEmailInput.text.Trim());

            PopupService.Instance.ShowConfirmation(
                PopupType.Information,
                "Email Sent",
                "If an account exists for this email, a password reset link has been sent.",
                onConfirm: OpenSignIn,
                onCancel: OpenSignIn
            );
        }
        finally
        {
            LoadingService.Instance.Hide();
        }
    }

    #endregion

    #region Error Handling

    private void HandleAuthError(AuthErrorType type, string message)
    {
        PopupService.Instance.ShowError("Authentication Error", message);
    }

    #endregion
}
