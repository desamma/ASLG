using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class QuestLogSlot : MonoBehaviour
{
    [SerializeField] private TMP_Text questNameText;
    [SerializeField] private TMP_Text questLevelText;

    public QuestSO currentQuest;

    public QuestLogUI questLogUI;
    private void OnValidate()
    {
        if (currentQuest != null)
            SetQuests(currentQuest);
        else
            gameObject.SetActive(false);
    }

    public void SetQuests(QuestSO questSO)
    {
        currentQuest = questSO;

        questNameText.text = currentQuest.questName;
        questLevelText.text = "Lv." + questSO.questLevel.ToString();

        gameObject.SetActive(true);
        
    }

    public void ClearSlot() 
    {
        currentQuest = null;
        gameObject.SetActive(false);
    }

    public void OnSlotClick()
    {
        questLogUI.HandleQuestClick(currentQuest);
    }
}