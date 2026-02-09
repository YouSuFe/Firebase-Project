using UnityEngine;

/// <summary>
/// Handles persistence of the "Remember Me" preference.
/// This controls whether the app is allowed to auto-login on startup.
/// </summary>
public static class RememberMeUtility
{
    private const string REMEMBER_ME_KEY = "REMEMBER_ME_ENABLED";

    /// <summary>
    /// Returns whether auto-login is allowed.
    /// Default is false.
    /// </summary>
    public static bool IsRememberMeEnabled()
    {
        return PlayerPrefs.GetInt(REMEMBER_ME_KEY, 0) == 1;
    }

    /// <summary>
    /// Persists the Remember Me preference.
    /// </summary>
    public static void SetRememberMe(bool enabled)
    {
        PlayerPrefs.SetInt(REMEMBER_ME_KEY, enabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Clears Remember Me explicitly (used on logout).
    /// </summary>
    public static void Clear()
    {
        PlayerPrefs.DeleteKey(REMEMBER_ME_KEY);
        PlayerPrefs.Save();
    }
}
