using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    void Start()
    {
        Debug.Log("Script MainMenu đã sẵn sàng!");
    }

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
        if (SaveManager.HasSaveFile())
        {
            SaveManager.Instance.LoadGame();
        }
        else
        {
            Debug.LogWarning("Không tìm thấy file save!");
        }
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}