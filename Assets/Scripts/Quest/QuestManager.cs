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
            foreach (var reward in questSO.rewards)
            {
                InventoryUI.instance.AddItems(reward.itemData, reward.quantity);
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
                if (objective.requiresMovement && progressDictionary[objective] < objective.requiredAmount)
                {
                    if (!movementTimers.ContainsKey(objective))
                        movementTimers[objective] = 0f;

                    movementTimers[objective] += deltaTime;

                    if (movementTimers[objective] >= 1f)
                    {
                        int secondsToAdd = Mathf.FloorToInt(movementTimers[objective]);
                        movementTimers[objective] -= secondsToAdd;
                        progressDictionary[objective] += secondsToAdd;

                        if (progressDictionary[objective] > objective.requiredAmount)
                            progressDictionary[objective] = objective.requiredAmount;

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
            if (objectiveDictionary.TryGetValue(objective, out var amount)) return amount;
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
}