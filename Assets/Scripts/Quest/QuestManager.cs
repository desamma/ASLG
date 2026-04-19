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
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // GIỮ NGUYÊN QUA CÁC SCENE
        }
        else Destroy(gameObject);
    }

    public List<QuestSO> GetActiveQuests() => activeQuests;

    private void OnEnable() => EnemyQuestTarget.OnEnemyDied += HandleEnemyKill;
    private void OnDisable() => EnemyQuestTarget.OnEnemyDied -= HandleEnemyKill;

    public void AcceptQuest(QuestSO questSO)
    {
        if (!activeQuests.Contains(questSO)) activeQuests.Add(questSO);
        if (!questProgress.ContainsKey(questSO)) questProgress[questSO] = new Dictionary<QuestObjective, int>();

        foreach (var objective in questSO.questObjectives) UpdateObjectiveProgress(questSO, objective);
        Debug.Log($"[Quest] Đã nhận Quest: {questSO.questName}");
    }

    public bool HasCompletedQuest(QuestSO questSO) => completedQuests.Contains(questSO);

    public bool IsQuestComplete(QuestSO questSO)
    {
        if (!questProgress.ContainsKey(questSO)) return false;
        var progressDictionary = questProgress[questSO];

        foreach (var objective in questSO.questObjectives)
        {
            int currentAmount = progressDictionary.ContainsKey(objective) ? progressDictionary[objective] : 0;
            if (currentAmount < objective.requiredAmount) return false;
        }
        return true;
    }

    public void CompleteQuest(QuestSO questSO)
    {
        if (activeQuests.Contains(questSO)) activeQuests.Remove(questSO);
        if (!completedQuests.Contains(questSO)) completedQuests.Add(questSO);
        if (questProgress.ContainsKey(questSO)) questProgress.Remove(questSO);

        if (InventoryManager.instance != null)
        {
            foreach (var reward in questSO.rewards) InventoryManager.instance.AddItem(reward.itemID, reward.quantity);
            Debug.Log($"[Quest] Đã trả Quest '{questSO.questName}' và phát thưởng!");
        }
    }

    public void UpdateObjectiveProgress(QuestSO questSO, QuestObjective objective)
    {
        if (!questProgress.ContainsKey(questSO)) questProgress[questSO] = new Dictionary<QuestObjective, int>();
        if (!questProgress[questSO].ContainsKey(objective)) questProgress[questSO][objective] = 0;
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
                    if (!movementTimers.ContainsKey(objective)) movementTimers[objective] = 0f;
                    movementTimers[objective] += deltaTime;

                    if (movementTimers[objective] >= 1f)
                    {
                        int secondsToAdd = Mathf.FloorToInt(movementTimers[objective]);
                        movementTimers[objective] -= secondsToAdd;
                        progressDict[objective] += secondsToAdd;

                        if (progressDict[objective] > objective.requiredAmount) progressDict[objective] = objective.requiredAmount;
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
            var progressDict = questProgress[quest];

            foreach (var objective in quest.questObjectives)
            {
                if (objective.targetEnemyType == deadEnemyType && deadEnemyType != EnemyType.None)
                {
                    if (!progressDict.ContainsKey(objective)) progressDict[objective] = 0;
                    if (progressDict[objective] < objective.requiredAmount)
                    {
                        progressDict[objective]++;
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
        else if (objective.requiresMovement || objective.targetEnemyType != EnemyType.None) return $"{currentAmount}/{objective.requiredAmount}";
        else return "In Progress";
    }

    // =======================================================
    // API CHO SAVE MANAGER
    // =======================================================
    public QuestSaveData ExportSaveData()
    {
        QuestSaveData data = new QuestSaveData();
        foreach (var q in activeQuests) data.activeQuestIDs.Add(q.questID);
        foreach (var q in completedQuests) data.completedQuestIDs.Add(q.questID);

        foreach (var kvp in questProgress)
        {
            List<int> objProgress = new List<int>();
            foreach (var obj in kvp.Key.questObjectives)
            {
                objProgress.Add(kvp.Value.ContainsKey(obj) ? kvp.Value[obj] : 0);
            }
            data.questProgress[kvp.Key.questID] = objProgress;
        }
        return data;
    }

    public void ImportSaveData(QuestSaveData data)
    {
        activeQuests.Clear();
        completedQuests.Clear();
        questProgress.Clear();

        // Nạp tất cả QuestSO từ thư mục Resources/Data/Quests
        QuestSO[] allAvailableQuests = Resources.LoadAll<QuestSO>("Data/Quests");
        Dictionary<string, QuestSO> questDB = new Dictionary<string, QuestSO>();
        foreach (var q in allAvailableQuests) questDB[q.questID] = q;

        foreach (var id in data.activeQuestIDs) if (questDB.ContainsKey(id)) activeQuests.Add(questDB[id]);
        foreach (var id in data.completedQuestIDs) if (questDB.ContainsKey(id)) completedQuests.Add(questDB[id]);

        foreach (var kvp in data.questProgress)
        {
            if (questDB.ContainsKey(kvp.Key))
            {
                QuestSO q = questDB[kvp.Key];
                questProgress[q] = new Dictionary<QuestObjective, int>();
                for (int i = 0; i < q.questObjectives.Count; i++)
                {
                    if (i < kvp.Value.Count) questProgress[q][q.questObjectives[i]] = kvp.Value[i];
                }
            }
        }
        Debug.Log("[QuestManager] Đã nạp dữ liệu Quest thành công.");
    }
}