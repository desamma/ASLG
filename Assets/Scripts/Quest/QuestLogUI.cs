using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class QuestLogUI : MonoBehaviour
{
    [SerializeField] private QuestManager questManager;

    [Header("UI Của Màn Hình Chi Tiết (Bên Phải)")]
    [Header("Icons cho Gold & Exp")]
    [SerializeField] private Sprite goldIcon;
    [SerializeField] private Sprite expIcon;

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
            SetCanvasState(questCanvas, true);
            
            // Khi mở bảng lên, tự động click vào nhiệm vụ đầu tiên còn tồn tại để hiển thị
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
    }

    public void OnQuestSlotClicked(QuestSO clickedQuestSO)
    {
        if (clickedQuestSO == null) return;
        
        HandleQuestClick(clickedQuestSO); 

        // Kiểm tra xem Quest này đã được bấm Accept trước đó chưa
        bool isAccepted = questManager.activeQuests.Contains(clickedQuestSO);
        bool isComplete = IsQuestComplete(clickedQuestSO);

        if (!isAccepted)
        {
            // CHƯA NHẬN: Hiện Accept để người chơi chọn, hiện Decline để từ chối không làm
            SetCanvasState(acceptCanvas, true);
            SetCanvasState(declineCanvas, true);
            SetCanvasState(completeCanvas, false);
        }
        else 
        {
            // ĐÃ NHẬN: Ẩn Accept, hiện Decline để hủy, hiện Complete nếu đã đánh xong quái
            SetCanvasState(acceptCanvas, false);
            SetCanvasState(declineCanvas, true); 
            SetCanvasState(completeCanvas, isComplete);
        }
    }

    public void OnAcceptQuestClick()
    {
        if (questManager == null || questSO == null) return;

        // Bấm Accept -> Ghi vào Manager
        questManager.AcceptQuest(questSO);
        
        // Ẩn nút Accept đi, kiểm tra xem xong chưa để mở nút Complete
        SetCanvasState(acceptCanvas, false);
        SetCanvasState(completeCanvas, IsQuestComplete(questSO));
    }

    public void OnDeclineQuestClick()
    {
        if (questManager == null || questSO == null) return;

        // Nếu đã lỡ Accept rồi thì Hủy (Abandon) khỏi Manager
        if (questManager.activeQuests.Contains(questSO))
        {
            questManager.AbandonQuest(questSO);
        }

        // Dù đã nhận hay chưa nhận, bấm Decline là xóa luôn khỏi Bảng
        RemoveQuestFromUI(questSO);
        ClearRightPanel();

        // Mở thông tin của Quest tiếp theo (nếu còn)
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
        if (questManager == null || questSO == null) return;

        if (!IsQuestComplete(questSO)) return;

        questManager.CompleteQuest(questSO);
        
        // Xong rồi thì xóa khỏi bảng luôn
        RemoveQuestFromUI(questSO);
        ClearRightPanel();

        // Mở thông tin của Quest tiếp theo (nếu còn)
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
        if (targetQuestSO == null) return;

        this.questSO = targetQuestSO;
        questNameText.text = questSO.questName;
        questDescriptionText.text = questSO.questDescription;

        DisplayObjectives();
        DisplayRewards();
    }

    private void RefreshCurrentQuestDisplay()
    {
        if (questCanvas != null && questCanvas.alpha > 0 && questSO != null)
        {
            DisplayObjectives();
            
            if (completeCanvas != null && IsQuestComplete(questSO))
            {
                SetCanvasState(acceptCanvas, false);
                SetCanvasState(declineCanvas, true); 
                SetCanvasState(completeCanvas, true);
            }
        }
    }

    private void ClearRightPanel()
    {
        questSO = null;

        if (questNameText != null) questNameText.text = "NO QUEST SELECTED";
        if (questDescriptionText != null) questDescriptionText.text = "";

        foreach (var slot in objectiveSlots) { if (slot != null) slot.gameObject.SetActive(false); }
        foreach (var slot in rewardSlots) { if (slot != null) slot.gameObject.SetActive(false); }

        SetCanvasState(acceptCanvas, false);
        SetCanvasState(declineCanvas, false);
        SetCanvasState(completeCanvas, false);
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
            if (currentSlotIndex >= rewardSlots.Length) break; 

            var reward = questSO.rewards[i];
            rewardSlots[currentSlotIndex].gameObject.SetActive(true);

            Sprite iconToDisplay = null;

            if (reward.isRandomItem || string.IsNullOrEmpty(reward.itemID))
            {
                iconToDisplay = Resources.Load<Sprite>("Icons/missing_icon"); 
            }
            else
            {
                ItemDefinition itemDef = ItemDatabase.GetItem(reward.itemID);
                iconToDisplay = (itemDef != null) ? itemDef.GetIcon() : null;
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
        if (canvasGroup == null) return;
        canvasGroup.alpha = state ? 1 : 0;
        canvasGroup.blocksRaycasts = state;
        canvasGroup.interactable = state;
    }

    private void RemoveQuestFromUI(QuestSO quest)
    {
        if (quest == null || questSlots == null) return;
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