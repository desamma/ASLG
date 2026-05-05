using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ItemStack
{
    public string itemID;
    public int amount;
    public ItemStack(string id, int amt) { itemID = id; amount = amt; }
}

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager instance { get; private set; }

    [Header("Túi đồ")]
    public List<ItemStack> bagItems = new List<ItemStack>();

    [Header("Quà Tân Thủ")]
    public List<ItemStack> starterItems = new List<ItemStack>();

    [Header("Trang bị")]
    public string equippedWeapon = "";
    public Dictionary<string, string> equippedArmor = new Dictionary<string, string>();
    public List<string> equippedAccessories = new List<string>();
    private const int MAX_ACCESSORIES = 2;

    public event Action OnInventoryChanged;

    private void Awake()
    {
        if (instance == null) { instance = this; DontDestroyOnLoad(gameObject); }
        else Destroy(gameObject);
    }

    private void Start() => ForceUIUpdate();

    private void Update()
    {
        //if (Input.GetKeyDown(KeyCode.Alpha1)) { AddItem("con_blood_potion", 5); AddItem("wp_iron_sword", 1); }
    }

    public void ForceUIUpdate() => OnInventoryChanged?.Invoke();

    public void ClearAndLoadStarterItems()
    {
        bagItems.Clear(); equippedWeapon = ""; equippedArmor.Clear(); equippedAccessories.Clear();
        foreach (var item in starterItems) AddItem(item.itemID, item.amount);
        Debug.Log("<color=green>[Inventory]</color> Đã làm sạch túi đồ và phát Quà Tân Thủ!");
    }


    public void AddItem(string itemID, int amount)
    {
        ItemDefinition def = ItemDatabase.GetItem(itemID);
        if (def == null) return;

        if (def.isStackable)
        {
            ItemStack existing = bagItems.Find(x => x.itemID == itemID && x.amount < def.maxStack);
            if (existing != null)
            {
                int space = def.maxStack - existing.amount;
                if (amount <= space) existing.amount += amount;
                else { existing.amount = def.maxStack; bagItems.Add(new ItemStack(itemID, amount - space)); }
            }
            else bagItems.Add(new ItemStack(itemID, amount));
        }
        else { for (int i = 0; i < amount; i++) bagItems.Add(new ItemStack(itemID, 1)); }

        OnInventoryChanged?.Invoke();
    }

    public void RemoveBagItem(ItemStack stack)
    {
        if (bagItems.Contains(stack)) { bagItems.Remove(stack); OnInventoryChanged?.Invoke(); }
    }

    public int GetItemCount(string itemID)
    {
        if (string.IsNullOrEmpty(itemID)) return 0;
        int total = 0;
        foreach (var s in bagItems) if (s.itemID == itemID) total += s.amount;
        return total;
    }

    public bool TryConsume(string itemID, int amount)
    {
        if (GetItemCount(itemID) < amount) return false;
        int remaining = amount;
        for (int i = bagItems.Count - 1; i >= 0 && remaining > 0; i--)
        {
            var s = bagItems[i];
            if (s.itemID != itemID) continue;
            int take = Mathf.Min(s.amount, remaining);
            s.amount -= take; remaining -= take;
            if (s.amount <= 0) bagItems.RemoveAt(i);
        }
        OnInventoryChanged?.Invoke();
        return true;
    }


    public bool IsEquipped(string itemID)
    {
        if (equippedWeapon == itemID) return true;
        if (equippedArmor.ContainsValue(itemID)) return true;
        if (equippedAccessories.Contains(itemID)) return true;
        return false;
    }

    public void ToggleEquip(string itemID)
    {
        ItemDefinition def = ItemDatabase.GetItem(itemID);
        if (def == null) return;
        if (IsEquipped(itemID))
        {
            if (def.itemType == "Weapon") equippedWeapon = "";
            else if (def.itemType == "Armor") equippedArmor.Remove(def.armorSlot);
            else if (def.itemType == "Accessory") equippedAccessories.Remove(itemID);
            if (StatsManager.instance != null) StatsManager.instance.RemoveItemBonus(itemID);
        }
        else
        {
            if (def.itemType == "Weapon") equippedWeapon = itemID;
            else if (def.itemType == "Armor") equippedArmor[def.armorSlot] = itemID;
            else if (def.itemType == "Accessory")
            {
                if (equippedAccessories.Count >= MAX_ACCESSORIES)
                {
                    string old = equippedAccessories[0];
                    if (StatsManager.instance != null) StatsManager.instance.RemoveItemBonus(old);
                    equippedAccessories.RemoveAt(0);
                }
                equippedAccessories.Add(itemID);
            }
            if (StatsManager.instance != null) StatsManager.instance.ApplyItemBonus(itemID);
        }
        OnInventoryChanged?.Invoke();
    }


    public void UseConsumable(ItemStack stack)
    {
        ItemDefinition def = ItemDatabase.GetItem(stack.itemID);
        if (def == null || def.itemType != "Consumable") return;
        if (StatsManager.instance != null)
            foreach (var bonus in def.statBonuses) StatsManager.instance.ApplyStatBonus(bonus.Key, bonus.Value);
        stack.amount--;
        if (stack.amount <= 0) bagItems.Remove(stack);
        OnInventoryChanged?.Invoke();
    }
}