using System.Collections.Generic;
using UnityEngine;

// 1. Phân thành 2 nhánh cốt truyện và Daily task
public enum QuestType
{
    Story, // Nhiệm vụ chính tuyến (Cốt truyện)
    Daily  // Nhiệm vụ hằng ngày
}

[CreateAssetMenu(fileName = "QuestSO", menuName = "QuestSO")]
public class QuestSO : ScriptableObject
{
    [Header("Quest Identity (BẮT BUỘC ĐỂ SAVE/LOAD)")]
    [Tooltip("Mỗi quest phải có 1 ID duy nhất. VD: main_01, daily_slime")]
    public string questID;
    public QuestType questType;

    [Header("Quest Info")]
    public string questName;
    [TextArea] public string questDescription;
    public int questLevel;

    public List<QuestObjective> questObjectives;
    public List<QuestReward> rewards;
}

[System.Serializable]
public class QuestObjective
{
    public string description;
    [SerializeField] private Object target; 
    public EnemyType targetEnemyType;
    public bool requiresMovement;
    public int requiredAmount;
}

[System.Serializable]
public class QuestReward
{
    [Tooltip("Nhập ID của item từ file items.json (VD: con_blood_potion)")]
    public string itemID; 
    public int quantity;
}