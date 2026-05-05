using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // Bắt buộc phải có để dùng Button

public class MainMenu : MonoBehaviour
{
    [Header("Buttons")]
    public Button playButton; // Tương đương New Game
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

        // Kiểm tra file save của UserID hiện tại: Nếu có thì sáng nút Continue, không có thì làm mờ (không cho bấm)
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

    // GỌI KHI BẤM NÚT "NEW GAME" / "PLAY"
    public void PlayGame()
    {
        if (SceneManager.sceneCountInBuildSettings > 1)
        {
            // SỬA: Reset dữ liệu dính của người trước VÀ tự động phát Quà Tân Thủ
            if (InventoryManager.instance != null) InventoryManager.instance.ClearAndLoadStarterItems();
            if (StatsManager.instance != null) StatsManager.instance.ResetStats();
            PlayerPrefs.DeleteKey(SEEN_INTRO_KEY);

            SceneManager.LoadScene("Creation"); // Chuyển sang Scene Creation (ID 1)
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