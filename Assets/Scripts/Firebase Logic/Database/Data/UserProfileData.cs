using System;
using Firebase.Firestore;

/// <summary>
/// Firestore user profile data model.
/// Stored at: users/{uid}
/// </summary>
[Serializable]
[FirestoreData]
public sealed class UserProfileData
{
    #region Firestore Fields
    [FirestoreProperty] public string uid { get; set; }
    [FirestoreProperty] public string email { get; set; }
    [FirestoreProperty] public string displayName { get; set; }
    [FirestoreProperty] public string photoUrl { get; set; }
    [FirestoreProperty] public Timestamp createdAt { get; set; }
    [FirestoreProperty] public Timestamp lastLoginAt { get; set; }
    #endregion

    #region Convenience
    public DateTime CreatedAtUtc => createdAt.ToDateTime().ToUniversalTime();
    public DateTime LastLoginAtUtc => lastLoginAt.ToDateTime().ToUniversalTime();
    #endregion
}
