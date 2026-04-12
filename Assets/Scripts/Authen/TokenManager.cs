using UnityEngine;
using System.IO; // Thư viện để đọc/ghi file

public static class TokenManager
{
    private const string TOKEN_KEY = "AuthToken";

    // Đường dẫn đến file text trong thư mục Project của bạn
    // Application.dataPath chính là thư mục "Assets"
    private static string logPath = Application.dataPath + "/Scripts/Authen/token_log.txt";

    public static void SaveToken(string token)
    {
        // 1. Lưu vào PlayerPrefs (để game vẫn chạy được logic cũ)
        PlayerPrefs.SetString(TOKEN_KEY, token);
        PlayerPrefs.Save();

        // 2. Ghi ra file Text để bạn dễ tìm kiếm
        try
        {
            string content = $"[TIME: {System.DateTime.Now}]\nTOKEN: {token}\n-------------------";
            File.WriteAllText(logPath, content);
            Debug.Log($"[TokenManager] Đã ghi Token ra file tại: {logPath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[TokenManager] Không thể ghi file text: {e.Message}");
        }
    }

    public static string GetToken()
    {
        return PlayerPrefs.GetString(TOKEN_KEY, string.Empty);
    }

    public static bool HasToken()
    {
        return !string.IsNullOrEmpty(GetToken());
    }

    public static void ClearToken()
    {
        PlayerPrefs.DeleteKey(TOKEN_KEY);
        PlayerPrefs.Save();

        // Xóa luôn nội dung file text khi đăng xuất
        if (File.Exists(logPath))
        {
            File.WriteAllText(logPath, "Đã đăng xuất - Token đã bị xóa.");
        }
    }
}