using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class QuestLogUI : MonoBehaviour
{
    [SerializeField] private QuestManager questManager;

    [Header("UI Của Màn Hình Chi Tiết (Bên Phải)")]
    [SerializeField] private TMP_Text questNameText;
    [SerializeField] private TMP_Text questDescriptionText;
    [SerializeField] private QuestObjectiveSlot[] objectiveSlots;
    [SerializeField] private QuestRewardSlot[] rewardSlots;

    private QuestSO questSO;

    // Tracks whether the current right-panel display was triggered from the quest log (left panel)
    private bool isViewingFromQuestLog = false;

    [Header("Danh Sách Các Nút Quest Đang Làm (Bên Trái)")]
    [SerializeField] private QuestLogSlot[] questSlots;

    [Header("Các Cụm UI Canvas (Bật/Tắt)")]
    [SerializeField] private CanvasGroup questCanvas;
    [SerializeField] private CanvasGroup acceptCanvas;
    [SerializeField] private CanvasGroup declineCanvas;
    [SerializeField] private CanvasGroup completeCanvas;

    private void Start()
    {
        if (questManager != null)
        {
            questManager.OnQuestProgressUpdated += RefreshCurrentQuestDisplay;
        }
    }

    private void OnDestroy()
    {
        if (questManager != null)
        {
            questManager.OnQuestProgressUpdated -= RefreshCurrentQuestDisplay;
        }
    }

    private void RefreshCurrentQuestDisplay()
    {
        if (questCanvas.alpha > 0 && questSO != null)
        {
            DisplayObjectives();

            // Re-evaluate buttons if viewing from the quest log,
            // so Complete appears automatically when all objectives are done
            if (isViewingFromQuestLog)
            {
                bool isComplete = IsQuestComplete(questSO);
                SetCanvasState(declineCanvas, !isComplete);
                SetCanvasState(completeCanvas, isComplete);
            }
        }
    }

    // ==========================================

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.J))
        {
            ToggleQuestUI();
        }
    }

    public void ToggleQuestUI()
    {
        bool isShowing = questCanvas.alpha > 0;

        if (isShowing)
        {
            questCanvas.alpha = 0;
            questCanvas.blocksRaycasts = false;
            questCanvas.interactable = false;
        }
        else
        {
            questCanvas.alpha = 1;
            questCanvas.blocksRaycasts = true;
            questCanvas.interactable = true;

            RefreshQuestList();
            RefreshCurrentQuestDisplay();
        }
    }

    public void ShowQuestOffer(QuestSO incomingQuestSO)
    {
        if (questManager.activeQuests.Contains(incomingQuestSO) || questManager.HasCompletedQuest(incomingQuestSO))
        {
            SetCanvasState(questCanvas, false);
            return;
        }

        isViewingFromQuestLog = false;
        HandleQuestClick(incomingQuestSO);

        SetCanvasState(questCanvas, true);
        SetCanvasState(acceptCanvas, true);
        SetCanvasState(declineCanvas, true);
        SetCanvasState(completeCanvas, false);
    }

    public void ShowQuestTurnIn(QuestSO incomingQuestSO)
    {
        isViewingFromQuestLog = false;
        HandleQuestClick(incomingQuestSO);

        SetCanvasState(questCanvas, true);
        SetCanvasState(acceptCanvas, false);
        SetCanvasState(declineCanvas, false);
        SetCanvasState(completeCanvas, true);
    }

    public void OnAcceptQuestClick()
    {
        questManager.AcceptQuest(questSO);
        SetCanvasState(acceptCanvas, false);
        SetCanvasState(declineCanvas, false);
        RefreshQuestList();
    }

    public void OnDeclineQuestClick()
    {
        SetCanvasState(questCanvas, false);
    }

    public void OnCompleteQuestClick()
    {
        questManager.CompleteQuest(questSO);
        SetCanvasState(completeCanvas, false);
        if (questSO != null) questManager.AcceptQuest(questSO);

        SetCanvasState(acceptCanvas, false);
        SetCanvasState(declineCanvas, false);

        RefreshQuestList();
    }

    // ==========================================

    public void OnDeclineQuestClick()
    {
        if (questSO == null) return;

        // Always abandon the quest (works both from quest offer and from left panel)
        if (questManager.activeQuests.Contains(questSO))
        {
            questManager.AbandonQuest(questSO);
        }

        // Hide Accept/Decline buttons
        SetCanvasState(acceptCanvas, false);
        SetCanvasState(declineCanvas, false);

        // Refresh the left panel — the abandoned quest is now gone
        RefreshQuestList();

        // Auto-select the next quest, or clear the right panel if none remain
        if (questManager.activeQuests.Count > 0)
        {
            // Select from quest log mode so Decline button shows again
            HandleQuestClickFromLog(questManager.activeQuests[0]);
        }
        else
        {
            ClearRightPanel();
        }
    }

    public void OnCompleteQuestClick()
    {
        if (questSO != null) questManager.CompleteQuest(questSO);

        SetCanvasState(completeCanvas, false);

        RefreshQuestList();

        if (questManager.activeQuests.Count > 0)
        {
            HandleQuestClick(questManager.activeQuests[0]);
        }
        else
        {
            questNameText.text = "";
            questDescriptionText.text = "NO QUEST SELECTED";
            foreach (var slot in objectiveSlots) slot.gameObject.SetActive(false);
            foreach (var slot in rewardSlots) slot.gameObject.SetActive(false);
        }
    }

            ClearRightPanel();
        }
    }

    // ==========================================

    private void SetCanvasState(CanvasGroup canvasGroup, bool state)
    {
        if (canvasGroup == null) return;
        canvasGroup.alpha = state ? 1 : 0;
        canvasGroup.blocksRaycasts = state;
        canvasGroup.interactable = state;
    }

    public void RefreshQuestList()
    {
        List<QuestSO> activeQuests = questManager.GetActiveQuests();

        for (int i = 0; i < questSlots.Length; i++)
        {
            if (i < activeQuests.Count)
            {
                questSlots[i].SetQuests(activeQuests[i]);
            }
            else
            {
                questSlots[i].ClearSlot();
            }
        }
    }

    public void HandleQuestClick(QuestSO questSO)
    {
        this.questSO = questSO;
    // Called when clicking a quest from the LEFT PANEL (quest log).
    // Displays quest details on the right and shows the Decline button so the player can abandon it.
    public void HandleQuestClickFromLog(QuestSO clickedQuestSO)
    {
        isViewingFromQuestLog = true;
        HandleQuestClick(clickedQuestSO);

        bool isComplete = IsQuestComplete(clickedQuestSO);

        // Hide Accept always (quest is already active)
        // Show Complete only if all objectives are done, otherwise show Decline
        SetCanvasState(acceptCanvas, false);
        SetCanvasState(declineCanvas, !isComplete);
        SetCanvasState(completeCanvas, isComplete);
    }

    private bool IsQuestComplete(QuestSO quest)
    {
        foreach (var objective in quest.questObjectives)
        {
            if (questManager.GetCurrentAmount(quest, objective) < objective.requiredAmount)
                return false;
        }
        return true;
    }

    // Core display logic — populates the right panel. Does NOT touch canvas states.
    public void HandleQuestClick(QuestSO questSO)
    {
        this.questSO = questSO;

        questNameText.text = questSO.questName;
        questDescriptionText.text = questSO.questDescription;

        DisplayObjectives();
        DisplayRewards();
    }

    private void ClearRightPanel()
    {
        questSO = null;
        isViewingFromQuestLog = false;
        questNameText.text = "NO QUEST SELECTED";
        questDescriptionText.text = "";
        foreach (var slot in objectiveSlots) { if (slot != null) slot.gameObject.SetActive(false); }
        foreach (var slot in rewardSlots) { if (slot != null) slot.gameObject.SetActive(false); }
    }

    private void DisplayObjectives()
    {
        for (int i = 0; i < objectiveSlots.Length; i++)
        {
            if (i < questSO.questObjectives.Count)
            {
                var objective = questSO.questObjectives[i];
                int currentAmount = questManager.GetCurrentAmount(questSO, objective);
                string progress = questManager.GetProgressText(questSO, objective);
                bool isCompleted = currentAmount >= objective.requiredAmount;

                int curruntAmount = questManager.GetCurrentAmount(questSO, objective);
                string progress = questManager.GetProgressText(questSO, objective);
                bool isCompleted = curruntAmount >= objective.requiredAmount;

                objectiveSlots[i].gameObject.SetActive(true);
                objectiveSlots[i].RefreshObjectives(objective.description, progress, isCompleted);
            }
            else
            {
                objectiveSlots[i].gameObject.SetActive(false);
            }
        }
    }

    private void DisplayRewards()
    {
        for (int i = 0; i < rewardSlots.Length; i++)
        {
            if (i < questSO.rewards.Count)
            {
                var reward = questSO.rewards[i];
                rewardSlots[i].gameObject.SetActive(true);

                // ĐÃ SỬA: Tìm thông tin Item từ ItemDatabase để lấy Icon
                ItemDefinition itemDef = ItemDatabase.GetItem(reward.itemID);
                Sprite iconToDisplay = (itemDef != null) ? itemDef.GetIcon() : null;

                rewardSlots[i].DisplayReward(iconToDisplay, reward.quantity);
            }
            else
            {
                rewardSlots[i].gameObject.SetActive(false);
            }
        }
    }
}
