using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // Bắt buộc phải có để dùng Button

public class MainMenu : MonoBehaviour
{
    [Header("Buttons")]
    public Button playButton; // Tương đương New Game
    public Button continueButton; 

    void Start()
    {
        Debug.Log("Script MainMenu đã sẵn sàng!");

        // Kiểm tra file save của UserID hiện tại: Nếu có thì sáng nút Continue, không có thì làm mờ (không cho bấm)
        if (continueButton != null)
        {
            continueButton.interactable = SaveManager.HasSaveFile();
        }
    }

    // GỌI KHI BẤM NÚT "NEW GAME" / "PLAY"
    public void PlayGame()
    {
        if (SceneManager.sceneCountInBuildSettings > 1)
        {
            // SỬA: Reset dữ liệu dính của người trước VÀ tự động phát Quà Tân Thủ
            if (InventoryManager.instance != null) InventoryManager.instance.ClearAndLoadStarterItems();
            if (StatsManager.instance != null) StatsManager.instance.ResetStats();

            SceneManager.LoadScene(1); // Chuyển sang Scene Creation (ID 1)
        }
        else
        {
            Debug.LogError("Chưa đưa Scene Creation vào Build Settings!");
        }
    }

    // GỌI KHI BẤM NÚT "CONTINUE"
    public void ContinueGame()
    {
        // Chắc cú kiểm tra lại lần nữa xem có file save không rồi mới load
        if (SaveManager.HasSaveFile())
        {
            Debug.Log("Đang nạp file Save...");
            SaveManager.Instance.LoadGame();
        }
    }

    public void QuitGame()
    {
        Debug.Log("Thoát Game!");
        Application.Quit();
    }
}