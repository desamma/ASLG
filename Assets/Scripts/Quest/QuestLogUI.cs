using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class QuestLogUI : MonoBehaviour
{
    [SerializeField] private QuestManager questManager;

    [Header("Reward Icons")]
    [SerializeField] private Sprite goldIcon;
    [SerializeField] private Sprite expIcon;

    [Header("Right Panel UI")]
    [SerializeField] private TMP_Text questNameText;
    [SerializeField] private TMP_Text questDescriptionText;
    [SerializeField] private QuestObjectiveSlot[] objectiveSlots;
    [SerializeField] private QuestRewardSlot[] rewardSlots;

    [Header("Quest List (Scroll View)")]
    [Tooltip("Kéo thả GameObject 'Content' của Scroll View vào đây")]
    [SerializeField] private Transform questContentContainer;
    [Tooltip("Kéo thả Prefab QuestSlot từ Project vào đây")]
    [SerializeField] private GameObject questSlotPrefab;

    [Header("Tab Buttons")]
    [SerializeField] private Button btnMainTab;
    [SerializeField] private Button btnDailyTab;

    [Header("Canvas Groups")]
    [SerializeField] private CanvasGroup questCanvas;
    [SerializeField] private CanvasGroup acceptCanvas;
    [SerializeField] private CanvasGroup declineCanvas;
    [SerializeField] private CanvasGroup completeCanvas;

    private QuestSO questSO;
    private QuestType currentTabType = QuestType.MainQuest;
    private bool isShowingAllQuests = true;
    private List<QuestLogSlot> activeSlotUIs = new List<QuestLogSlot>();

    public bool IsOpen => questCanvas != null && questCanvas.alpha > 0f;


    private void Start()
    {
        // Listen to quest progress updates to refresh UI automatically
        if (questManager != null)
            questManager.OnQuestProgressUpdated += RefreshCurrentQuestDisplay;

        // Setup tab button listeners
        if (btnMainTab != null)
            btnMainTab.onClick.AddListener(() => SwitchTab(QuestType.MainQuest));

        if (btnDailyTab != null)
            btnDailyTab.onClick.AddListener(() => SwitchTab(QuestType.DailyQuest));
    }

    private void OnDestroy()
    {
        // Always unsubscribe from events to prevent memory leaks
        if (questManager != null)
            questManager.OnQuestProgressUpdated -= RefreshCurrentQuestDisplay;
    }

    private void Update()
    {
        // Press 'J' to open/close the Quest Log
        if (Input.GetKeyDown(KeyCode.J) && !LLMChatManager.Instance.IsChatting)
            ToggleQuestUI();
    }

    public void ToggleQuestUI() => SetOpen(!IsOpen);
    public void Toggle() => ToggleQuestUI();
    public void Open() => SetOpen(true);
    public void Close() => SetOpen(false);

    public void SetOpen(bool visible)
    {
        if (questCanvas == null) return;

        if (!visible)
        {
            SetCanvasState(questCanvas, false);
            return;
        }

        SetCanvasState(questCanvas, true);

        // Reset to show all quests when the UI is opened
        isShowingAllQuests = true;
        UpdateButtonVisuals();
        LoadQuestsForCurrentTab();
    }

    private void SwitchTab(QuestType newTabType)
    {
        // Change the active tab and reload the quest list
        isShowingAllQuests = false;
        currentTabType = newTabType;
        UpdateButtonVisuals();
        LoadQuestsForCurrentTab();
    }

    private void UpdateButtonVisuals()
    {
        // Disable the button of the currently active tab
        if (btnMainTab != null)
            btnMainTab.interactable = isShowingAllQuests || currentTabType != QuestType.MainQuest;

        if (btnDailyTab != null)
            btnDailyTab.interactable = isShowingAllQuests || currentTabType != QuestType.DailyQuest;
    }

    private void LoadQuestsForCurrentTab()
    {
        if (questManager == null || questContentContainer == null || questSlotPrefab == null) return;

        // 1. Clear old UI slots before spawning new ones
        foreach (var slotUI in activeSlotUIs)
        {
            if (slotUI != null) Destroy(slotUI.gameObject);
        }
        activeSlotUIs.Clear();
        ClearRightPanel();

        // 2. Filter quests based on the current tab (All, Main, or Daily)
        var questsToShow = new List<QuestSO>();

        if (isShowingAllQuests)
        {
            questsToShow.AddRange(questManager.GetActiveQuests());
            questsToShow.AddRange(questManager.GetAvailableQuests(QuestType.MainQuest));
            questsToShow.AddRange(questManager.GetAvailableQuests(QuestType.DailyQuest));
        }
        else
        {
            foreach (var q in questManager.GetActiveQuests())
            {
                if (q.questType == currentTabType) questsToShow.Add(q);
            }
            questsToShow.AddRange(questManager.GetAvailableQuests(currentTabType));
        }

        // 3. Instantiate UI prefabs for each filtered quest
        foreach (var quest in questsToShow)
        {
            GameObject newSlotObj = Instantiate(questSlotPrefab, questContentContainer);
            QuestLogSlot slotScript = newSlotObj.GetComponent<QuestLogSlot>();

            if (slotScript == null) continue;

            slotScript.Initialize(quest);
            QuestSO capturedQuest = quest;

            if (newSlotObj.GetComponent<StopScrollPropagation>() == null)
                newSlotObj.AddComponent<StopScrollPropagation>();

            // Setup click event for the quest slot
            Button slotBtn = newSlotObj.GetComponent<Button>();
            if (slotBtn != null)
            {
                slotBtn.onClick.RemoveAllListeners();
                slotBtn.onClick.AddListener(() =>
                {
                    OnQuestSlotClicked(capturedQuest);
                });
            }
            else
            {
                EventTrigger trigger = newSlotObj.GetComponent<EventTrigger>()
                                    ?? newSlotObj.AddComponent<EventTrigger>();
                trigger.triggers.Clear();
                var entry = new EventTrigger.Entry
                {
                    eventID = EventTriggerType.PointerClick
                };
                entry.callback.AddListener((_) => OnQuestSlotClicked(capturedQuest));
                trigger.triggers.Add(entry);
            }

            activeSlotUIs.Add(slotScript);
        }

        // Auto-select the first quest in the list if available
        if (questsToShow.Count > 0)
            OnQuestSlotClicked(questsToShow[0]);
    }


    public void OnQuestSlotClicked(QuestSO clickedQuestSO)
    {
        if (clickedQuestSO == null || questManager == null) return;

        // Display quest details on the right panel
        HandleQuestClick(clickedQuestSO);

        // Check quest status to show the correct action buttons (Accept/Decline/Complete)
        bool isAccepted = questManager.activeQuests.Contains(clickedQuestSO);
        bool isComplete = IsQuestComplete(clickedQuestSO);

        if (!isAccepted)
        {
            SetCanvasState(acceptCanvas, true);
            SetCanvasState(declineCanvas, false);
            SetCanvasState(completeCanvas, false);
        }
        else
        {
            SetCanvasState(acceptCanvas, false);
            SetCanvasState(declineCanvas, true);
            SetCanvasState(completeCanvas, isComplete);
        }
    }

    public void OnAcceptQuestClick()
    {
        if (questManager == null || questSO == null) return;

        questManager.AcceptQuest(questSO);
        SetCanvasState(acceptCanvas, false);
        SetCanvasState(declineCanvas, true);
        SetCanvasState(completeCanvas, IsQuestComplete(questSO));
        LoadQuestsForCurrentTab();
    }

    public void OnDeclineQuestClick()
    {
        if (questManager == null || questSO == null) return;

        if (questManager.activeQuests.Contains(questSO))
            questManager.AbandonQuest(questSO);

        LoadQuestsForCurrentTab();
    }

    public void OnCompleteQuestClick()
    {
        if (questManager == null || questSO == null || !IsQuestComplete(questSO)) return;

        questManager.CompleteQuest(questSO);
        LoadQuestsForCurrentTab();
    }

    private void HandleQuestClick(QuestSO targetQuestSO)
    {
        // Update Title and Description
        if (targetQuestSO == null) return;

        questSO = targetQuestSO;
        questNameText.text = questSO.questName;
        questDescriptionText.text = questSO.questDescription;
        DisplayObjectives();
        DisplayRewards();
    }

    private void RefreshCurrentQuestDisplay()
    {
        // Automatically updates the objectives and complete button when progress changes
        if (!IsOpen || questSO == null) return;

        DisplayObjectives();

        if (completeCanvas != null && IsQuestComplete(questSO))
        {
            SetCanvasState(acceptCanvas, false);
            SetCanvasState(declineCanvas, true);
            SetCanvasState(completeCanvas, true);
        }
    }

    private void ClearRightPanel()
    {
        // Hides all data when no quest is selected
        questSO = null;

        if (questNameText != null) questNameText.text = "NO QUEST SELECTED";
        if (questDescriptionText != null) questDescriptionText.text = string.Empty;

        foreach (var slot in objectiveSlots) { if (slot != null) slot.gameObject.SetActive(false); }
        foreach (var slot in rewardSlots) { if (slot != null) slot.gameObject.SetActive(false); }

        SetCanvasState(acceptCanvas, false);
        SetCanvasState(declineCanvas, false);
        SetCanvasState(completeCanvas, false);
    }

    private void DisplayObjectives()
    {
        // Maps quest objectives to UI slots and updates their progress text
        if (questSO == null || objectiveSlots == null || questManager == null) return;

        for (int i = 0; i < objectiveSlots.Length; i++)
        {
            if (i < questSO.questObjectives.Count)
            {
                var objective = questSO.questObjectives[i];
                int currentAmount = questManager.GetCurrentAmount(questSO, objective);
                string progress = questManager.GetProgressText(questSO, objective);
                bool isCompleted = currentAmount >= objective.requiredAmount;

                if (objectiveSlots[i] != null)
                {
                    objectiveSlots[i].gameObject.SetActive(true);
                    objectiveSlots[i].RefreshObjectives(objective.description, progress, isCompleted);
                }
            }
            else if (objectiveSlots[i] != null)
            {
                objectiveSlots[i].gameObject.SetActive(false);
            }
        }
    }

    private void DisplayRewards()
    {
        // Populates the reward slots with Gold, Exp, and Items dynamically
        if (questSO == null || rewardSlots == null) return;

        int idx = 0;

        if (questSO.rewardGold > 0 && idx < rewardSlots.Length)
        {
            rewardSlots[idx].gameObject.SetActive(true);
            rewardSlots[idx].DisplayReward(goldIcon, questSO.rewardGold);
            idx++;
        }

        if (questSO.rewardExp > 0 && idx < rewardSlots.Length)
        {
            rewardSlots[idx].gameObject.SetActive(true);
            rewardSlots[idx].DisplayReward(expIcon, questSO.rewardExp);
            idx++;
        }

        for (int i = 0; i < questSO.rewards.Count; i++)
        {
            if (idx >= rewardSlots.Length) break;

            var reward = questSO.rewards[i];
            rewardSlots[idx].gameObject.SetActive(true);

            Sprite iconToDisplay;
            if (reward.isRandomItem || string.IsNullOrEmpty(reward.itemID))
            {
                iconToDisplay = Resources.Load<Sprite>("Icons/missing_icon");
            }
            else
            {
                ItemDefinition itemDef = ItemDatabase.GetItem(reward.itemID);
                iconToDisplay = itemDef?.GetIcon();
            }

            rewardSlots[idx].DisplayReward(iconToDisplay, reward.quantity);
            idx++;
        }

        // Hide unused reward slots
        for (int i = idx; i < rewardSlots.Length; i++)
        {
            if (rewardSlots[i] != null) rewardSlots[i].gameObject.SetActive(false);
        }
    }

    private bool IsQuestComplete(QuestSO quest)
    {
        // Checks if all objectives in the quest have met their required amount
        if (quest == null || questManager == null) return false;

        foreach (var objective in quest.questObjectives)
        {
            if (questManager.GetCurrentAmount(quest, objective) < objective.requiredAmount)
                return false;
        }

        return true;
    }

    private void SetCanvasState(CanvasGroup canvasGroup, bool state)
    {
        // Utility method to easily show/hide UI CanvasGroups
        if (canvasGroup == null) return;

        canvasGroup.alpha = state ? 1f : 0f;
        canvasGroup.blocksRaycasts = state;
        canvasGroup.interactable = state;
    }
}