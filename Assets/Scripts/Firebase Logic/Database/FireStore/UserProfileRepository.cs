using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using UnityEngine;

/// <summary>
/// Handles Firestore operations for user profiles.
/// Creates/updates the user's profile document and provides read/update helpers.
/// </summary>
public sealed class UserProfileRepository
{
    #region Constants
    private const string USERS_COLLECTION = "users";
    #endregion

    #region Private Fields
    private readonly FirebaseAuth firebaseAuth;
    private readonly FirebaseFirestore firestore;
    #endregion

    #region Constructor
    /// <summary>
    /// Creates a new repository instance for user profile operations.
    /// </summary>
    public UserProfileRepository()
    {
        firebaseAuth = FirebaseAuth.DefaultInstance;
        firestore = FirebaseFirestore.DefaultInstance;
    }
    #endregion

    #region Public API

    /// <summary>
    /// Ensures the current user's profile document exists in Firestore.
    ///
    /// Behavior:
    /// - If the profile document does NOT exist (first login):
    ///   - Creates the document and seeds profile fields (displayName, photoUrl)
    ///     from FirebaseAuth.
    /// - If the profile document already exists:
    ///   - DOES NOT overwrite user-editable fields (displayName, photoUrl).
    /// - Always updates lastLoginAt on every successful login.
    /// </summary>
    public async Task EnsureProfileExistsAndUpdateLoginAsync()
    {
        FirebaseUser user = firebaseAuth.CurrentUser;
        if (user == null)
            throw new InvalidOperationException("Cannot ensure profile: no authenticated user.");

        DocumentReference docRef = GetUserDocRef(user.UserId);

        try
        {
            DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

            Dictionary<string, object> updates = new()
            {
                { "lastLoginAt", FieldValue.ServerTimestamp }
            };

            if (!snapshot.Exists)
            {
                updates["uid"] = user.UserId;
                updates["email"] = user.Email ?? string.Empty;
                updates["createdAt"] = FieldValue.ServerTimestamp;
                updates["displayName"] = user.DisplayName ?? string.Empty;
                updates["photoUrl"] = user.PhotoUrl?.ToString() ?? string.Empty;
            }

            await docRef.SetAsync(updates, SetOptions.MergeAll);
        }
        catch (FirebaseException ex)
        {
#if UNITY_EDITOR
            Debug.LogError(
                $"[UserProfileRepository] FIRESTORE ERROR | Code={ex.ErrorCode} | Msg={ex.Message}");

            if (ex.ErrorCode == 7)
                Debug.LogError("[UserProfileRepository]  Permission denied by Firestore rules");

#endif
            throw;
        }
    }



    /// <summary>
    /// Gets the current user's profile document from Firestore.
    /// Returns null if the document does not exist.
    /// </summary>
    public async Task<UserProfileData> GetCurrentUserProfileAsync()
    {
        FirebaseUser user = firebaseAuth.CurrentUser;
        if (user == null)
            throw new InvalidOperationException("Cannot load profile: no authenticated user.");

        try
        {
            DocumentReference docRef = GetUserDocRef(user.UserId);
            DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

            if (!snapshot.Exists)
                return null;

            return snapshot.ConvertTo<UserProfileData>();
        }
        catch (Exception ex)
        {
#if UNITY_EDITOR
            Debug.LogError($"[UserProfileRepository] Failed to load profile. {ex}");
#endif
            throw;
        }
    }

    /// <summary>
    /// Updates the user's display name in Firestore.
    /// </summary>
    /// <param name="newDisplayName">New display name value.</param>
    public async Task UpdateDisplayNameAsync(string newDisplayName)
    {
        FirebaseUser user = firebaseAuth.CurrentUser;
        if (user == null)
            throw new InvalidOperationException("Cannot update display name: no authenticated user.");

        string trimmedName = newDisplayName?.Trim() ?? string.Empty;

        try
        {
            // 1) Update FirebaseAuth profile
            UserProfile authProfile = new UserProfile
            {
                DisplayName = trimmedName,
            };
            await user.UpdateUserProfileAsync(authProfile);

            // 2) Update Firestore profile
            DocumentReference docRef = GetUserDocRef(user.UserId);
            await docRef.UpdateAsync("displayName", trimmedName);
        }
        catch (Exception ex)
        {
#if UNITY_EDITOR
            Debug.LogError($"[UserProfileRepository] Failed to update display name. {ex}");
#endif
            throw;
        }
    }

    /// <summary>
    /// Updates the user's photoUrl in Firestore.
    /// Intended for Google sign-in users (we do not upload images).
    /// </summary>
    /// <param name="photoUrl">The photo URL string.</param>
    public async Task UpdatePhotoUrlAsync(string photoUrl)
    {
        FirebaseUser user = firebaseAuth.CurrentUser;
        if (user == null)
            throw new InvalidOperationException("Cannot update photo URL: no authenticated user.");

        try
        {
            Uri photoUri = string.IsNullOrWhiteSpace(photoUrl) ? null : new Uri(photoUrl);

            // 1) Update FirebaseAuth profile
            UserProfile authProfile = new UserProfile
            {
                DisplayName = user.DisplayName,
                PhotoUrl = photoUri
            };
            await user.UpdateUserProfileAsync(authProfile);

            // 2) Update Firestore profile
            DocumentReference docRef = GetUserDocRef(user.UserId);
            await docRef.UpdateAsync("photoUrl", photoUrl ?? string.Empty);
        }
        catch (Exception ex)
        {
#if UNITY_EDITOR
            Debug.LogError($"[UserProfileRepository] Failed to update photo URL. {ex}");
#endif
            throw;
        }
    }

    #endregion

    #region Private Helpers
    // Gets document reference for users/{uid}.
    private DocumentReference GetUserDocRef(string uid)
    {
        return firestore.Collection(USERS_COLLECTION).Document(uid);
    }
    #endregion
}
