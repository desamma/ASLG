using System.Collections.Generic;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    // Singleton pattern: Cho phép các script khác (như PlayerMovement) truy cập QuestManager.instance 
    // mà không cần kéo thả trong Inspector.
    public static QuestManager instance { get; private set; }

    [Header("Danh sách Quest")]
    // Lưu những nhiệm vụ người chơi ĐANG làm (hiển thị bên trái bảng Quest Log)
    public List<QuestSO> activeQuests = new List<QuestSO>();

    // Lưu những nhiệm vụ người chơi ĐÃ LÀM XONG (để NPC không giao lại nhiệm vụ cũ)
    public List<QuestSO> completedQuests = new List<QuestSO>();

    // "Sổ tay" lưu tiến độ ngầm. Cấu trúc: Nhiệm vụ -> Mục tiêu -> Số lượng đã làm (int)
    private Dictionary<QuestSO, Dictionary<QuestObjective, int>> questProgress = new();

    // Bộ đếm phụ để cộng dồn số thời gian lẻ (giây) cho nhiệm vụ di chuyển. Cứ đủ 1 giây thì cộng vào sổ tay.
    private Dictionary<QuestObjective, float> movementTimers = new();

    // ==========================================
    // KÊNH PHÁT SÓNG (EVENT): Khi tiến độ thay đổi (giết quái, đi bộ), 
    // QuestManager sẽ gọi event này để báo cho UI biết mà vẽ lại màn hình (VD: 1/3 -> 2/3).
    // ==========================================
    public event System.Action OnQuestProgressUpdated;

    private void Awake()
    {
        // Khởi tạo Singleton
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    public List<QuestSO> GetActiveQuests()
    {
        return activeQuests;
    }

    private void OnEnable()
    {
        // Khi bật script, bắt đầu lắng nghe đài phát thanh báo quái chết
        EnemyQuestTarget.OnEnemyDied += HandleEnemyKill;
    }

    private void OnDisable()
    {
        // Tắt lắng nghe khi tắt script để tránh lỗi
        EnemyQuestTarget.OnEnemyDied -= HandleEnemyKill;
    }

    // GỌI KHI NGƯỜI CHƠI BẤM NÚT "ACCEPT" TRONG BẢNG NHIỆM VỤ
    public void AcceptQuest(QuestSO questSO)
    {
        // 1. Thêm vào danh sách đang làm
        if (!activeQuests.Contains(questSO))
        {
            activeQuests.Add(questSO);
        }

        // 2. Tạo một trang mới trong "sổ tay" cho Quest này
        if (!questProgress.ContainsKey(questSO))
        {
            questProgress[questSO] = new Dictionary<QuestObjective, int>();
        }

        // 3. Khởi tạo tất cả mục tiêu bắt đầu từ số 0
        foreach (var objective in questSO.questObjectives)
        {
            UpdateObjectiveProgress(questSO, objective);
        }

        Debug.Log($"[Hệ Thống] Đã nhận Quest: {questSO.questName}");
    }

    // Kiểm tra xem Quest này đã từng làm trong quá khứ chưa
    public bool HasCompletedQuest(QuestSO questSO)
    {
        return completedQuests.Contains(questSO);
    }

    // Kiểm tra xem Quest này đã đủ điều kiện để báo cáo "Complete" chưa
    public bool IsQuestComplete(QuestSO questSO)
    {
        if (!questProgress.ContainsKey(questSO)) return false;

        var progressDictionary = questProgress[questSO];

        foreach (var objective in questSO.questObjectives)
        {
            int currentAmount = 0;
            if (progressDictionary.ContainsKey(objective))
            {
                currentAmount = progressDictionary[objective];
            }

            // Cứ 1 mục tiêu chưa đạt yêu cầu (VD: mới 2/3 con Slime) là lập tức báo False
            if (currentAmount < objective.requiredAmount)
            {
                return false;
            }
        }
        return true;
    }

    // GỌI KHI NGƯỜI CHƠI BẤM NÚT "COMPLETE" ĐỂ TRẢ NHIỆM VỤ
    public void CompleteQuest(QuestSO questSO)
    {
        // Xóa khỏi danh sách đang làm
        if (activeQuests.Contains(questSO))
            activeQuests.Remove(questSO);

        // Chuyển sang danh sách đã hoàn thành
        if (!completedQuests.Contains(questSO))
            completedQuests.Add(questSO);

        // Xé bỏ trang sổ tay tiến độ ngầm đi cho nhẹ máy
        if (questProgress.ContainsKey(questSO))
            questProgress.Remove(questSO);

        // TRẢ THƯỞNG VÀO TÚI ĐỒ (INVENTORY)
        if (InventoryUI.instance != null)
        {
            foreach (var reward in questSO.rewards)
            {
                // Gọi hàm AddItems để gộp số lượng lớn (VD: Nhận 100 Vàng) thành 1 lần xử lý.
                InventoryUI.instance.AddItems(reward.itemData, reward.quantity);
            }
            Debug.Log($"[Hệ Thống] Đã trả Quest '{questSO.questName}' và phát thưởng!");
        }
    }

    // Khởi tạo mục tiêu bằng 0
    public void UpdateObjectiveProgress(QuestSO questSO, QuestObjective objective)
    {
        var progressDictionary = questProgress[questSO];

        if (!progressDictionary.ContainsKey(objective))
        {
            progressDictionary[objective] = 0;
        }
    }

    // ĐƯỢC GỌI TỪ SCRIPT CỦA PLAYER (PlayerMovement) MỖI KHI NHÂN VẬT ĐANG BƯỚC ĐI
    public void AddMovementProgress(float deltaTime)
    {
        foreach (var quest in activeQuests)
        {
            if (!questProgress.ContainsKey(quest)) continue;
            var progressDictionary = questProgress[quest];

            foreach (var objective in quest.questObjectives)
            {
                // Nếu mục tiêu là "Yêu cầu di chuyển" và chưa đủ số giây (VD: chưa đủ 3s)
                if (objective.requiresMovement && progressDictionary[objective] < objective.requiredAmount)
                {
                    if (!movementTimers.ContainsKey(objective))
                        movementTimers[objective] = 0f;

                    // Cộng dồn thời gian lẻ của từng khung hình
                    movementTimers[objective] += deltaTime;

                    // Khi gom đủ 1 giây tròn (hoặc hơn)
                    if (movementTimers[objective] >= 1f)
                    {
                        // Lấy phần nguyên để cộng vào tiến độ
                        int secondsToAdd = Mathf.FloorToInt(movementTimers[objective]);
                        // Trừ phần nguyên ra để giữ lại phần thập phân lẻ (chống hao hụt)
                        movementTimers[objective] -= secondsToAdd;

                        // Cộng thẳng số giây vào sổ tay
                        progressDictionary[objective] += secondsToAdd;

                        // Giới hạn max ở mức yêu cầu (VD: Đủ 3s thì ngưng không đếm nữa)
                        if (progressDictionary[objective] > objective.requiredAmount)
                            progressDictionary[objective] = objective.requiredAmount;

                        // Báo cáo UI vẽ lại số giây (VD: 1/3, 2/3)
                        OnQuestProgressUpdated?.Invoke();
                    }
                }
            }
        }
    }

    // ĐƯỢC GỌI KHI CÓ QUÁI CHẾT (Thông qua Event)
    private void HandleEnemyKill(EnemyType deadEnemyType)
    {
        foreach (var quest in activeQuests)
        {
            if (!questProgress.ContainsKey(quest)) continue;

            var progressDictionary = questProgress[quest];

            foreach (var objective in quest.questObjectives)
            {
                // Nếu quái vừa chết ĐÚNG với loại quái đang cần đánh
                if (objective.targetEnemyType == deadEnemyType && deadEnemyType != EnemyType.None)
                {
                    if (!progressDictionary.ContainsKey(objective))
                        progressDictionary[objective] = 0;

                    // Nếu chưa giết đủ số lượng
                    if (progressDictionary[objective] < objective.requiredAmount)
                    {
                        progressDictionary[objective]++;

                        // Báo cáo UI vẽ lại số lượng quái
                        OnQuestProgressUpdated?.Invoke();
                    }
                }
            }
        }
    }

    // Lấy ra con số hiện tại trong sổ tay
    public int GetCurrentAmount(QuestSO questSO, QuestObjective objective)
    {
        if (questProgress.TryGetValue(questSO, out var objectiveDictionary))
        {
            if (objectiveDictionary.TryGetValue(objective, out var amount))
            {
                return amount;
            }
        }
        return 0;
    }

    // Chuẩn bị đoạn chữ để hiển thị ra màn hình UI (VD: "2/3" hoặc "Complete")
    public string GetProgressText(QuestSO questSO, QuestObjective objective)
    {
        int currentAmount = GetCurrentAmount(questSO, objective);

        if (currentAmount >= objective.requiredAmount)
            return "Complete";
        else if (objective.requiresMovement || objective.targetEnemyType != EnemyType.None)
            return $"{currentAmount}/{objective.requiredAmount}";
        else
            return "In Progress";
    }
}