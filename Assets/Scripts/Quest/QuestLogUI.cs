using TMPro;
using UnityEngine;

public class QuestLogUI : MonoBehaviour
{
    [SerializeField] private QuestManager questManager;

    [Header("Reward Icons")]
    [SerializeField] private Sprite goldIcon;
    [SerializeField] private Sprite expIcon;

    [SerializeField] private TMP_Text questNameText;
    [SerializeField] private TMP_Text questDescriptionText;
    [SerializeField] private QuestObjectiveSlot[] objectiveSlots;
    [SerializeField] private QuestRewardSlot[] rewardSlots;

    [Header("Quest List")]
    [SerializeField] private QuestLogSlot[] questSlots;

    [Header("Canvas Groups")]
    [SerializeField] private CanvasGroup questCanvas;
    [SerializeField] private CanvasGroup acceptCanvas;
    [SerializeField] private CanvasGroup declineCanvas;
    [SerializeField] private CanvasGroup completeCanvas;

    private QuestSO questSO;

    public bool IsOpen => questCanvas != null && questCanvas.alpha > 0f;

    private void Start()
    {
        if (questManager != null)
            questManager.OnQuestProgressUpdated += RefreshCurrentQuestDisplay;
    }

    private void OnDestroy()
    {
        if (questManager != null)
            questManager.OnQuestProgressUpdated -= RefreshCurrentQuestDisplay;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.J))
            ToggleQuestUI();
    }

    public void ToggleQuestUI()
    {
        SetOpen(!IsOpen);
    }

    public void Toggle()
    {
        ToggleQuestUI();
    }

    public void Open()
    {
        SetOpen(true);
    }

    public void Close()
    {
        SetOpen(false);
    }

    public void SetOpen(bool visible)
    {
        if (questCanvas == null)
            return;

        if (!visible)
        {
            SetCanvasState(questCanvas, false);
            return;
        }

        SetCanvasState(questCanvas, true);

        foreach (var slot in questSlots)
        {
            if (slot.gameObject.activeSelf && slot.currentQuest != null)
            {
                OnQuestSlotClicked(slot.currentQuest);
                return;
            }
        }

        ClearRightPanel();
    }

    public void OnQuestSlotClicked(QuestSO clickedQuestSO)
    {
        if (clickedQuestSO == null || questManager == null)
            return;

        HandleQuestClick(clickedQuestSO);

        bool isAccepted = questManager.activeQuests.Contains(clickedQuestSO);
        bool isComplete = IsQuestComplete(clickedQuestSO);

        if (!isAccepted)
        {
            SetCanvasState(acceptCanvas, true);
            SetCanvasState(declineCanvas, true);
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
        if (questManager == null || questSO == null)
            return;

        questManager.AcceptQuest(questSO);
        SetCanvasState(acceptCanvas, false);
        SetCanvasState(completeCanvas, IsQuestComplete(questSO));
    }

    public void OnDeclineQuestClick()
    {
        if (questManager == null || questSO == null)
            return;

        if (questManager.activeQuests.Contains(questSO))
            questManager.AbandonQuest(questSO);

        RemoveQuestFromUI(questSO);
        ClearRightPanel();

        foreach (var slot in questSlots)
        {
            if (slot.gameObject.activeSelf && slot.currentQuest != null)
            {
                OnQuestSlotClicked(slot.currentQuest);
                return;
            }
        }
    }

    public void OnCompleteQuestClick()
    {
        if (questManager == null || questSO == null || !IsQuestComplete(questSO))
            return;

        questManager.CompleteQuest(questSO);
        RemoveQuestFromUI(questSO);
        ClearRightPanel();

        foreach (var slot in questSlots)
        {
            if (slot.gameObject.activeSelf && slot.currentQuest != null)
            {
                OnQuestSlotClicked(slot.currentQuest);
                return;
            }
        }
    }

    private void HandleQuestClick(QuestSO targetQuestSO)
    {
        if (targetQuestSO == null)
            return;

        questSO = targetQuestSO;
        questNameText.text = questSO.questName;
        questDescriptionText.text = questSO.questDescription;

        DisplayObjectives();
        DisplayRewards();
    }

    private void RefreshCurrentQuestDisplay()
    {
        if (!IsOpen || questSO == null)
            return;

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
        questSO = null;

        if (questNameText != null)
            questNameText.text = "NO QUEST SELECTED";

        if (questDescriptionText != null)
            questDescriptionText.text = string.Empty;

        foreach (var slot in objectiveSlots)
        {
            if (slot != null)
                slot.gameObject.SetActive(false);
        }

        foreach (var slot in rewardSlots)
        {
            if (slot != null)
                slot.gameObject.SetActive(false);
        }

        SetCanvasState(acceptCanvas, false);
        SetCanvasState(declineCanvas, false);
        SetCanvasState(completeCanvas, false);
    }

    private void DisplayObjectives()
    {
        if (questSO == null || objectiveSlots == null || questManager == null)
            return;

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
        if (questSO == null || rewardSlots == null)
            return;

        int currentSlotIndex = 0;

        if (questSO.rewardGold > 0 && currentSlotIndex < rewardSlots.Length)
        {
            rewardSlots[currentSlotIndex].gameObject.SetActive(true);
            rewardSlots[currentSlotIndex].DisplayReward(goldIcon, questSO.rewardGold);
            currentSlotIndex++;
        }

        if (questSO.rewardExp > 0 && currentSlotIndex < rewardSlots.Length)
        {
            rewardSlots[currentSlotIndex].gameObject.SetActive(true);
            rewardSlots[currentSlotIndex].DisplayReward(expIcon, questSO.rewardExp);
            currentSlotIndex++;
        }

        for (int i = 0; i < questSO.rewards.Count; i++)
        {
            if (currentSlotIndex >= rewardSlots.Length)
                break;

            var reward = questSO.rewards[i];
            rewardSlots[currentSlotIndex].gameObject.SetActive(true);

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

            rewardSlots[currentSlotIndex].DisplayReward(iconToDisplay, reward.quantity);
            currentSlotIndex++;
        }

        for (int i = currentSlotIndex; i < rewardSlots.Length; i++)
        {
            if (rewardSlots[i] != null)
                rewardSlots[i].gameObject.SetActive(false);
        }
    }

    private bool IsQuestComplete(QuestSO quest)
    {
        if (quest == null || questManager == null)
            return false;

        foreach (var objective in quest.questObjectives)
        {
            if (questManager.GetCurrentAmount(quest, objective) < objective.requiredAmount)
                return false;
        }

        return true;
    }

    private void SetCanvasState(CanvasGroup canvasGroup, bool state)
    {
        if (canvasGroup == null)
            return;

        canvasGroup.alpha = state ? 1f : 0f;
        canvasGroup.blocksRaycasts = state;
        canvasGroup.interactable = state;
    }

    private void RemoveQuestFromUI(QuestSO quest)
    {
        if (quest == null || questSlots == null)
            return;

        foreach (var slot in questSlots)
        {
            if (slot != null && slot.currentQuest == quest)
            {
                slot.ClearSlot();
                break;
            }
        }
    }
}