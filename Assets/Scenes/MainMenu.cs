
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    void Start()
    {
        Debug.Log("Script MainMenu đã sẵn sàng trên Object: " + gameObject.name);
    }

    public void PlayGame()
    {
        Debug.Log("--- Nút Play đã được nhấn! ---");
        
        // Kiểm tra xem có Scene số 1 trong Build Settings không
        if (SceneManager.sceneCountInBuildSettings > 1)
        {
            Debug.Log("Đang chuyển sang Scene Index 1...");
            SceneManager.LoadScene(3);
        }
        else
        {
            Debug.LogError("LỖI: Bạn chưa thêm Scene thứ 2 vào Build Settings hoặc Scene đó không có Index là 1!");
        }
    }

    public void QuitGame()
    {
        Debug.Log("--- Nút Quit đã được nhấn! ---");
        Debug.Log("Game sẽ đóng khi chạy bản Build chính thức (.exe/.apk)");
        Application.Quit();
    }
}