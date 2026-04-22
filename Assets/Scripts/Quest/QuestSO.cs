using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "QuestSO", menuName = "QuestSO")]
public class QuestSO : ScriptableObject
{
    [Header("Backend Data")]
    [Tooltip("Mã ID duy nhất của Quest. (VD: quest_kill_slime_01)")]
    public string questID; 

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
    public bool requiresMovement;
    public int requiredAmount;
}

[System.Serializable]
public class QuestReward
{
    [Tooltip("Nhập ID của item từ file items.json (VD: con_blood_potion)")]
    public string itemID; 
    public int quantity;

    //Tick cho đồ random, ở trong Inspector ấy, a Q ưng thì đổi.
    [Tooltip("Đánh dấu nếu muốn phần thưởng này là một Item Ngẫu Nhiên từ Database")]
    public bool isRandomItem;
}