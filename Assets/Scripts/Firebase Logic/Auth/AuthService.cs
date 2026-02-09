using System;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using UnityEngine;

/// <summary>
/// Represents application-level authentication error categories.
/// Not all values originate from Firebase; some are raised by
/// external providers (e.g., Google Sign-In) or local validation.
public enum AuthErrorType
{
    // Input / validation
    InvalidEmail,
    MissingEmail,
    MissingPassword,
    UnverifiedEmail,
    WeakPassword,
    PasswordMismatch,

    // Account state
    UserNotFound,
    EmailAlreadyInUse,
    UserDisabled,
    OperationNotAllowed,

    // Credentials / providers
    WrongPassword,
    CredentialInvalid,
    CredentialAlreadyInUse,
    AccountExistsWithDifferentCredential,

    // Network / platform
    NetworkError,
    TooManyRequests,
    ConfigurationError,

    // Fallback
    Unknown,
}


/// <summary>
/// Provides authentication functionality using Firebase Authentication.
/// Acts as the single source of truth for auth-related operations.
/// </summary>
public sealed class AuthService
{
    #region Events

    /// <summary>
    /// Fired when an authentication error occurs.
    /// UI layer should subscribe and show a popup accordingly.
    /// </summary>
    public event Action<AuthErrorType, string> OnAuthError;

    #endregion

    #region Private Fields
    private readonly FirebaseAuth firebaseAuth;
    #endregion

    #region Constructor

    /// <summary>
    /// Creates a new AuthService instance.
    /// </summary>
    public AuthService()
    {
        firebaseAuth = FirebaseAuth.DefaultInstance;
    }

    #endregion

    #region Public API

    /// <summary>
    /// Signs in an existing user using email and password.
    /// </summary>
    /// <param name="email">User email address.</param>
    /// <param name="password">User password.</param>
    public async Task SignInWithEmailAsync(string email, string password)
    {
        try
        {
            AuthResult result = await firebaseAuth.SignInWithEmailAndPasswordAsync(email, password);
        }
        catch (Exception exception)
        {
            HandleAuthException(exception);
        }
    }


    /// <summary>
    /// Creates a new user using email and password,
    /// sets the display name, sends a verification email,
    /// and signs the user out until verification is completed.
    /// </summary>
    /// <param name="email">User email address.</param>
    /// <param name="password">User password.</param>
    /// <param name="displayName">User display name.</param>
    /// <returns>
    /// <c>true</c> if the account was created and the verification email was sent;
    /// otherwise, <c>false</c>.
    /// </returns>
    public async Task<bool> SignUpWithEmailAsync(
        string email,
        string password,
        string displayName)
    {
        try
        {
            AuthResult result =
                await firebaseAuth.CreateUserWithEmailAndPasswordAsync(email, password);

            UserProfile profile = new()
            {
                DisplayName = displayName
            };

            await result.User.UpdateUserProfileAsync(profile);

            await result.User.SendEmailVerificationAsync();

            // Force sign-out until email is verified
            firebaseAuth.SignOut();

            return true;
        }
        catch (Exception exception)
        {
            HandleAuthException(exception);
            return false;
        }
    }

    /// <summary>
    /// Sends a password reset email to the specified address.
    /// </summary>
    /// <param name="email">Email address to send reset link.</param>
    public async Task SendPasswordResetEmailAsync(string email)
    {
        try
        {
            await firebaseAuth.SendPasswordResetEmailAsync(email);
        }
        catch (Exception exception)
        {
            HandleAuthException(exception);
            return;
        }
    }

    /// <summary>
    /// Signs out the currently authenticated user.
    /// </summary>
    public void SignOut()
    {
        if (firebaseAuth.CurrentUser == null)
            return;

        firebaseAuth.SignOut();
    }

    #endregion

    #region Error Handling

    // Handles Firebase authentication exceptions and maps them to app-level errors.
    private void HandleAuthException(Exception exception)
    {
        if (exception is FirebaseException firebaseException)
        {
            AuthError authError = (AuthError)firebaseException.ErrorCode;
            MapFirebaseError(authError);
            return;
        }

        RaiseError(AuthErrorType.Unknown, "An unexpected error occurred.");
    }

    // Converts Firebase AuthError to AuthErrorType.
    private void MapFirebaseError(AuthError authError)
    {
        switch (authError)
        {
            // ----- Email / Password -----

            case AuthError.InvalidEmail:
                RaiseError(AuthErrorType.InvalidEmail, "The email address is not valid.");
                break;

            case AuthError.MissingEmail:
                RaiseError(AuthErrorType.MissingEmail, "Email address is required.");
                break;

            case AuthError.MissingPassword:
                RaiseError(AuthErrorType.MissingPassword, "Password is required.");
                break;

            case AuthError.WeakPassword:
                RaiseError(AuthErrorType.WeakPassword, "Password is too weak.");
                break;

            case AuthError.WrongPassword:
                RaiseError(AuthErrorType.WrongPassword, "Incorrect password.");
                break;

            case AuthError.UnverifiedEmail:
                RaiseError(AuthErrorType.UnverifiedEmail, "Email is not verified.");
                break;

            // ----- Account -----

            case AuthError.UserNotFound:
                RaiseError(AuthErrorType.UserNotFound, "No account found with this email.");
                break;

            case AuthError.EmailAlreadyInUse:
                RaiseError(AuthErrorType.EmailAlreadyInUse, "This email is already in use.");
                break;

            case AuthError.UserDisabled:
                RaiseError(AuthErrorType.UserDisabled, "This account has been disabled.");
                break;

            case AuthError.OperationNotAllowed:
                RaiseError(
                    AuthErrorType.OperationNotAllowed,
                    "This authentication method is not enabled."
                );
                break;

            // ----- Credentials / Providers -----

            case AuthError.InvalidCredential:
                RaiseError(
                    AuthErrorType.CredentialInvalid,
                    "The authentication credential is invalid or expired."
                );
                break;

            case AuthError.CredentialAlreadyInUse:
                RaiseError(
                    AuthErrorType.CredentialAlreadyInUse,
                    "This credential is already associated with another account."
                );
                break;

            case AuthError.AccountExistsWithDifferentCredentials:
                RaiseError(
                    AuthErrorType.AccountExistsWithDifferentCredential,
                    "An account already exists with a different sign-in method."
                );
                break;

            // ----- Network / Limits -----

            case AuthError.NetworkRequestFailed:
                RaiseError(
                    AuthErrorType.NetworkError,
                    "Network error. Please check your internet connection."
                );
                break;

            case AuthError.TooManyRequests:
                RaiseError(
                    AuthErrorType.TooManyRequests,
                    "Too many attempts. Please try again later."
                );
                break;

            // ----- Configuration -----

            case AuthError.InvalidApiKey:
            case AuthError.AppNotAuthorized:
            case AuthError.InvalidAppCredential:
                RaiseError(
                    AuthErrorType.ConfigurationError,
                    "Authentication is not configured correctly."
                );
                break;

            // ----- Fallback -----

            default:
                RaiseError(AuthErrorType.Unknown, "Authentication failed. Please try again.");
                break;
        }
    }


    // Raises an authentication error event.
    private void RaiseError(AuthErrorType errorType, string message)
    {
        OnAuthError?.Invoke(errorType, message);
    }

    #endregion
}
