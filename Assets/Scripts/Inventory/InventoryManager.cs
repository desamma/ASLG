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

    [Header("Túi đồ (Lưu lúc đang chơi)")]
    public List<ItemStack> bagItems = new List<ItemStack>();

    [Header("Quà Tân Thủ (Nhận khi bấm New Game)")]
    public List<ItemStack> starterItems = new List<ItemStack>();

    [Header("Trang bị hiện tại")]
    public string equippedWeapon = ""; 
    public Dictionary<string, string> equippedArmor = new Dictionary<string, string>(); 
    public List<string> equippedAccessories = new List<string>(); 
    private const int MAX_ACCESSORIES = 2;

    public event Action OnInventoryChanged;

    private void Awake()
    {
        if (instance == null) 
        { 
            instance = this; 
            DontDestroyOnLoad(gameObject); 
        }
        else Destroy(gameObject);
    }

    private void Start()
    {
        ForceUIUpdate();
    }

    private void Update()
    {
        // Phím Test cho Developer
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            AddItem("con_blood_potion", 5);
            AddItem("wp_iron_sword", 1);
        }
    }

    public void ForceUIUpdate()
    {
        OnInventoryChanged?.Invoke();
    }

    // GỌI KHI BẤM "NEW GAME" ĐỂ TẨY TRẮNG VÀ PHÁT QUÀ
    public void ClearAndLoadStarterItems()
    {
        bagItems.Clear();
        equippedWeapon = "";
        equippedArmor.Clear();
        equippedAccessories.Clear();

        // Phát quà dựa theo cấu hình Inspector
        foreach (var item in starterItems)
        {
            AddItem(item.itemID, item.amount);
        }

        Debug.Log("<color=green>[Inventory]</color> Đã làm sạch túi đồ và phát Quà Tân Thủ!");
    }

    // --- LOGIC THÊM/XÓA ĐỒ ---
    public void AddItem(string itemID, int amount)
    {
        ItemDefinition def = ItemDatabase.GetItem(itemID);
        if (def == null) return;

        if (def.isStackable)
        {
            ItemStack existing = bagItems.Find(x => x.itemID == itemID && x.amount < def.maxStack);
            if (existing != null)
            {
                int spaceLeft = def.maxStack - existing.amount;
                if (amount <= spaceLeft) existing.amount += amount;
                else
                {
                    existing.amount = def.maxStack;
                    bagItems.Add(new ItemStack(itemID, amount - spaceLeft));
                }
            }
            else bagItems.Add(new ItemStack(itemID, amount));
        }
        else
        {
            for (int i = 0; i < amount; i++) bagItems.Add(new ItemStack(itemID, 1));
        }
        
        OnInventoryChanged?.Invoke();
    }

    public void RemoveBagItem(ItemStack stackToRemove)
    {
        if (bagItems.Contains(stackToRemove))
        {
            bagItems.Remove(stackToRemove);
            OnInventoryChanged?.Invoke();
        }
    }

    // --- LOGIC TRANG BỊ ---
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
                    string oldAcc = equippedAccessories[0];
                    if (StatsManager.instance != null) StatsManager.instance.RemoveItemBonus(oldAcc);
                    equippedAccessories.RemoveAt(0);
                }
                equippedAccessories.Add(itemID);
            }
            
            if (StatsManager.instance != null) StatsManager.instance.ApplyItemBonus(itemID);
        }
        OnInventoryChanged?.Invoke();
    }

    // --- LOGIC DÙNG ĐỒ ---
    public void UseConsumable(ItemStack stack)
    {
        ItemDefinition def = ItemDatabase.GetItem(stack.itemID);
        if (def == null || def.itemType != "Consumable") return;

        if (StatsManager.instance != null)
        {
            foreach (var bonus in def.statBonuses)
            {
                StatsManager.instance.ApplyStatBonus(bonus.Key, bonus.Value);
            }
        }

        stack.amount--;
        if (stack.amount <= 0) bagItems.Remove(stack);
        
        OnInventoryChanged?.Invoke();
    }
}