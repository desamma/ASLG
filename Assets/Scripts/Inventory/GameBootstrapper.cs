using UnityEngine;

public class GameBootstrapper : MonoBehaviour
{
    private static bool isInitialized = false;

    private void Awake()
    {
        // Kiểm tra để đảm bảo chỉ đọc JSON 1 lần duy nhất lúc bật game
        if (!isInitialized)
        {
            ItemDatabase.Initialize();
            isInitialized = true;
            
            // Giữ object này sống mãi không bị hủy
            DontDestroyOnLoad(gameObject); 
        }
        else
        {
            // Tránh sinh ra bản sao nếu người chơi quay lại Start Screen
            Destroy(gameObject);
        }
    }
}