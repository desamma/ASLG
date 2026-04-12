using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

// 1. CLASS LƯU TRỮ DỮ LIỆU
[System.Serializable]
public class SavedItemData
{
    public string itemName;
    public int amount;
}

[System.Serializable]
public class PlayerSaveData
{
    // Class nhân vật (Rất quan trọng để khôi phục đúng nhân vật)
    public PlayerClass selectedClass;

    // Vị trí & Map
    public float posX, posY, posZ;
    public int sceneIndex;

    // Stats
    public int level;
    public int currentExp;
    public float currentHealth;
    public float currentMana;
    public float currentStamina;

    // Túi đồ
    public List<SavedItemData> inventory = new List<SavedItemData>();
}

// 2. MANAGER QUẢN LÝ SAVE/LOAD
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance;

    [Header("Database (Cơ sở dữ liệu)")]
    [Tooltip("Kéo thả TOÀN BỘ các file ItemData (ScriptableObject) vào đây để hệ thống nhận diện khi Load game")]
    public List<ItemData> allItemsDatabase = new List<ItemData>();

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

    private string GetSavePath()
    {
        // Tách file save riêng cho từng tài khoản đăng nhập (mặc định là 'default' nếu chơi offline)
        string currentUser = PlayerPrefs.GetString("CurrentUser", "default");
        return Application.persistentDataPath + $"/{currentUser}_savegame.json";
    }

    // --- GỌI HÀM NÀY ĐỂ LƯU GAME ---
    public void SaveGame()
    {
        PlayerSaveData data = new PlayerSaveData();

        // 0. Lưu Class hiện tại
        if (ClassManager.Instance != null)
        {
            data.selectedClass = ClassManager.Instance.SelectedClass;
        }

        // 1. Lưu Vị trí & Scene
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            data.posX = player.transform.position.x;
            data.posY = player.transform.position.y;
            data.posZ = player.transform.position.z;
        }
        data.sceneIndex = SceneManager.GetActiveScene().buildIndex;

        // 2. Lưu Stats
        if (StatsManager.instance != null)
        {
            data.level = StatsManager.instance.level;
            data.currentExp = StatsManager.instance.currentExp;
            data.currentHealth = StatsManager.instance.currentHealth;
            data.currentMana = StatsManager.instance.currentMana;
            data.currentStamina = StatsManager.instance.currentStamina;
        }

        // 3. Lưu Inventory
        if (InventoryUI.instance != null)
        {
            foreach (ItemData item in InventoryUI.instance.playerItems)
            {
                data.inventory.Add(new SavedItemData { itemName = item.itemName, amount = item.currentStack });
            }
        }

        // Ghi ra file JSON
        string jsonData = JsonUtility.ToJson(data, true);
        File.WriteAllText(GetSavePath(), jsonData);
        Debug.Log($"<color=green>[SaveManager]</color> Đã lưu game thành công tại: {GetSavePath()}");
    }

    // --- GỌI HÀM NÀY ĐỂ TẢI GAME ---
    public void LoadGame()
    {
        string path = GetSavePath();
        if (!File.Exists(path))
        {
            Debug.LogWarning("<color=orange>[SaveManager]</color> Không tìm thấy file save!");
            return;
        }

        string jsonData = File.ReadAllText(path);
        PlayerSaveData data = JsonUtility.FromJson<PlayerSaveData>(jsonData);

        // Việc Load Scene cần thời gian, nên ta chạy Coroutine chờ scene load xong mới áp dụng dữ liệu
        StartCoroutine(LoadSceneAndApplyData(data));
    }

    private IEnumerator LoadSceneAndApplyData(PlayerSaveData data)
    {
        // Chuyển tới Map đã lưu
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(data.sceneIndex);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // --- SCENE ĐÃ LOAD XONG, BẮT ĐẦU ÁP DỤNG DỮ LIỆU ---

        // 0. Phục hồi Class cho người chơi trước khi áp dụng Stats
        if (ClassManager.Instance != null)
        {
            ClassManager.Instance.SelectClass(data.selectedClass);
        }

        // 1. Đặt lại vị trí Player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            player.transform.position = new Vector3(data.posX, data.posY, data.posZ);
        }

        // 2. Trả lại Stats
        if (StatsManager.instance != null)
        {
            StatsManager.instance.level = data.level;
            StatsManager.instance.currentExp = data.currentExp;
            StatsManager.instance.currentHealth = data.currentHealth;
            StatsManager.instance.currentMana = data.currentMana;
            StatsManager.instance.currentStamina = data.currentStamina;
        }

        // 3. Phục hồi Túi đồ (Dùng Clone để bảo vệ file gốc)
        if (InventoryUI.instance != null)
        {
            InventoryUI.instance.playerItems.Clear(); // Xóa đồ cũ
            
            foreach (SavedItemData savedItem in data.inventory)
            {
                // Tìm item gốc trong Database theo tên
                ItemData originalItem = allItemsDatabase.Find(i => i.itemName == savedItem.itemName);
                
                if (originalItem != null)
                {
                    // Tạo một bản sao (Instantiate) để số lượng (currentStack) không ghi đè vào file gốc của Unity
                    ItemData clonedItem = Instantiate(originalItem);
                    clonedItem.currentStack = savedItem.amount;
                    
                    InventoryUI.instance.playerItems.Add(clonedItem);
                }
            }
        }

        Debug.Log("<color=green>[SaveManager]</color> Đã Load Game hoàn tất!");
    }
}