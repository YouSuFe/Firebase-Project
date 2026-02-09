using UnityEngine;

/// <summary>
/// Root service container for the application.
/// Ensures all core systems persist across scene loads.
/// </summary>
public sealed class AppServices : MonoBehaviour
{
    #region Singleton
    public static AppServices Instance { get; private set; }
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
    }
    #endregion
}
