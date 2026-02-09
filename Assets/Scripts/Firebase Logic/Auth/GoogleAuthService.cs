using System;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Google;
using UnityEngine;

/// <summary>
/// Handles Google Sign-In and exchanges credentials with Firebase Authentication.
/// Global service, created once and reused.
/// </summary>
[DefaultExecutionOrder(-1)]
public sealed class GoogleAuthService : MonoBehaviour
{
    #region Singleton

    public static GoogleAuthService Instance { get; private set; }

    #endregion

    #region Events

    /// <summary>
    /// Fired when a Google authentication error occurs.
    /// </summary>
    public event Action<AuthErrorType, string> OnAuthError;

    #endregion

    #region WEB API

    [Tooltip("OAuth 2.0 Web Client ID from Firebase Console.")]
    private string webClientId = "693896604653-v6il5h42nq1b5sn23drmu9ro3o3c012j.apps.googleusercontent.com";

    #endregion

    #region Private Fields

    private FirebaseAuth firebaseAuth;
    private GoogleSignInConfiguration configuration;

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

        Initialize();
    }

    #endregion

    #region Initialization

    /// <summary>
    /// Initializes Firebase Auth reference and Google Sign-In configuration.
    /// </summary>
    private void Initialize()
    {
        firebaseAuth = FirebaseAuth.DefaultInstance;

        configuration = new GoogleSignInConfiguration
        {
            WebClientId = webClientId,
            RequestIdToken = true,
            RequestEmail = true,
            UseGameSignIn = false
        };
    }

    #endregion

    #region Public API

    /// <summary>
    /// Starts the Google Sign-In flow and signs the user into Firebase.
    /// </summary>
    public async Task SignInWithGoogleAsync()
    {
        if (string.IsNullOrWhiteSpace(webClientId))
        {
            RaiseError(
                AuthErrorType.ConfigurationError,
                "Google Sign-In is not configured correctly.");
            return;
        }

        try
        {
            GoogleSignIn.Configuration = configuration;

            GoogleSignInUser googleUser =
                await GoogleSignIn.DefaultInstance.SignIn();

            if (googleUser == null)
            {
                RaiseError(
                    AuthErrorType.Unknown,
                    "Google sign-in was cancelled.");
                return;
            }

            if (string.IsNullOrEmpty(googleUser.IdToken))
            {
                RaiseError(
                    AuthErrorType.CredentialInvalid,
                    "Google authentication failed. Please try again.");
                return;
            }

            Credential credential =
                GoogleAuthProvider.GetCredential(googleUser.IdToken, null);

            await firebaseAuth.SignInWithCredentialAsync(credential);
        }
        catch (Exception exception)
        {
            HandleGoogleException(exception);
        }
    }

    /// <summary>
    /// Clears the Google Sign-In session for this app.
    ///
    /// What this does:
    /// - Signs out the current Google session (if any).
    /// - Disconnects the app from the previously used Google account.
    ///
    /// Why this exists:
    /// - Google Play Services caches the last signed-in account.
    /// - Calling this ensures the NEXT Google sign-in will show
    ///   the account chooser instead of auto-selecting.
    /// </summary>
    public void ClearGoogleSession()
    {
        // These methods are synchronous (void) by design.
        // They are best-effort calls to reset Google account state.
        GoogleSignIn.DefaultInstance.SignOut();
        GoogleSignIn.DefaultInstance.Disconnect();
    }

    #endregion

    #region Error Handling

    private void HandleGoogleException(Exception exception)
    {
        if (exception is GoogleSignIn.SignInException)
        {
            RaiseError(AuthErrorType.NetworkError, "Google sign-in failed.");
            return;
        }

        if (exception is FirebaseException firebaseException)
        {
            RaiseError(AuthErrorType.Unknown, firebaseException.Message);
            return;
        }

        RaiseError(AuthErrorType.Unknown, "An unexpected error occurred.");
    }

    private void RaiseError(AuthErrorType errorType, string message)
    {
        OnAuthError?.Invoke(errorType, message);
    }

    #endregion
}
