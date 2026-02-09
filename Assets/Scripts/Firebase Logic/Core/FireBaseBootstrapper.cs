using System;
using System.Linq;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Initializes Firebase services and determines which scene the user
/// should be routed to based on authentication state and application rules.
/// 
/// This component is intended to run in a dedicated bootstrap scene
/// and acts as the single authority for authentication-based routing.
/// </summary>
public sealed class FirebaseBootstrapper : MonoBehaviour
{
    #region Constants
    private const string LOGIN_SCENE = "Login";
    private const string PROFILE_SCENE = "Profile";

    #endregion

    #region Private Fields
    private UserProfileRepository profileRepository;

    private FirebaseAuth firebaseAuth;
    private FirebaseUser previousUser;
    private bool isInitialized;
    private bool isRouting;
    private bool isInitializing;

    /// <summary>
    /// True only during initial app startup.
    /// Remember Me is applied ONLY during cold start.
    /// </summary>
    private bool isColdStart = true;
    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// Unity entry point.
    /// Begins Firebase initialization on application start.
    /// </summary>
    private async void Start()
    {
        await InitializeFirebaseAsync();
    }

    /// <summary>
    /// Called when the application is quitting.
    /// If Remember Me is disabled, Firebase authentication is explicitly signed out
    /// to prevent session persistence across launches.
    /// </summary>
    private void OnApplicationQuit()
    {
        if (!RememberMeUtility.IsRememberMeEnabled())
            firebaseAuth?.SignOut();
    }

    /// <summary>
    /// Cleans up Firebase event subscriptions when the object is destroyed.
    /// </summary>
    private void OnDestroy()
    {
        if (firebaseAuth != null)
            firebaseAuth.StateChanged -= OnAuthStateChanged;
    }

    #endregion

    #region Initialization

    /// <summary>
    /// Checks Firebase dependencies, initializes Firebase Authentication,
    /// and triggers the initial routing decision.
    /// </summary>
    private async Task InitializeFirebaseAsync()
    {
        if (isInitialized || isInitializing)
            return;

        isInitializing = true;

        LoadingService.Instance.Show();

        DependencyStatus dependencyStatus =
            await FirebaseApp.CheckAndFixDependenciesAsync();

        LoadingService.Instance.Hide();
        isInitializing = false;

        if (dependencyStatus != DependencyStatus.Available)
        {
            ShowConnectionErrorPopup();
            return;
        }

        firebaseAuth = FirebaseAuth.DefaultInstance;
        previousUser = firebaseAuth.CurrentUser;

        profileRepository = new UserProfileRepository();

        firebaseAuth.StateChanged += OnAuthStateChanged;

        isInitialized = true;

        // Initial deterministic routing after successful initialization
        RouteUserAsync();
    }

    #endregion

    #region Authentication State

    /// <summary>
    /// Invoked by Firebase whenever the authentication state changes.
    /// This includes sign-in, sign-out, token refresh, and session restoration.
    /// </summary>
    private void OnAuthStateChanged(object sender, EventArgs eventArgs)
    {
        if (!isInitialized)
            return;

        FirebaseUser currentUser = firebaseAuth.CurrentUser;

        // Ignore duplicate state change events for the same user
        if (currentUser == previousUser)
        {
            return;
        }

        previousUser = currentUser;
        RouteUserAsync();
    }

    #endregion

    #region Routing

    /// <summary>
    /// Determines the correct scene to load based on:
    /// - Remember Me preference (applied ONLY on cold start)
    /// - Firebase authentication state
    /// - Email verification (for email/password users)
    /// - Profile existence
    ///
    /// This method is guarded to prevent concurrent execution.
    /// </summary>
    private async void RouteUserAsync()
    {
        if (isRouting)
            return;

        isRouting = true;

        try
        {
            FirebaseUser currentUser = firebaseAuth.CurrentUser;

            // ─────────────────────────────
            // COLD START ROUTING (ONLY ONCE)
            // ─────────────────────────────
            if (isColdStart)
            {
                isColdStart = false;

                if (!RememberMeUtility.IsRememberMeEnabled())
                {
                    if (currentUser != null)
                        firebaseAuth.SignOut();

                    LoadSceneIfNeeded(LOGIN_SCENE);
                    return;
                }
            }

            // ─────────────────────────────
            // AUTH-BASED ROUTING (RUNTIME)
            // ─────────────────────────────
            if (currentUser == null)
            {
                LoadSceneIfNeeded(LOGIN_SCENE);
                return;
            }

            // Refresh user data safely
            try
            {
                await currentUser.ReloadAsync();
            }
            catch (FirebaseException ex)
            {
                // Allow transient network failures
                if (ex.ErrorCode != (int)AuthError.NetworkRequestFailed)
                {
                    firebaseAuth.SignOut();
                    LoadSceneIfNeeded(LOGIN_SCENE);
                    return;
                }
            }

            // Email verification applies only to email/password users
            bool isEmailPasswordUser =
                currentUser.ProviderData.Any(
                    p => p.ProviderId == EmailAuthProvider.ProviderId);

            if (isEmailPasswordUser && !currentUser.IsEmailVerified)
            {
                if (AuthSessionContext.IsSignUpInProgress)
                {
#if UNITY_EDITOR
                    Debug.Log(
                        "[Bootstrap] Skipping unverified-email routing. " +
                        "User was just created via sign-up flow; verification email " +
                        "is already handled by the sign-up UI.");
#endif
                    return;
                }

                PopupService.Instance.ShowConfirmation(
                    PopupType.Warning,
                    "Email Not Verified",
                    "You need to verify your email.\nWould you like us to resend the verification email?",
                    onConfirm: async () =>
                    {
                        try
                        {
                            await currentUser.SendEmailVerificationAsync();
                        }

                        catch (Exception)
                        {
                            PopupService.Instance.ShowError(
                                "Verification Failed",
                                "We couldn't send the verification email.\n\n" +
                                "Please try again later."
                            );
                        }

                        finally
                        {
                            firebaseAuth.SignOut();
                            LoadSceneIfNeeded(LOGIN_SCENE);
                        }
                    },
                    onCancel: () =>
                    {
                        firebaseAuth.SignOut();
                        LoadSceneIfNeeded(LOGIN_SCENE);
                    }
                );

                return;
            }

            // Ensure profile exists and update last login
            await profileRepository.EnsureProfileExistsAndUpdateLoginAsync();

            LoadSceneIfNeeded(PROFILE_SCENE);
        }
        catch (Exception)
        {
            PopupService.Instance.ShowError(
                "Startup Error",
                "Something went wrong while starting the app.");

            LoadSceneIfNeeded(LOGIN_SCENE);
        }
        finally
        {
            isRouting = false;
        }
    }


    #endregion

    #region Error Handling

    /// <summary>
    /// Displays a blocking popup when Firebase dependencies cannot be resolved.
    /// </summary>
    private void ShowConnectionErrorPopup()
    {
        PopupService.Instance.ShowConfirmation(
            PopupType.Error,
            "Connection Error",
            "Unable to connect to server.\n\nPlease check your internet connection and try again.",
            OnRetryConnection,
            OnQuitApplication
        );
    }

    /// <summary>
    /// Retries Firebase initialization after a dependency or connection failure.
    /// </summary>
    private void OnRetryConnection()
    {
        _ = InitializeFirebaseAsync();
    }

    /// <summary>
    /// Quits the application after a fatal initialization failure.
    /// </summary>
    private void OnQuitApplication()
    {
        Debug.Log("[Bootstrap] Application quit by user.");
        Application.Quit();
    }

    #endregion

    #region Utility
    private void LoadSceneIfNeeded(string sceneName)
    {
        if (SceneManager.GetActiveScene().name == sceneName)
            return;

        SceneManager.LoadScene(sceneName);
    }
    #endregion
}
