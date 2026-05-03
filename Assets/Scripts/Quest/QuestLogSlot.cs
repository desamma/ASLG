using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class QuestLogSlot : MonoBehaviour
{
    [SerializeField] private TMP_Text questNameText;
    [SerializeField] private TMP_Text questLevelText;

    public QuestSO currentQuest;

    public void Initialize(QuestSO questSO)
    {
        currentQuest = questSO;

        if (questNameText != null)
            questNameText.text = currentQuest.questName;

        if (questLevelText != null)
            questLevelText.text = "Lv." + currentQuest.questLevel.ToString();

        gameObject.SetActive(true);
    }

    private void OnValidate()
    {
        if (currentQuest != null)
            Initialize(currentQuest);
        else
            gameObject.SetActive(false);
    }

    public void ClearSlot()
    {
        currentQuest = null;
        gameObject.SetActive(false);
    }
}