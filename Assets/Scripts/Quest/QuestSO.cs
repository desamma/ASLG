using System.Collections.Generic;
using UnityEngine;

// Định nghĩa các loại nhiệm vụ
public enum QuestType
{
    MainQuest,
    DailyQuest
}

[CreateAssetMenu(fileName = "QuestSO", menuName = "QuestSO")]
public class QuestSO : ScriptableObject
{
    [Header("Backend Data")]
    [Tooltip("Mã ID duy nhất của Quest. (VD: quest_kill_slime_01)")]
    public string questID;

    [Header("Phân loại & Điều kiện mở khóa (Unlock Conditions)")]
    public QuestType questType;
    public int requiredLevel = 1;
    public string requiredMapUnlocked;
    public EnemyType requiredBossKilled;
    public QuestSO requiredPreviousQuest;

    [Header("Info")]
    public string questName;
    [TextArea] public string questDescription;
    public int questLevel;

    [Header("Rewards (Stats)")]
    public int rewardGold;
    public int rewardExp;

    public List<QuestObjective> questObjectives;
    public List<QuestReward> rewards;
}

[System.Serializable]
public class QuestObjective
{
    public string description;
    [SerializeField] private Object target;
    public EnemyType targetEnemyType;

    // Giữ lại từ bài học chuyển map trước đó để hệ thống của bạn không bị lỗi
    [Tooltip("Tên Map cần đến (Dành cho nhiệm vụ đi qua cổng chuyển cảnh)")]
    public string targetMapName;

    // THÊM MỚI CHO TRỤ ĐÁ
    [Tooltip("Đánh dấu nếu mục tiêu là tương tác với Trụ Dịch Chuyển")]
    public bool requiresTeleport;

    public bool requiresMovement;
    public int requiredAmount;
}

[System.Serializable]
public class QuestReward
{
    [Tooltip("Nhập ID của item từ file items.json (VD: con_blood_potion)")]
    public string itemID;
    public int quantity;

    [Tooltip("Đánh dấu nếu muốn phần thưởng này là một Item Ngẫu Nhiên từ Database")]
    public bool isRandomItem;
}