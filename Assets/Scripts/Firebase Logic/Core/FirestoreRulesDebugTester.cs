using System;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using UnityEngine;

/// <summary>
/// Debug-only utility to intentionally perform INVALID Firestore writes
/// in order to test Firestore security rules.
/// 
/// ⚠️ DO NOT SHIP THIS IN PRODUCTION.
/// </summary>
public sealed class FirestoreRulesDebugTester : MonoBehaviour
{
    #region Private Fields

    private FirebaseAuth firebaseAuth;
    private FirebaseFirestore firestore;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        firebaseAuth = FirebaseAuth.DefaultInstance;
        firestore = FirebaseFirestore.DefaultInstance;
    }

    #endregion

    #region Debug Tests

    /// <summary>
    /// Attempts to illegally update the user's email field.
    /// Expected result: PERMISSION DENIED.
    /// </summary>
    [ContextMenu("Test / Update Email (Should Fail)")]
    public async void Test_UpdateEmail_ShouldFail()
    {
        await TryIllegalUpdateAsync("email", "hacked@email.com");
    }

    /// <summary>
    /// Attempts to illegally update the user's uid field.
    /// Expected result: PERMISSION DENIED.
    /// </summary>
    [ContextMenu("Test / Update UID (Should Fail)")]
    public async void Test_UpdateUid_ShouldFail()
    {
        await TryIllegalUpdateAsync("uid", "FAKE_UID_123");
    }

    /// <summary>
    /// Attempts to illegally update the user's createdAt field.
    /// Expected result: PERMISSION DENIED.
    /// </summary>
    [ContextMenu("Test / Update CreatedAt (Should Fail)")]
    public async void Test_UpdateCreatedAt_ShouldFail()
    {
        await TryIllegalUpdateAsync(
            "createdAt",
            Timestamp.FromDateTime(DateTime.UtcNow.AddYears(-10))
        );
    }

    /// <summary>
    /// Attempts to add an unauthorized field to the document.
    /// Expected result: PERMISSION DENIED.
    /// </summary>
    [ContextMenu("Test / Add Admin Field (Should Fail)")]
    public async void Test_AddExtraField_ShouldFail()
    {
        await TryIllegalUpdateAsync("isAdmin", true);
    }

    #endregion

    #region Internal Helper

    private async Task TryIllegalUpdateAsync(string field, object value)
    {
        FirebaseUser user = firebaseAuth.CurrentUser;
        if (user == null)
        {
            Debug.LogError("[RulesTest] No authenticated user.");
            return;
        }

        DocumentReference docRef =
            firestore.Collection("users").Document(user.UserId);

        Debug.Log(
            $"[RulesTest] Attempting ILLEGAL update | field={field}, value={value}");

        try
        {
            await docRef.UpdateAsync(field, value);
            Debug.LogError(
                "[RulesTest]  WRITE SUCCEEDED (THIS IS BAD — RULES ARE WRONG)");
        }
        catch (FirebaseException ex)
        {
            Debug.Log(
                $"[RulesTest]  EXPECTED FAILURE | Code={ex.ErrorCode} | Msg={ex.Message}");

            if (ex.ErrorCode == 7)
            {
                Debug.Log(
                    "[RulesTest] PERMISSION DENIED — Firestore rules working correctly");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[RulesTest] Unexpected error: {ex}");
        }
    }

    #endregion
}
