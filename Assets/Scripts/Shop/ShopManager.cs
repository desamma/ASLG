using System;
using System.Collections.Generic;
using UnityEngine;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    [SerializeField] private ShopKeeper _currentShopKeeper;
    public ShopKeeper CurrentShopKeeper { get => _currentShopKeeper; set { _currentShopKeeper = value; } }
    [SerializeField] private ShopSlot[] shopSlots;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void PopulateShopItems(List<ShopItem> shopItems)
    {
        for (int i = 0; i < shopItems.Count && i < shopSlots.Length; i++)
        {
            ShopItem shopItem = shopItems[i];
            shopSlots[i].Initialize(shopItem.itemKeyName, shopItem.price);
            shopSlots[i].gameObject.SetActive(true);
        }

        for (int i = shopItems.Count; i < shopSlots.Length; i++)
        {
            shopSlots[i].gameObject.SetActive(false);
        }
    }

    public void ClearShop()
    {
        foreach (var slot in shopSlots)
            slot.gameObject.SetActive(false);
    }

    public void TryBuyItem(string itemKeyName, int price)
    {
        if (itemKeyName != null && StatsManager.instance.gold >= price)
        {
            StatsManager.instance.AddGold(-price);
            InventoryManager.instance.AddItem(itemKeyName, 1);
        }
    }
}

[System.Serializable]
public class ShopItem
{
    public string itemKeyName;
    [Min(1)] public int price;
}
