using System.Collections.Generic;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public static QuestManager instance { get; private set; }

    [Header("Quest Lists")]
    public List<QuestSO> activeQuests = new List<QuestSO>();
    public List<QuestSO> completedQuests = new List<QuestSO>();

    [Header("Database (Auto-load)")]
    // List containing ALL quests available in the game
    public List<QuestSO> allQuestsInGame = new List<QuestSO>();

    // Tracking dictionaries
    private Dictionary<QuestSO, Dictionary<QuestObjective, int>> questProgress = new();
    private Dictionary<QuestObjective, float> movementTimers = new();
    public Dictionary<string, long> dailyQuestCompletionTimes = new();

    public event System.Action OnQuestProgressUpdated;

    private void Awake()
    {
        // Singleton setup to keep this manager across scene loads
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    private void Start()
    {
        // Load all QuestSO files located in the Resources/Quests folder
        QuestSO[] loadedQuests = Resources.LoadAll<QuestSO>("Quests");
        allQuestsInGame.Clear();
        allQuestsInGame.AddRange(loadedQuests);

        // Initialize progress tracking for quests that are already active
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

    // Subscribe to enemy death events
    private void OnEnable() => EnemyQuestTarget.OnEnemyDied += HandleEnemyKill;
    private void OnDisable() => EnemyQuestTarget.OnEnemyDied -= HandleEnemyKill;


    public List<QuestSO> GetAvailableQuests(QuestType type)
    {
        List<QuestSO> availableQuests = new List<QuestSO>();

        foreach (var quest in allQuestsInGame)
        {
            // 1. Skip if the quest is already active or completed
            if (activeQuests.Contains(quest) || completedQuests.Contains(quest))
                continue;

            // 2. Filter by quest tab type (Main or Daily)
            if (quest.questType != type)
                continue;

            // 3. Check if player meets all unlock conditions
            if (IsQuestUnlocked(quest))
            {
                availableQuests.Add(quest);
            }
        }

        return availableQuests;
    }

    private bool IsQuestUnlocked(QuestSO quest)
    {
        // 1. Check Level Requirement
        if (StatsManager.instance != null)
        {
            if (StatsManager.instance.level < quest.requiredLevel) return false;
        }

        // 2. Check Map Unlock Requirement
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

        // 3. Check Prerequisite Quest Requirement
        if (quest.requiredPreviousQuest != null)
        {
            if (!HasCompletedQuest(quest.requiredPreviousQuest)) return false;
        }

        // 4. Check Boss Killed Requirement (To be implemented later)
        if (quest.requiredBossKilled != EnemyType.None)
        {
            // Placeholder for future boss kill logic
        }

        // Passes all checks
        return true;
    }


    public void AcceptQuest(QuestSO questSO)
    {
        // Add to active list and initialize progress dictionary
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
        // Move quest from active to completed list and clear progress
        if (activeQuests.Contains(questSO)) activeQuests.Remove(questSO);
        if (!completedQuests.Contains(questSO)) completedQuests.Add(questSO);
        if (questProgress.ContainsKey(questSO)) questProgress.Remove(questSO);

        // 1. Grant Gold and Exp Rewards
        if (StatsManager.instance != null)
        {
            if (questSO.rewardGold > 0) StatsManager.instance.AddGold(questSO.rewardGold);
            if (questSO.rewardExp > 0) StatsManager.instance.AddExp(questSO.rewardExp);
        }

        // 2. Grant Item Rewards
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

        // 3. Record completion time for Daily Quests to handle future resets
        if (questSO.questType == QuestType.DailyQuest && !string.IsNullOrEmpty(questSO.questID))
        {
            dailyQuestCompletionTimes[questSO.questID] = System.DateTime.Now.Ticks;
        }

        OnQuestProgressUpdated?.Invoke();
    }

    public void AbandonQuest(QuestSO questSO)
    {
        // Remove from active list and clear progress tracking
        if (activeQuests.Contains(questSO)) activeQuests.Remove(questSO);
        if (questProgress.ContainsKey(questSO)) questProgress.Remove(questSO);

        OnQuestProgressUpdated?.Invoke();
    }

    public void AddMovementProgress(float deltaTime)
    {
        // Track movement time for quests requiring movement
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
        // Update kill count for quests targeting the specific enemy type
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
        // Retrieve current progress amount for UI display
        if (questProgress.TryGetValue(questSO, out var objDict))
        {
            if (objDict.TryGetValue(objective, out var amount)) return amount;
        }
        return 0;
    }

    public string GetProgressText(QuestSO questSO, QuestObjective objective)
    {
        // Format progress text (e.g., "3/5" or "Complete")
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
        // Advance progress manually by interacting with specific objects (e.g., doors)
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
        // Advance progress for teleport-related objectives
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
        // Package active, completed, and daily quest completion times for saving
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
        // Restore quest states from save data and handle daily quest resets
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
                    // Handle Daily Quest resets if 24 hours have passed since completion
                    if (q.questType == QuestType.DailyQuest)
                    {
                        if (dailyQuestCompletionTimes.TryGetValue(qID, out long ticks))
                        {
                            System.DateTime completionTime = new System.DateTime(ticks);

                            if (System.DateTime.Now.Date > completionTime.Date)
                            {
                                dailyQuestCompletionTimes.Remove(qID);
                                continue; // Skip adding to completed list so it can be accepted again
                            }
                        }
                    }

                    completedQuests.Add(q);
                }
                else
                {
                    // Restore active quests and initialize their progress to 0
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