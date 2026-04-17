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

    // Biến lưu trữ Nhiệm vụ MÀ NGƯỜI CHƠI ĐANG CLICK CHUỘT VÀO XEM
    private QuestSO questSO;

    [Header("Danh Sách Các Nút Quest Đang Làm (Bên Trái)")]
    [SerializeField] private QuestLogSlot[] questSlots;

    [Header("Các Cụm UI Canvas (Bật/Tắt)")]
    [SerializeField] private CanvasGroup questCanvas;
    [SerializeField] private CanvasGroup acceptCanvas;
    [SerializeField] private CanvasGroup declineCanvas;
    [SerializeField] private CanvasGroup completeCanvas;

    // ==========================================
    // LẮNG NGHE SỰ KIỆN TỪ QUEST MANAGER
    // ==========================================
    private void Start()
    {
        if (questManager != null)
        {
            // Đăng ký nhận thông báo mỗi khi tiến độ tăng lên
            questManager.OnQuestProgressUpdated += RefreshCurrentQuestDisplay;
        }
    }

    private void OnDestroy()
    {
        if (questManager != null)
        {
            // Hủy đăng ký khi tắt object để tránh rò rỉ bộ nhớ
            questManager.OnQuestProgressUpdated -= RefreshCurrentQuestDisplay;
        }
    }

    // Hàm được gọi khi có thông báo: Bắt buộc vẽ lại màn hình chi tiết
    private void RefreshCurrentQuestDisplay()
    {
        // Chỉ vẽ lại nếu bảng UI ĐANG MỞ và đang có 1 quest được chọn xem
        if (questCanvas.alpha > 0 && questSO != null)
        {
            DisplayObjectives();
        }
    }
    // ==========================================

    private void Update()
    {
        // Bấm J để đóng/mở bảng Nhiệm vụ
        if (Input.GetKeyDown(KeyCode.J))
        {
            ToggleQuestUI();
        }
    }

    // Bật tắt bảng Nhiệm Vụ (dùng Alpha thay vì SetActive để giữ lại các component)
    public void ToggleQuestUI()
    {
        bool isShowing = questCanvas.alpha > 0;

        if (isShowing) // Nếu đang bật -> Tắt đi
        {
            questCanvas.alpha = 0;
            questCanvas.blocksRaycasts = false;
            questCanvas.interactable = false;
        }
        else // Nếu đang tắt -> Bật lên
        {
            questCanvas.alpha = 1;
            questCanvas.blocksRaycasts = true;
            questCanvas.interactable = true;

            // Load lại số liệu mới nhất khi mở bảng lên
            RefreshCurrentQuestDisplay();
        }
    }

    // Gọi khi lại gần bảng nhiệm vụ (NPC) để Nhận nhiệm vụ mới
    public void ShowQuestOffer(QuestSO incomingQuestSO)
    {
        // Check kỹ: Nếu đang làm hoặc đã làm xong rồi thì dẹp, không cho nhận nữa
        if (questManager.activeQuests.Contains(incomingQuestSO) || questManager.HasCompletedQuest(incomingQuestSO))
        {
            SetCanvasState(questCanvas, false);
            return;
        }

        HandleQuestClick(incomingQuestSO);

        // Bật nút Accept, Decline. Tắt nút Complete
        SetCanvasState(questCanvas, true);
        SetCanvasState(acceptCanvas, true);
        SetCanvasState(declineCanvas, true);
        SetCanvasState(completeCanvas, false);
    }

    // Gọi khi lại gần NPC để Trả nhiệm vụ
    public void ShowQuestTurnIn(QuestSO incomingQuestSO)
    {
        HandleQuestClick(incomingQuestSO);

        // Tắt nút Accept, Decline. CHỈ BẬT nút Complete
        SetCanvasState(questCanvas, true);
        SetCanvasState(acceptCanvas, false);
        SetCanvasState(declineCanvas, false);
        SetCanvasState(completeCanvas, true);
    }

    // Khi người chơi bấm nút "Accept"
    public void OnAcceptQuestClick()
    {
        questManager.AcceptQuest(questSO);

        SetCanvasState(acceptCanvas, false);
        SetCanvasState(declineCanvas, false);

        // Tải lại danh sách bên trái (thêm nhiệm vụ mới vào)
        RefreshQuestList();
    }

    // Khi người chơi bấm nút "Decline"
    public void OnDeclineQuestClick()
    {
        SetCanvasState(questCanvas, false); // Tắt cmn luôn bảng
    }

    // Khi người chơi bấm nút "Complete"
    public void OnCompleteQuestClick()
    {
        // Báo cho Manager dọn dẹp và phát thưởng
        questManager.CompleteQuest(questSO);

        // Tắt cái nút Complete đi kẻo bấm nhầm nhận thưởng 2 lần
        SetCanvasState(completeCanvas, false);

        // Tải lại danh sách bên trái (xóa nhiệm vụ này đi)
        RefreshQuestList();

        // XỬ LÝ MÀN HÌNH BÊN PHẢI (Tránh bị trống trơn sau khi hoàn thành)
        if (questManager.activeQuests.Count > 0)
        {
            // Cố gắng mở cái Quest kế tiếp đang nằm trong danh sách lên xem luôn
            HandleQuestClick(questManager.activeQuests[0]);
        }
        else
        {
            // Nếu sạch sành sanh không còn Quest nào đang làm nữa thì mới xóa trắng màn hình
            questNameText.text = "";
            questDescriptionText.text = "NO QUEST SELECTED";
            foreach (var slot in objectiveSlots) slot.gameObject.SetActive(false);
            foreach (var slot in rewardSlots) slot.gameObject.SetActive(false);
        }
    }

    // Hàm tiện ích để set tắt/mở nguyên 1 cụm UI
    private void SetCanvasState(CanvasGroup canvasGroup, bool state)
    {
        canvasGroup.alpha = state ? 1 : 0;
        canvasGroup.blocksRaycasts = state;
        canvasGroup.interactable = state;
    }

    // Quét lại toàn bộ danh sách activeQuests và vẽ ra các cái nút bấm bên trái
    public void RefreshQuestList()
    {
        List<QuestSO> activeQuests = questManager.GetActiveQuests();

        for (int i = 0; i < questSlots.Length; i++)
        {
            if (i < activeQuests.Count)
            {
                questSlots[i].SetQuests(activeQuests[i]); // Nếu có quest thì đổ dữ liệu vào nút
            }
            else
            {
                questSlots[i].ClearSlot(); // Nếu dư ô slot thì làm trống đi
            }
        }
    }

    // Khi người chơi BẤM CHUỘT vào một Quest trong danh sách bên trái
    public void HandleQuestClick(QuestSO questSO)
    {
        this.questSO = questSO; // Lưu lại quest đang xem

        // Đổ chữ ra màn hình
        questNameText.text = questSO.questName;
        questDescriptionText.text = questSO.questDescription;

        DisplayObjectives();
        DisplayRewards();
    }

    // Vẽ danh sách mục tiêu cần làm (đánh quái, chạy bộ)
    private void DisplayObjectives()
    {
        for (int i = 0; i < objectiveSlots.Length; i++)
        {
            if (i < questSO.questObjectives.Count)
            {
                var objective = questSO.questObjectives[i];

                // Lấy con số thực tế (VD: 2) và đoạn chữ (VD: "2/3")
                int curruntAmount = questManager.GetCurrentAmount(questSO, objective);
                string progress = questManager.GetProgressText(questSO, objective);

                // Đủ số là True, chưa đủ là False
                bool isCompleted = curruntAmount >= objective.requiredAmount;

                objectiveSlots[i].gameObject.SetActive(true);
                objectiveSlots[i].RefreshObjectives(objective.description, progress, isCompleted);
            }
            else
            {
                objectiveSlots[i].gameObject.SetActive(false); // Ẩn các ô thừa
            }
        }
    }

    // Vẽ danh sách phần thưởng 
    private void DisplayRewards()
    {
        for (int i = 0; i < rewardSlots.Length; i++)
        {
            if (i < questSO.rewards.Count)
            {
                var reward = questSO.rewards[i];
                rewardSlots[i].gameObject.SetActive(true);
                rewardSlots[i].DisplayReward(reward.itemData.icon, reward.quantity);
            }
            else
            {
                rewardSlots[i].gameObject.SetActive(false); // Ẩn các ô thừa
            }
        }
    }
}