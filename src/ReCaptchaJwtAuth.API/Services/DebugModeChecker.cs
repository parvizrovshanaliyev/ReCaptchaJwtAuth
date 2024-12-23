namespace ReCaptchaJwtAuth.API.Services;

public static class DebugModeChecker
{
    /// <summary>
    /// Determines if the application is running in Debug mode.
    /// </summary>
    /// <returns>True if running in Debug mode; otherwise, False.</returns>
    public static bool IsDebugMode()
    {
#if DEBUG
        return true;
#else
        return false;
#endif
    }
}
