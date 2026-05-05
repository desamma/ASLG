using System.Collections.Generic;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public static QuestManager instance { get; private set; }

    [Header("Quest Lists")]
    public List<QuestSO> activeQuests = new List<QuestSO>();
    public List<QuestSO> completedQuests = new List<QuestSO>();

    [Header("Database (Auto-load)")]
    public List<QuestSO> allQuestsInGame = new List<QuestSO>();

    private Dictionary<QuestSO, Dictionary<QuestObjective, int>> questProgress = new();
    private Dictionary<QuestObjective, float> movementTimers = new();
    public Dictionary<string, long> dailyQuestCompletionTimes = new();

    public event System.Action OnQuestProgressUpdated;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    private void Start()
    {
        QuestSO[] loadedQuests = Resources.LoadAll<QuestSO>("Quests");
        allQuestsInGame.Clear();
        allQuestsInGame.AddRange(loadedQuests);

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

    public List<QuestSO> GetActiveQuests() => activeQuests;

    private void OnEnable() => EnemyQuestTarget.OnEnemyDied += HandleEnemyKill;
    private void OnDisable() => EnemyQuestTarget.OnEnemyDied -= HandleEnemyKill;


    public List<QuestSO> GetAvailableQuests(QuestType type)
    {
        List<QuestSO> availableQuests = new List<QuestSO>();

        foreach (var quest in allQuestsInGame)
        {
            if (activeQuests.Contains(quest) || completedQuests.Contains(quest))
                continue;

            if (quest.questType != type)
                continue;

            if (IsQuestUnlocked(quest))
            {
                availableQuests.Add(quest);
            }
        }

        return availableQuests;
    }

    private bool IsQuestUnlocked(QuestSO quest)
    {
        if (StatsManager.instance != null)
        {
            if (StatsManager.instance.level < quest.requiredLevel) return false;
        }

        if (!string.IsNullOrEmpty(quest.requiredMapUnlocked))
        {
            if (quest.requiredMapUnlocked.Trim() == "") return false;

            if (WorldMapManager.Instance != null)
            {
                bool isMapUnlocked = false;
                foreach (var zone in WorldMapManager.Instance.zones)
                {
                    if (zone.zoneName == quest.requiredMapUnlocked && zone.discovered)
                    {
                        isMapUnlocked = true;
                        break;
                    }
                }

                if (!isMapUnlocked) return false;
            }
            else return false;
        }

        if (quest.requiredPreviousQuest != null)
        {
            if (!HasCompletedQuest(quest.requiredPreviousQuest)) return false;
        }

        if (quest.requiredBossKilled != EnemyType.None)
        {
            // Placeholder for future boss kill logic
        }

        return true;
    }


    public void AcceptQuest(QuestSO questSO)
    {
        if (!activeQuests.Contains(questSO)) activeQuests.Add(questSO);
        if (!questProgress.ContainsKey(questSO)) questProgress[questSO] = new Dictionary<QuestObjective, int>();

        foreach (var objective in questSO.questObjectives)
        {
            if (!questProgress[questSO].ContainsKey(objective))
                questProgress[questSO][objective] = 0;
        }
    }

    public bool HasCompletedQuest(QuestSO questSO) => completedQuests.Contains(questSO);

    public void CompleteQuest(QuestSO questSO)
    {
        if (activeQuests.Contains(questSO)) activeQuests.Remove(questSO);
        if (!completedQuests.Contains(questSO)) completedQuests.Add(questSO);
        if (questProgress.ContainsKey(questSO)) questProgress.Remove(questSO);

        if (StatsManager.instance != null)
        {
            if (questSO.rewardGold > 0) StatsManager.instance.AddGold(questSO.rewardGold);
            if (questSO.rewardExp > 0) StatsManager.instance.AddExp(questSO.rewardExp);
        }

        if (InventoryManager.instance != null)
        {
            foreach (var reward in questSO.rewards)
            {
                string finalItemID = reward.itemID;
                if (reward.isRandomItem || string.IsNullOrEmpty(finalItemID)) finalItemID = ItemDatabase.GetRandomItemID();

                if (!string.IsNullOrEmpty(finalItemID))
                {
                    InventoryManager.instance.AddItem(finalItemID, reward.quantity);
                }
            }
        }

        if (questSO.questType == QuestType.DailyQuest && !string.IsNullOrEmpty(questSO.questID))
        {
            dailyQuestCompletionTimes[questSO.questID] = System.DateTime.Now.Ticks;
        }

        OnQuestProgressUpdated?.Invoke();
    }

    public void AbandonQuest(QuestSO questSO)
    {
        if (activeQuests.Contains(questSO)) activeQuests.Remove(questSO);
        if (questProgress.ContainsKey(questSO)) questProgress.Remove(questSO);

        OnQuestProgressUpdated?.Invoke();
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
        return $"{currentAmount}/{objective.requiredAmount}";
    }

    public void NotifyUIUpdate()
    {
        OnQuestProgressUpdated?.Invoke();
    }

    public void AdvanceQuestByReference(QuestSO targetQuest)
    {
        if (!activeQuests.Contains(targetQuest)) return;
        if (!questProgress.ContainsKey(targetQuest)) return;

        var progressDict = questProgress[targetQuest];

        foreach (var objective in targetQuest.questObjectives)
        {
            if (progressDict[objective] < objective.requiredAmount)
            {
                progressDict[objective]++;
                NotifyUIUpdate();
                return;
            }
        }
    }

    public void OnPlayerTeleported()
    {
        bool hasUpdated = false;

        foreach (var quest in activeQuests)
        {
            if (!questProgress.ContainsKey(quest)) continue;
            var progressDict = questProgress[quest];

            foreach (var objective in quest.questObjectives)
            {
                if (objective.requiresTeleport && progressDict[objective] < objective.requiredAmount)
                {
                    progressDict[objective]++;
                    hasUpdated = true;
                }
            }
        }

        if (hasUpdated)
        {
            NotifyUIUpdate();
        }
    }


    public QuestSaveData ExportSaveData()
    {
        QuestSaveData data = new QuestSaveData();

        foreach (var q in activeQuests)
        {
            if (!string.IsNullOrEmpty(q.questID)) data.questStatus[q.questID] = false;
        }

        foreach (var q in completedQuests)
        {
            if (!string.IsNullOrEmpty(q.questID)) data.questStatus[q.questID] = true;
        }

        data.dailyQuestCompletionTimes = new Dictionary<string, long>(dailyQuestCompletionTimes);

        return data;
    }

    public void ImportSaveData(QuestSaveData data)
    {
        if (data == null || data.questStatus == null) return;

        activeQuests.Clear();
        completedQuests.Clear();
        questProgress.Clear();

        if (data.dailyQuestCompletionTimes != null)
        {
            dailyQuestCompletionTimes = new Dictionary<string, long>(data.dailyQuestCompletionTimes);
        }

        foreach (var kvp in data.questStatus)
        {
            string qID = kvp.Key;
            bool isCompleted = kvp.Value;

            QuestSO q = allQuestsInGame.Find(quest => quest.questID == qID);

            if (q != null)
            {
                if (isCompleted)
                {
                    if (q.questType == QuestType.DailyQuest)
                    {
                        if (dailyQuestCompletionTimes.TryGetValue(qID, out long ticks))
                        {
                            System.DateTime completionTime = new System.DateTime(ticks);

                            if (System.DateTime.Now.Date > completionTime.Date)
                            {
                                dailyQuestCompletionTimes.Remove(qID);
                                continue;
                            }
                        }
                    }

                    completedQuests.Add(q);
                }
                else
                {
                    activeQuests.Add(q);
                    questProgress[q] = new Dictionary<QuestObjective, int>();
                    foreach (var obj in q.questObjectives)
                    {
                        questProgress[q][obj] = 0;
                    }
                }
            }
        }

        NotifyUIUpdate();
    }
}