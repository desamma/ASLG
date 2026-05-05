using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class CompanionShopManager : MonoBehaviour
{
    public static CompanionShopManager Instance { get; private set; }

    [Header("Shop Settings")]
    public GameObject shopUIPanel;
    public GameObject johnsonPrefab;
    public int johnsonCost = 500;
    public TMP_Text johnsonButtonText;

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
        UpdateShopUI();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Update()
    {
        if (!LLMChatManager.Instance.IsChatting && Input.GetKeyDown(KeyCode.L) && GameObject.FindGameObjectWithTag("Player") != null)
        {
            ToggleShop();
        }
        
        if (isShopOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleShop();
        }
    }

    public void ToggleShop()
    {
        isShopOpen = !isShopOpen;
        if (shopUIPanel != null) shopUIPanel.SetActive(isShopOpen);
        if (isShopOpen)
        {
            UpdateShopUI();
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        isShopOpen = false;
        if (shopUIPanel != null) shopUIPanel.SetActive(false);
    }

    public void PurchaseJohnson()
    {
        if (SaveManager.Instance.currentSaveData.unlockedCompanions.Contains("npc_johnson"))
        {
            Debug.Log("Bạn đã sở hữu Johnson rồi!");
            return;
        }

        if (StatsManager.instance != null && StatsManager.instance.gold >= johnsonCost)
        {
            StatsManager.instance.AddGold(-johnsonCost);
            SaveManager.Instance.currentSaveData.unlockedCompanions.Add("npc_johnson");

            Transform player = GameObject.FindGameObjectWithTag("Player").transform;
            GameObject johnson = Instantiate(johnsonPrefab, player.position + new Vector3(-2f, 0, 0), Quaternion.identity);
            
            NPCCompanion npcScript = johnson.GetComponent<NPCCompanion>();
            if (npcScript != null) npcScript.playerTransform = player;
            
            LLMChatManager.Instance?.RegisterCompanion(npcScript);
            Debug.Log("Đã mua Johnson thành công!");
            UpdateShopUI();
        }
        else { Debug.Log("Không đủ Vàng!"); }
    }

    public void CloseShop()
    {
        isShopOpen = false;
        if (shopUIPanel != null) shopUIPanel.SetActive(false);
    }

    private void UpdateShopUI()
    {
        if (SaveManager.Instance != null && SaveManager.Instance.currentSaveData != null)
        {
            if (SaveManager.Instance.currentSaveData.unlockedCompanions.Contains("npc_johnson"))
            {
                if (johnsonButtonText != null)
                    johnsonButtonText.text = "HIRED";
            }
        }
    }
}