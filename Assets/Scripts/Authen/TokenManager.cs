using UnityEngine;
using System.IO;

public static class TokenManager
{
    private const string TOKEN_KEY = "AuthToken";
    private const string USER_ID_KEY = "UserId";

    // Đường dẫn đến file text trong thư mục Project của bạn
    private static string logPath = Application.dataPath + "/Scripts/Authen/token_log.txt";

    public static void SaveSession(string token, string userId)
    {
        // 1. Lưu vào PlayerPrefs
        PlayerPrefs.SetString(TOKEN_KEY, token);
        PlayerPrefs.SetString(USER_ID_KEY, userId); 
        PlayerPrefs.Save();

        // 2. Ghi ra file Text để bạn dễ tìm kiếm
        try
        {
            // THÊM: Ghi User ID vào dòng trên cùng của file text
            string content = $"[TIME: {System.DateTime.Now}]\nUSER ID: {userId}\nTOKEN: {token}\n-------------------";
            File.WriteAllText(logPath, content);
            Debug.Log($"[TokenManager] Đã ghi Token và User ID ra file tại: {logPath}");

            // Tự động làm mới thư mục trong Unity Editor để thấy file ngay lập tức
#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif
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

    // THÊM: Hàm để các script khác lấy User ID ra dùng
    public static string GetUserId()
    {
        return PlayerPrefs.GetString(USER_ID_KEY, string.Empty);
    }

    public static bool HasToken()
    {
        return !string.IsNullOrEmpty(GetToken());
    }

    // ĐÃ SỬA: Xóa cả Token lẫn User ID khi đăng xuất
    public static void ClearSession()
    {
        PlayerPrefs.DeleteKey(TOKEN_KEY);
        PlayerPrefs.DeleteKey(USER_ID_KEY);
        PlayerPrefs.Save();

        // Xóa luôn nội dung file text khi đăng xuất
        if (File.Exists(logPath))
        {
            File.WriteAllText(logPath, "Đã đăng xuất - Token và User ID đã bị xóa.");
        }
    }
}