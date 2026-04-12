using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "QuestSO", menuName = "QuestSO")]
public class QuestSO : ScriptableObject
{
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
    public ItemData itemData;
    public int quantity;
}