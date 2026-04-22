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

    // ĐÃ FIX LỖI 1: Thêm lại biến kiểm tra trạng thái
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
        if (questCanvas != null && questCanvas.alpha > 0 && questSO != null)
        {
            DisplayObjectives();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.J))
        {
            ToggleQuestUI();
        }
    }

    public void ToggleQuestUI()
    {
        if (questCanvas == null) return;

        bool isShowing = questCanvas.alpha > 0;

        if (isShowing)
        {
            SetCanvasState(questCanvas, false);
        }
        else
        {
            questCanvas.alpha = 1;
            questCanvas.blocksRaycasts = true;
            questCanvas.interactable = true;
            RefreshCurrentQuestDisplay();
        }
    }

    public void ShowQuestOffer(QuestSO incomingQuestSO)
    {
        if (questManager == null || incomingQuestSO == null) return;

        if (questManager.activeQuests.Contains(incomingQuestSO) || questManager.HasCompletedQuest(incomingQuestSO))
        {
            SetCanvasState(questCanvas, false);
            return;
        }

        HandleQuestClick(incomingQuestSO);
        SetCanvasState(questCanvas, true);
        SetCanvasState(acceptCanvas, true);
        SetCanvasState(declineCanvas, true);
        SetCanvasState(completeCanvas, false);
    }

    public void ShowQuestTurnIn(QuestSO incomingQuestSO)
    {
        if (incomingQuestSO == null) return;

        isViewingFromQuestLog = false;
        HandleQuestClick(incomingQuestSO);
        SetCanvasState(questCanvas, true);
        SetCanvasState(acceptCanvas, false);
        SetCanvasState(declineCanvas, false);
        SetCanvasState(completeCanvas, true);
    }

    public void OnAcceptQuestClick()
    {
        if (questManager == null || questSO == null) return;

        questManager.AcceptQuest(questSO);
        SetCanvasState(acceptCanvas, false);
        SetCanvasState(declineCanvas, false);
        RefreshQuestList();
    }

    public void OnDeclineQuestClick()
    {
        if (questManager == null) return;
        if (questSO == null)
        {
            SetCanvasState(questCanvas, false);
            return;
        }

        if (questManager.activeQuests.Contains(questSO))
        {
            questManager.AbandonQuest(questSO);
        }

        SetCanvasState(acceptCanvas, false);
        SetCanvasState(declineCanvas, false);

        RefreshQuestList();

        if (questManager.activeQuests.Count > 0)
        {
            HandleQuestClickFromLog(questManager.activeQuests[0]);
        }
        else
        {
            ClearRightPanel();
        }
    }

    public void OnCompleteQuestClick()
    {
        if (questManager == null) return;

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

    private void SetCanvasState(CanvasGroup canvasGroup, bool state)
    {
        if (canvasGroup == null) return;
        canvasGroup.alpha = state ? 1 : 0;
        canvasGroup.blocksRaycasts = state;
        canvasGroup.interactable = state;
    }

    public void RefreshQuestList()
    {
        if (questManager == null) return;

        List<QuestSO> activeQuests = questManager.GetActiveQuests() ?? new List<QuestSO>();

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

    public void HandleQuestClickFromLog(QuestSO clickedQuestSO)
    {
        if (clickedQuestSO == null) return;

        isViewingFromQuestLog = true;
        HandleQuestClick(clickedQuestSO);

        bool isComplete = IsQuestComplete(clickedQuestSO);

        SetCanvasState(acceptCanvas, false);
        SetCanvasState(declineCanvas, !isComplete);
        SetCanvasState(completeCanvas, isComplete);
    }

    private bool IsQuestComplete(QuestSO quest)
    {
        if (quest == null || questManager == null) return false;

        foreach (var objective in quest.questObjectives)
        {
            if (questManager.GetCurrentAmount(quest, objective) < objective.requiredAmount)
                return false;
        }
        // ĐÃ FIX LỖI 2: Thêm dòng return true
        return true;
    }

    public void HandleQuestClick(QuestSO questSO)
    {
        if (questSO == null) return;

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

        if (questNameText != null) questNameText.text = "NO QUEST SELECTED";
        if (questDescriptionText != null) questDescriptionText.text = "";

        foreach (var slot in objectiveSlots) { if (slot != null) slot.gameObject.SetActive(false); }
        foreach (var slot in rewardSlots) { if (slot != null) slot.gameObject.SetActive(false); }
    }

    private void DisplayObjectives()
    {
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
            // ĐÃ FIX LỖI 3: Dọn dẹp lại dấu ngoặc cho chuẩn
            else
            {
                if (objectiveSlots[i] != null)
                    objectiveSlots[i].gameObject.SetActive(false);
            }
        }
    }

    private void DisplayRewards()
    {
        if (questSO == null || rewardSlots == null) return;

        for (int i = 0; i < rewardSlots.Length; i++)
        {
            if (i < questSO.rewards.Count)
            {
                var reward = questSO.rewards[i];
                if (rewardSlots[i] == null) continue;

                rewardSlots[i].gameObject.SetActive(true);

                ItemDefinition itemDef = ItemDatabase.GetItem(reward.itemID);
                Sprite iconToDisplay = (itemDef != null) ? itemDef.GetIcon() : null;

                rewardSlots[i].DisplayReward(iconToDisplay, reward.quantity);
            }
            else
            {
                if (rewardSlots[i] != null)
                    rewardSlots[i].gameObject.SetActive(false);
            }
        }
    }
}