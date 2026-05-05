using UnityEngine;
using UnityEngine.UI;

public class PersistentUIMenuButtons : MonoBehaviour
{
    private void Awake()
    {
        if (GetComponent<Canvas>() != null && GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();
    }

    public void ToggleInventory()
    {
        FindFirstObjectByType<InventoryUI>(FindObjectsInactive.Include).Toggle();
    }

    public void ToggleCrafting()
    {
        FindFirstObjectByType<CraftingUI>(FindObjectsInactive.Include).Toggle();
    }

    public void ToggleStats()
    {
        FindFirstObjectByType<StatsUI>(FindObjectsInactive.Include).Toggle();
    }

    public void ToggleQuestLog()
    {
        FindFirstObjectByType<QuestLogUI>(FindObjectsInactive.Include).Toggle();
    }

    public void ToggleMap()
    {
        FindFirstObjectByType<WorldMapManager>(FindObjectsInactive.Include)?.ToggleMap();
    }

    public void ToggleCompanionShop()
    {
        if (CompanionShopManager.Instance != null)
        {
            CompanionShopManager.Instance.ToggleShop();
        }
    }
}
