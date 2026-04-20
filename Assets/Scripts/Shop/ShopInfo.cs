using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ShopInfo : MonoBehaviour
{
    [Header("Canvas")]
    [SerializeField] private CanvasGroup infoPanel;
    [SerializeField] private RectTransform infoPanelRect;

    [Header("Text")]
    [SerializeField] private TextMeshProUGUI itemDescriptionText;

    [Header ("Stat Fields")]
    [SerializeField] private TextMeshProUGUI[] statTexts;

    private void Awake()
    {
        infoPanelRect = GetComponent<RectTransform>();
    }

    public void ShowItemInfo(ItemDefinition item)
    {
        infoPanel.alpha = 1f;
        itemDescriptionText.text = item.description;

        List<string> stats = new List<string>();

        foreach (var bonus in item.statBonuses)
        {
            switch (bonus.Key.ToLower())
            {
                case "currenthealth": case "hp": case "health": stats.Add($"Health\n {bonus.Value}"); break;
                case "currentmana": case "mp": case "mana": stats.Add($"Mana\n {bonus.Value}"); break;
                case "currentstamina": case "sp": case "stamina": stats.Add($"Stamina\n {bonus.Value}"); break;

                case "currentexp": case "exp": stats.Add($"Experience\n {Mathf.RoundToInt(bonus.Value)}"); break;
                case "upgradepoints": stats.Add($"Upgrade Point\n {Mathf.RoundToInt(bonus.Value)}"); break;
            }
        }

        if ( stats.Count <= 0 ) 
            return;

        for (int i = 0; i < statTexts.Length; i++)
        {
            if (i < stats.Count)
            {
                statTexts[i].text = stats[i];
                statTexts[i].gameObject.SetActive(true);
            }
            else
            {
                statTexts[i].gameObject.SetActive(false);
            }
        }
    }

    public void HideItemInfo()
    {
        infoPanel.alpha = 0f;
        itemDescriptionText.text = "";
    }

    public void FollowMouse()
    {
        Vector3 mousePosition = Input.mousePosition;
        Vector3 offset = new Vector3(10, -10, 0);

        infoPanelRect.position = mousePosition + offset;
    }
}
