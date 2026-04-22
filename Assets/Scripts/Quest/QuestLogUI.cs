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

        HandleQuestClick(incomingQuestSO);
        SetCanvasState(questCanvas, true);
        SetCanvasState(acceptCanvas, true);
        SetCanvasState(declineCanvas, true);
        SetCanvasState(completeCanvas, false);
    }

    public void ShowQuestTurnIn(QuestSO incomingQuestSO)
    {
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
        questNameText.text = questSO.questName;
        questDescriptionText.text = questSO.questDescription;

        DisplayObjectives();
        DisplayRewards();
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