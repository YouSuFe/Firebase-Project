
/// <summary>
/// Holds short-lived authentication flow context.
/// This is NOT a service and has no behavior.
/// It exists only to give routing logic intent awareness.
/// </summary>
public static class AuthSessionContext
{
    /// <summary>
    /// True only while a sign-up flow is actively running.
    /// Used to suppress routing logic that should not
    /// execute during sign-up.
    /// </summary>
    public static bool IsSignUpInProgress { get; private set; }

    public static void BeginSignUp()
    {
        IsSignUpInProgress = true;
    }

    public static void EndSignUp()
    {
        IsSignUpInProgress = false;
    }
}
