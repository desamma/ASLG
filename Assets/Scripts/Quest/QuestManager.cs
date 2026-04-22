using System.Collections.Generic;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public static QuestManager instance { get; private set; }

    [Header("Danh sách Quest")]
    public List<QuestSO> activeQuests = new List<QuestSO>();
    public List<QuestSO> completedQuests = new List<QuestSO>();

    private Dictionary<QuestSO, Dictionary<QuestObjective, int>> questProgress = new();
    private Dictionary<QuestObjective, float> movementTimers = new();

    public event System.Action OnQuestProgressUpdated;

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // Khởi tạo sổ tay tiến độ cho các Quest đã kéo sẵn trong Inspector
        foreach (var quest in activeQuests)
        {
            if (!questProgress.ContainsKey(quest))
            {
                questProgress[quest] = new Dictionary<QuestObjective, int>();
                foreach (var objective in quest.questObjectives)
                {
                    questProgress[quest][objective] = 0;
                }
            }
        }
    }

    public List<QuestSO> GetActiveQuests()
    {
        return activeQuests;
    }

    private void OnEnable()
    {
        EnemyQuestTarget.OnEnemyDied += HandleEnemyKill;
    }

    private void OnDisable()
    {
        EnemyQuestTarget.OnEnemyDied -= HandleEnemyKill;
    }

    public void AcceptQuest(QuestSO questSO)
    {
        if (!activeQuests.Contains(questSO))
        {
            activeQuests.Add(questSO);
        }

        if (!questProgress.ContainsKey(questSO))
        {
            questProgress[questSO] = new Dictionary<QuestObjective, int>();
        }

        foreach (var objective in questSO.questObjectives)
        {
            if (!questProgress[questSO].ContainsKey(objective))
                questProgress[questSO][objective] = 0;
        }

        Debug.Log($"[Hệ Thống] Đã nhận Quest: {questSO.questName}");
    }

    public bool HasCompletedQuest(QuestSO questSO)
    {
        return completedQuests.Contains(questSO);
    }

    public void CompleteQuest(QuestSO questSO)
    {
        if (activeQuests.Contains(questSO))
            activeQuests.Remove(questSO);

        if (!completedQuests.Contains(questSO))
            completedQuests.Add(questSO);

        if (questProgress.ContainsKey(questSO))
            questProgress.Remove(questSO);

        if (InventoryUI.instance != null)
        {
            // InventoryUI currently does not expose an AddItems method in provided signatures.
            // Use reward data fields that exist in QuestReward (itemID and quantity) and log them.
            // Integrate with your inventory system implementation here when available.
            foreach (var reward in questSO.rewards)
            {
                Debug.Log($"[Hệ Thống] Thưởng nhận được: {reward.itemID} x{reward.quantity}");
            }
        }
    }

    public void AddMovementProgress(float deltaTime)
    {
        foreach (var quest in activeQuests)
        {
            if (!questProgress.ContainsKey(quest)) continue;
            var progressDict = questProgress[quest];

            foreach (var objective in quest.questObjectives)
            {
                if (objective.requiresMovement && progressDict[objective] < objective.requiredAmount)
                {
                    if (!movementTimers.ContainsKey(objective))
                        movementTimers[objective] = 0f;

                    movementTimers[objective] += deltaTime;

                    if (movementTimers[objective] >= 1f)
                    {
                        int secondsToAdd = Mathf.FloorToInt(movementTimers[objective]);
                        movementTimers[objective] -= secondsToAdd;
                        progressDict[objective] += secondsToAdd;

                        if (progressDict[objective] > objective.requiredAmount)
                            progressDict[objective] = objective.requiredAmount;

                        OnQuestProgressUpdated?.Invoke();
                    }
                }
            }
        }
    }

    private void HandleEnemyKill(EnemyType deadEnemyType)
    {
        foreach (var quest in activeQuests)
        {
            if (!questProgress.ContainsKey(quest)) continue;
            var progressDictionary = questProgress[quest];

            foreach (var objective in quest.questObjectives)
            {
                if (objective.targetEnemyType == deadEnemyType && deadEnemyType != EnemyType.None)
                {
                    if (!progressDictionary.ContainsKey(objective)) progressDictionary[objective] = 0;

                    if (progressDictionary[objective] < objective.requiredAmount)
                    {
                        progressDictionary[objective]++;
                        OnQuestProgressUpdated?.Invoke();
                    }
                }
            }
        }
    }

    public int GetCurrentAmount(QuestSO questSO, QuestObjective objective)
    {
        if (questProgress.TryGetValue(questSO, out var objDict))
        {
            if (objDict.TryGetValue(objective, out var amount)) return amount;
        }
        return 0;
    }

    public string GetProgressText(QuestSO questSO, QuestObjective objective)
    {
        int currentAmount = GetCurrentAmount(questSO, objective);
        if (currentAmount >= objective.requiredAmount) return "Complete";
        return $"{currentAmount}/{objective.requiredAmount}";
    }

    public void NotifyUIUpdate()
    {
        OnQuestProgressUpdated?.Invoke();
    }

    public void AbandonQuest(QuestSO questSO)
    {
        // 1. Xóa khỏi danh sách đang làm
        if (activeQuests.Contains(questSO))
        {
            activeQuests.Remove(questSO);
        }

        // 2. Xóa sổ tay tiến độ ngầm của nó đi
        if (questProgress.ContainsKey(questSO))
        {
            questProgress.Remove(questSO);
        }

        Debug.Log($"[Hệ Thống] Đã hủy bỏ nhiệm vụ: {questSO.questName}");

        // 3. Báo cho UI biết để cập nhật lại
        OnQuestProgressUpdated?.Invoke();
    }

    // ==========================================
    // LIÊN KẾT VỚI SAVE MANAGER (LƯU & TẢI GAME)
    // ==========================================

    public QuestSaveData ExportSaveData()
    {
        QuestSaveData data = new QuestSaveData();

        // Lưu danh sách Quest đang làm và Đã xong (Dùng tên file làm ID)
        foreach (var q in activeQuests) data.activeQuestIDs.Add(q.name);
        foreach (var q in completedQuests) data.completedQuestIDs.Add(q.name);

        // Lưu tiến độ đánh quái/di chuyển
        foreach (var kvp in questProgress)
        {
            List<int> progressList = new List<int>();
            foreach (var obj in kvp.Key.questObjectives)
            {
                progressList.Add(kvp.Value.ContainsKey(obj) ? kvp.Value[obj] : 0);
            }
            data.questProgress.Add(kvp.Key.name, progressList);
        }
        return data;
    }

    public void ImportSaveData(QuestSaveData data)
    {
        if (data == null) return;

        activeQuests.Clear();
        completedQuests.Clear();
        questProgress.Clear();

        // Tải lại các Quest đang làm từ thư mục Resources/Quests
        foreach (var id in data.activeQuestIDs)
        {
            QuestSO q = Resources.Load<QuestSO>("Quests/" + id);
            if (q != null) activeQuests.Add(q);
        }

        // Tải lại các Quest đã xong
        foreach (var id in data.completedQuestIDs)
        {
            QuestSO q = Resources.Load<QuestSO>("Quests/" + id);
            if (q != null) completedQuests.Add(q);
        }

        // Tải lại tiến độ đánh quái
        foreach (var kvp in data.questProgress)
        {
            QuestSO q = Resources.Load<QuestSO>("Quests/" + kvp.Key);
            if (q != null && !questProgress.ContainsKey(q))
            {
                questProgress[q] = new Dictionary<QuestObjective, int>();
                for (int i = 0; i < q.questObjectives.Count; i++)
                {
                    if (i < kvp.Value.Count)
                    {
                        questProgress[q][q.questObjectives[i]] = kvp.Value[i];
                    }
                }
            }
        }

        NotifyUIUpdate();
    }
}