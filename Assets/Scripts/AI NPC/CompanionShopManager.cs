using UnityEngine;
using UnityEngine.SceneManagement;

public class CompanionShopManager : MonoBehaviour
{
    public static CompanionShopManager Instance { get; private set; }

    [Header("Shop Settings")]
    public GameObject shopUIPanel;
    public GameObject johnsonPrefab;
    public int johnsonCost = 500;

    private bool isShopOpen = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (shopUIPanel != null) shopUIPanel.SetActive(false);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Update()
    {
        // Chỉ cho phép mở Shop khi đang ở trong Game (có Player)
        if (Input.GetKeyDown(KeyCode.L) && GameObject.FindGameObjectWithTag("Player") != null)
        {
            ToggleShop();
        }
        
        // Đóng shop nếu bấm phím Esc
        if (isShopOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleShop();
        }
    }

    private void ToggleShop()
    {
        isShopOpen = !isShopOpen;
        if (shopUIPanel != null) shopUIPanel.SetActive(isShopOpen);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Luôn ẩn Shop khi vừa chuyển sang scene mới
        isShopOpen = false;
        if (shopUIPanel != null) shopUIPanel.SetActive(false);
    }

    // Gọi hàm này bằng UI Button Event trong Unity Inspector
    public void PurchaseJohnson()
    {
        if (SaveManager.Instance.currentSaveData.unlockedCompanions.Contains("npc_johnson"))
        {
            Debug.Log("Bạn đã sở hữu Johnson rồi!");
            return;
        }

        if (StatsManager.instance != null && StatsManager.instance.gold >= johnsonCost)
        {
            StatsManager.instance.AddGold(-johnsonCost); // Trừ tiền
            SaveManager.Instance.currentSaveData.unlockedCompanions.Add("npc_johnson");

            // Sinh ra Johnson bên cạnh Player
            Transform player = GameObject.FindGameObjectWithTag("Player").transform;
            GameObject johnson = Instantiate(johnsonPrefab, player.position + new Vector3(-2f, 0, 0), Quaternion.identity);
            
            NPCCompanion npcScript = johnson.GetComponent<NPCCompanion>();
            if (npcScript != null) npcScript.playerTransform = player;
            
            LLMChatManager.Instance?.RegisterCompanion(npcScript);
            Debug.Log("Đã mua Johnson thành công!");
        }
        else { Debug.Log("Không đủ Vàng!"); }
    }

    // Gọi hàm này bằng UI Button "X" (Close Button) trong Inspector
    public void CloseShop()
    {
        isShopOpen = false;
        if (shopUIPanel != null) shopUIPanel.SetActive(false);
    }
}