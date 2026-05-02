using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MaterialRowUI : MonoBehaviour
{
    public Image itemIcon;
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI quantityText;
    public Image checkIcon;

    public Color haveEnoughColor = new Color(0.50f, 0.75f, 0.25f);
    public Color notEnoughColor = new Color(0.87f, 0.38f, 0.38f);

    public void Setup(ItemDefinition item, int required, int currentHave)
    {
        if (item == null) { Debug.LogWarning("[MaterialRowUI] ItemDefinition is null!"); return; }
        if (required <= 0) required = 1;
        if (currentHave < 0) currentHave = 0;

        bool enough = currentHave >= required;

        if (itemIcon) { var icon = item.GetIcon(); itemIcon.sprite = icon; itemIcon.enabled = icon != null; }
        if (itemNameText) itemNameText.text = item.name;
        if (quantityText) { quantityText.text = $"{currentHave}/{required}"; quantityText.color = enough ? haveEnoughColor : notEnoughColor; }
        if (checkIcon) checkIcon.enabled = enough;
    }
}