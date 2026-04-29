using UnityEngine;

public static class GameSettings
{
    public const string GuestDisplayName = "Guest";

    public static bool IsOfflineMode { get; private set; }

    public static void SetOfflineMode(bool isOffline)
    {
        if (IsOfflineMode == isOffline)
        {
            return;
        }

        IsOfflineMode = isOffline;
        Debug.Log($"<color=yellow>[System]</color> Offline mode: {IsOfflineMode}");
    }

    public static void BeginGuestSession()
    {
        SetOfflineMode(true);
        TokenManager.ClearSession();
        PlayerPrefs.SetString("CurrentUser", GuestDisplayName);
        PlayerPrefs.Save();
    }

    public static void BeginOnlineSession(string currentUser = null)
    {
        SetOfflineMode(false);

        if (!string.IsNullOrWhiteSpace(currentUser))
        {
            PlayerPrefs.SetString("CurrentUser", currentUser);
            PlayerPrefs.Save();
        }
    }
}
