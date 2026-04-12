using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

public class MainMenu : MonoBehaviour
{
    [Header("UI Buttons")]
    public GameObject continueButton;

    void Start()
    {
        Debug.Log("Script MainMenu đã sẵn sàng!");

        // Kiểm tra xem có file save không để hiện/ẩn nút Continue
        if (continueButton != null)
        {
            string currentUser = PlayerPrefs.GetString("CurrentUser", "default");
            string savePath = Application.persistentDataPath + $"/{currentUser}_savegame.json";
            
            if (File.Exists(savePath))
                continueButton.SetActive(true);
            else
                continueButton.SetActive(false);
        }
    }

    // Đổi tên PlayGame thành NewGame cho rõ nghĩa (có thể vẫn giữ tên PlayGame nếu muốn)
    public void PlayGame()
    {
        if (SceneManager.sceneCountInBuildSettings > 1)
        {
            SceneManager.LoadScene(1); // Chuyển sang Scene Creation (ID 1)
        }
        else
        {
            Debug.LogError("Chưa đưa Scene Creation vào Build Settings!");
        }
    }

    public void ContinueGame()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.LoadGame(); // Gọi hàm LoadGame, nó sẽ tự động chuyển Scene
        }
        else
        {
            Debug.LogError("[MainMenu] Không tìm thấy SaveManager trong Scene!");
        }
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}