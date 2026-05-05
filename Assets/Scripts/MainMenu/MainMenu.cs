using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("Buttons")]
    public Button playButton; 
    public Button continueButton; 
    public Button yesButton; 
    public Button noButton;
    public CanvasGroup recreateCanvasGroup;
    private const string SEEN_INTRO_KEY = "LoreIntro_Seen";

    void Start()
    {
        recreateCanvasGroup = GameObject.Find("RecreateCanvas").GetComponent<CanvasGroup>();
        recreateCanvasGroup.alpha = 0f;
        recreateCanvasGroup.interactable = false;
        recreateCanvasGroup.blocksRaycasts = false;

        Debug.Log("Script MainMenu đã sẵn sàng!");

        if (continueButton != null)
        {
            continueButton.interactable = SaveManager.HasSaveFile();
            if (SaveManager.HasSaveFile())
            {
                playButton.onClick.AddListener(OpenRecreateMenu);
            }
            else
            {
                playButton.onClick.AddListener(PlayGame);
            }
        }

        yesButton.onClick.AddListener(PlayGame);
        noButton.onClick.AddListener(Return);
    }

    public void PlayGame()
    {
        if (SceneManager.sceneCountInBuildSettings > 1)
        {
            if (InventoryManager.instance != null) InventoryManager.instance.ClearAndLoadStarterItems();
            if (StatsManager.instance != null) StatsManager.instance.ResetStats();
            PlayerPrefs.DeleteKey(SEEN_INTRO_KEY);

            SceneManager.LoadScene("Creation"); 
        }
        else
        {
            Debug.LogError("Chưa đưa Scene Creation vào Build Settings!");
        }
    }

    public void OpenRecreateMenu()
    {
        PlayerPrefs.DeleteKey(SEEN_INTRO_KEY);
        recreateCanvasGroup.alpha = 1f;
        recreateCanvasGroup.interactable = true;
        recreateCanvasGroup.blocksRaycasts = true;
    }

    public void Return()
    {
        recreateCanvasGroup.alpha = 0f;
        recreateCanvasGroup.interactable = false;
        recreateCanvasGroup.blocksRaycasts = false;
    }

    public void ContinueGame()
    {
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