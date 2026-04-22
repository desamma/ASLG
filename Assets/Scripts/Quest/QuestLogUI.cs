using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class QuestLogUI : MonoBehaviour
{
    [SerializeField] private QuestManager questManager;

    [Header("UI Của Màn Hình Chi Tiết (Bên Phải)")]
    //Fix icon vàng exp không hiển thị
    [Header("Icons cho Gold & Exp (Kéo thả ảnh từ thư mục vào đây)")]
    [SerializeField] private Sprite goldIcon;
    [SerializeField] private Sprite expIcon;

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
        
        // CHỈ Refresh UI bên trái nếu đang mở từ Quest Log, 
        // không Refresh nếu đang mở từ NPC (để tránh mất các nút Quest khác)
        if (isViewingFromQuestLog) 
        {
            RefreshQuestList();
        }
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
        if (questManager == null || questSO == null) return;

        // CHẶN: Chưa xong không cho trả quest
        if (!IsQuestComplete(questSO)) return;

        questManager.CompleteQuest(questSO);
        SetCanvasState(completeCanvas, false);

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

        // Ẩn toàn bộ nút
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

        // Biến đếm xem đang dùng đến ô Slot thứ mấy rồi
        int currentSlotIndex = 0;

        // 1. KIỂM TRA VÀ HIỂN THỊ GOLD
        if (questSO.rewardGold > 0 && currentSlotIndex < rewardSlots.Length)
        {
            rewardSlots[currentSlotIndex].gameObject.SetActive(true);
            // Hiển thị icon vàng và số lượng vàng
            rewardSlots[currentSlotIndex].DisplayReward(goldIcon, questSO.rewardGold);
            currentSlotIndex++; // Tăng biến đếm slot lên 1
        }

        // 2. KIỂM TRA VÀ HIỂN THỊ EXP
        if (questSO.rewardExp > 0 && currentSlotIndex < rewardSlots.Length)
        {
            rewardSlots[currentSlotIndex].gameObject.SetActive(true);
            // Hiển thị icon exp và số lượng exp
            rewardSlots[currentSlotIndex].DisplayReward(expIcon, questSO.rewardExp);
            currentSlotIndex++;
        }

        // 3. KIỂM TRA VÀ HIỂN THỊ CÁC VẬT PHẨM (ITEMS)
        for (int i = 0; i < questSO.rewards.Count; i++)
        {
            // Nếu đã hết số ô hiển thị (slots) trên UI thì ngắt vòng lặp
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

        // 4. ẨN CÁC Ô CÒN THỪA (Những ô không có phần thưởng nào chiếm chỗ)
        for (int i = currentSlotIndex; i < rewardSlots.Length; i++)
        {
            if (rewardSlots[i] != null)
                rewardSlots[i].gameObject.SetActive(false);
        }
    }
}