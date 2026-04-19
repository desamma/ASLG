﻿using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// InventoryUI - Quản lý giao diện inventory hoàn chỉnh.
///
/// Equip rules:
///   Weapon    – tối đa 1
///   Armor     – mỗi ArmorSlot (Head/Body/Leg/Shoes) tối đa 1
///   Accessory – tối đa 2
///   Consumable– không equip, dùng ngay (trừ stack / xóa)
///   Misc      – không equip
/// </summary>
public class InventoryUI : MonoBehaviour
{
    public static InventoryUI instance { get; private set; }

    // ── Equip limits ──────────────────────────────────────────────────────
    private const int MAX_WEAPON = 1;
    private const int MAX_ACCESSORY = 2;

    [Header("Root")]
    [SerializeField] private GameObject inventoryPanel;

    // ── STATS PANEL ──────────────────────────────────────────────────────
    [Header("Stats Panel")]
    [SerializeField] private TextMeshProUGUI txt_level;
    [SerializeField] private TextMeshProUGUI txt_expBar;
    [SerializeField] private Slider slider_exp;
    [SerializeField] private TextMeshProUGUI txt_hp;
    [SerializeField] private Slider slider_hp;
    [SerializeField] private TextMeshProUGUI txt_mana;
    [SerializeField] private Slider slider_mana;
    [SerializeField] private TextMeshProUGUI txt_stamina;
    [SerializeField] private Slider slider_stamina;
    [SerializeField] private TextMeshProUGUI txt_damage;
    [SerializeField] private TextMeshProUGUI txt_moveSpeed;
    [SerializeField] private TextMeshProUGUI txt_range;
    [SerializeField] private TextMeshProUGUI txt_defence;
    [SerializeField] private TextMeshProUGUI txt_cooldown;
    [SerializeField] private TextMeshProUGUI txt_upgradePoints;

    // ── INVENTORY PANEL ──────────────────────────────────────────────────
    [Header("Inventory Panel")]
    [SerializeField] private GameObject itemSlotPrefab;
    [SerializeField] private Transform slotGrid;
    [SerializeField] private Button btn_nextPage;
    [SerializeField] private Button btn_prevPage;
    [SerializeField] private TextMeshProUGUI txt_pageIndicator;
    [SerializeField] private int slotsPerPage = 10;

    // ── PREVIEW PANEL ────────────────────────────────────────────────────
    [Header("Preview Panel")]
    [SerializeField] private GameObject emptyHint;
    [SerializeField] private GameObject previewContent;
    [SerializeField] private Image img_previewIcon;
    [SerializeField] private TextMeshProUGUI txt_previewName;
    [SerializeField] private TextMeshProUGUI txt_previewRarity;
    [SerializeField] private TextMeshProUGUI txt_previewType;      // ← NEW: hiển thị type/slot
    [SerializeField] private TextMeshProUGUI txt_previewDesc;
    [SerializeField] private TextMeshProUGUI txt_previewStats;
    [SerializeField] private Button btn_equip;                     // double duty: EQUIP / UNEQUIP / USE
    [SerializeField] private Button btn_drop;

    // ── Runtime ──────────────────────────────────────────────────────────
    private bool isOpen;
    private int currentPage;
    private ItemData selectedItem;

    // Equipped lists per type
    private List<ItemData> equippedWeapons = new List<ItemData>(); // max 1
    private Dictionary<ArmorSlot, ItemData> equippedArmor = new Dictionary<ArmorSlot, ItemData>();
    private List<ItemData> equippedAccessories = new List<ItemData>(); // max 2

    private List<GameObject> spawnedSlots = new List<GameObject>();
    public List<ItemData> playerItems = new List<ItemData>();

    // ─────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (instance == null) instance = this;
        else { Destroy(gameObject); return; }
    }

    private void Start()
    {
        btn_nextPage?.onClick.AddListener(NextPage);
        btn_prevPage?.onClick.AddListener(PrevPage);
        btn_equip?.onClick.AddListener(OnEquipButtonClicked);
        btn_drop?.onClick.AddListener(DropSelected);

        SetupButtonTexts();

        if (StatsManager.instance != null)
            StatsManager.instance.OnStatsChangedEvent += RefreshStats;

        SpawnSlots();
        inventoryPanel.SetActive(false);
    }

    // ── Setup ─────────────────────────────────────────────────────────────
    private void SetupButtonTexts()
    {
        SetButtonText(btn_drop, "DROP");
    }

    private void SetButtonText(Button button, string text)
    {
        if (button == null) return;
        var t = button.GetComponentInChildren<TextMeshProUGUI>();
        if (t) t.text = text;
    }

    // ── Toggle ────────────────────────────────────────────────────────────
    public void Toggle()
    {
        isOpen = !isOpen;
        inventoryPanel.SetActive(isOpen);
        if (isOpen) { currentPage = 0; RefreshAll(); }
        Debug.Log($"[Inventory] Toggle: isOpen={isOpen}");
    }

    // ── Refresh ───────────────────────────────────────────────────────────
    private void RefreshAll()
    {
        RefreshStats();
        RenderPage();
        ShowEmptyPreview();
    }

    private void RefreshStats()
    {
        var s = StatsManager.instance;
        if (s == null) return;

        txt_level?.SetText($"LV {s.level}");
        txt_expBar?.SetText($"{s.currentExp} / {s.expToNextLevel} EXP");
        if (slider_exp) slider_exp.value = (float)s.currentExp / s.expToNextLevel;

        txt_hp?.SetText($"{s.currentHealth:0} / {s.maxHealth:0}");
        if (slider_hp) slider_hp.value = s.currentHealth / s.maxHealth;

        txt_mana?.SetText($"{s.currentMana:0} / {s.maxMana:0}");
        if (slider_mana) slider_mana.value = s.currentMana / s.maxMana;

        txt_stamina?.SetText($"{s.currentStamina:0} / {s.maxStamina:0}");
        if (slider_stamina) slider_stamina.value = s.currentStamina / s.maxStamina;

        txt_damage?.SetText($"{s.damage}");
        txt_moveSpeed?.SetText($"{s.moveSpeed:F1}");
        txt_range?.SetText($"{s.weaponRange:F1}");
        txt_cooldown?.SetText($"{s.cooldown:F2}s");
        txt_defence?.SetText($"{s.defence:F1}");
        txt_upgradePoints?.SetText($"{s.upgradePoints} pts");
    }

    // ── Grid / Paging ─────────────────────────────────────────────────────
    private void SpawnSlots()
    {
        foreach (var s in spawnedSlots) Destroy(s);
        spawnedSlots.Clear();
        for (int i = 0; i < slotsPerPage; i++)
            spawnedSlots.Add(Instantiate(itemSlotPrefab, slotGrid));
    }

    private void RenderPage()
    {
        int total = Mathf.Max(1, Mathf.CeilToInt((float)playerItems.Count / slotsPerPage));
        currentPage = Mathf.Clamp(currentPage, 0, total - 1);
        txt_pageIndicator?.SetText($"{currentPage + 1} / {total}");
        btn_prevPage?.gameObject.SetActive(currentPage > 0);
        btn_nextPage?.gameObject.SetActive(currentPage < total - 1);

        int start = currentPage * slotsPerPage;
        for (int i = 0; i < slotsPerPage; i++)
        {
            if (i >= spawnedSlots.Count) break;
            var slot = spawnedSlots[i].GetComponent<ItemSlot>();
            int idx = start + i;

            if (idx < playerItems.Count)
            {
                var data = playerItems[idx];
                slot?.SetItem(data, () => ShowPreview(data));
            }
            else
            {
                slot?.SetEmpty();
            }
        }
    }

    private void NextPage() { currentPage++; RenderPage(); ShowEmptyPreview(); }
    private void PrevPage() { currentPage--; RenderPage(); ShowEmptyPreview(); }

    // ── Preview ───────────────────────────────────────────────────────────
    private void ShowPreview(ItemData item)
    {
        if (item == null) { ShowEmptyPreview(); return; }

        selectedItem = item;
        emptyHint?.SetActive(false);
        previewContent?.SetActive(true);

        // Icon
        if (img_previewIcon)
        {
            img_previewIcon.sprite = item.icon;
            img_previewIcon.enabled = item.icon != null;
        }

        // Name
        txt_previewName?.SetText(item.itemName);

        // Rarity
        if (txt_previewRarity)
        {
            txt_previewRarity.SetText(item.rarity.ToString().ToUpper());
            txt_previewRarity.color = RarityColor(item.rarity);
        }

        // Type label  (NEW)
        if (txt_previewType)
        {
            string typeLabel = item.itemType switch
            {
                ItemType.Weapon => "⚔️  WEAPON",
                ItemType.Armor => $"🛡️  ARMOR – {item.armorSlot.ToString().ToUpper()}",
                ItemType.Accessory => "💍  ACCESSORY",
                ItemType.Consumable => "🧪  CONSUMABLE",
                _ => "📦  MISC"
            };
            txt_previewType.SetText(typeLabel);
        }

        // Description
        txt_previewDesc?.SetText(item.description);

        // Stat list
        var sb = new System.Text.StringBuilder();
        foreach (var bonus in item.statBonuses)
        {
            if (!string.IsNullOrEmpty(bonus.statName))
                sb.AppendLine($"<color=#6a5a42>{bonus.statName}</color>   <color=#90c060>+{bonus.value}</color>");
        }
        txt_previewStats?.SetText(sb.ToString());

        // ── Equip / USE button ──────────────────────────────────────────
        if (btn_equip)
        {
            if (item.IsConsumable)
            {
                // Consumable → hiển thị nút USE
                btn_equip.gameObject.SetActive(true);
                SetButtonText(btn_equip, "USE");
            }
            else if (item.IsEquippable)
            {
                btn_equip.gameObject.SetActive(true);
                bool isEquipped = IsEquipped(item);
                SetButtonText(btn_equip, isEquipped ? "UNEQUIP" : "EQUIP");

                // Disable nút EQUIP nếu slot đã đầy và item chưa được equip
                btn_equip.interactable = isEquipped || CanEquip(item);
            }
            else
            {
                // Misc – không có nút equip/use
                btn_equip.gameObject.SetActive(false);
            }
        }

        btn_drop?.gameObject.SetActive(true);
    }

    private void ShowEmptyPreview()
    {
        selectedItem = null;
        emptyHint?.SetActive(true);
        previewContent?.SetActive(false);
        btn_equip?.gameObject.SetActive(false);
        btn_drop?.gameObject.SetActive(false);
    }

    // ── Equip Button Handler ──────────────────────────────────────────────
    private void OnEquipButtonClicked()
    {
        if (selectedItem == null) return;

        if (selectedItem.IsConsumable)
            UseConsumable(selectedItem);
        else if (selectedItem.IsEquippable)
            ToggleEquip(selectedItem);
    }

    // ── Equip Logic ───────────────────────────────────────────────────────

    /// <summary>Kiểm tra item có thể equip thêm không (chưa đầy slot).</summary>
    private bool CanEquip(ItemData item)
    {
        switch (item.itemType)
        {
            case ItemType.Weapon:
                return equippedWeapons.Count < MAX_WEAPON;

            case ItemType.Armor:
                return !equippedArmor.ContainsKey(item.armorSlot);

            case ItemType.Accessory:
                return equippedAccessories.Count < MAX_ACCESSORY;

            default:
                return false;
        }
    }

    /// <summary>Kiểm tra item đang được equip.</summary>
    private bool IsEquipped(ItemData item)
    {
        switch (item.itemType)
        {
            case ItemType.Weapon: return equippedWeapons.Contains(item);
            case ItemType.Armor: return equippedArmor.TryGetValue(item.armorSlot, out var a) && a == item;
            case ItemType.Accessory: return equippedAccessories.Contains(item);
            default: return false;
        }
    }

    /// <summary>EQUIP hoặc UNEQUIP item tùy trạng thái hiện tại.</summary>
    private void ToggleEquip(ItemData item)
    {
        if (IsEquipped(item))
        {
            UnequipItem(item);
        }
        else
        {
            // Nếu slot đầy → tự động unequip item cũ trước
            switch (item.itemType)
            {
                case ItemType.Weapon when equippedWeapons.Count >= MAX_WEAPON:
                    UnequipItem(equippedWeapons[0]);
                    break;

                case ItemType.Armor when equippedArmor.ContainsKey(item.armorSlot):
                    UnequipItem(equippedArmor[item.armorSlot]);
                    break;

                case ItemType.Accessory when equippedAccessories.Count >= MAX_ACCESSORY:
                    UnequipItem(equippedAccessories[0]); // unequip accessory cũ nhất
                    break;
            }

            EquipItem(item);
        }

        ShowPreview(item);
        RefreshStats();
    }

    private void EquipItem(ItemData item)
    {
        switch (item.itemType)
        {
            case ItemType.Weapon: equippedWeapons.Add(item); break;
            case ItemType.Armor: equippedArmor[item.armorSlot] = item; break;
            case ItemType.Accessory: equippedAccessories.Add(item); break;
        }
        StatsManager.instance?.ApplyItemBonus(item);
        Debug.Log($"[Inventory] ⚔️ EQUIPPED [{item.itemType}]: {item.itemName}");
    }

    private void UnequipItem(ItemData item)
    {
        switch (item.itemType)
        {
            case ItemType.Weapon: equippedWeapons.Remove(item); break;
            case ItemType.Armor: equippedArmor.Remove(item.armorSlot); break;
            case ItemType.Accessory: equippedAccessories.Remove(item); break;
        }
        StatsManager.instance?.RemoveItemBonus(item);
        Debug.Log($"[Inventory] 📦 UNEQUIPPED [{item.itemType}]: {item.itemName}");
    }

    // ── Consumable ────────────────────────────────────────────────────────
    private void UseConsumable(ItemData item)
    {
        var stats = StatsManager.instance;
        if (stats == null)
        {
            Debug.LogWarning("[Inventory] StatsManager not found – cannot use consumable.");
            return;
        }

        // Áp dụng effect (hồi HP, Mana, …)
        foreach (var bonus in item.statBonuses)
        {
            if (string.IsNullOrEmpty(bonus.statName)) continue;

            switch (bonus.statName.ToLower())
            {
                case "currenthealth":
                    stats.currentHealth = Mathf.Min(stats.currentHealth + bonus.value, stats.maxHealth);
                    break;
                case "currentmana":
                    stats.currentMana = Mathf.Min(stats.currentMana + bonus.value, stats.maxMana);
                    break;
                case "currentstamina":
                    stats.currentStamina = Mathf.Min(stats.currentStamina + bonus.value, stats.maxStamina);
                    break;
                default:
                    // Hỗ trợ heal theo tên stat khác nếu cần mở rộng
                    Debug.Log($"[Inventory] Consumable effect '{bonus.statName}' not handled directly.");
                    break;
            }
        }

        Debug.Log($"[Inventory] 🧪 USED: {item.itemName}");

        // Trừ stack hoặc xóa item
        if (item.isStackable && item.currentStack > 1)
        {
            item.currentStack--;
            RenderPage();
            ShowPreview(item); // refresh preview để cập nhật số lượng
        }
        else
        {
            playerItems.Remove(item);
            ShowEmptyPreview();
            RenderPage();
        }

        RefreshStats();
    }

    // ── Drop ──────────────────────────────────────────────────────────────
    public void DropSelected()
    {
        if (selectedItem == null) return;

        // Unequip nếu đang trang bị
        if (IsEquipped(selectedItem))
            UnequipItem(selectedItem);

        Debug.Log($"[Inventory] 🗑️ DROPPED: {selectedItem.itemName}");
        playerItems.Remove(selectedItem);
        ShowEmptyPreview();
        RenderPage();
        RefreshStats();
    }

    // ── Public API ────────────────────────────────────────────────────────
    public void AddItem(ItemData item)
    {
        if (item == null) { Debug.LogWarning("[Inventory] Tried to add null item."); return; }

        if (item.isStackable)
        {
            var existing = playerItems.Find(i => i.itemName == item.itemName && i.currentStack < item.maxStack);
            if (existing != null)
            {
                existing.currentStack++;
                if (isOpen) RenderPage();
                Debug.Log($"[Inventory] ➕ STACKED: {item.itemName} (x{existing.currentStack})");
                return;
            }
        }

        playerItems.Add(item);
        if (isOpen) RenderPage();
        Debug.Log($"[Inventory] ✅ ADDED: {item.itemName}");
    }

    // =========================================================================
    // THÊM: HÀM NHẬN NHIỀU ITEM CÙNG LÚC ĐỂ TỐI ƯU PHẦN THƯỞNG QUEST (VÀNG, EXP)
    // =========================================================================
    public void AddItems(ItemData item, int amount)
    {
        if (item == null || amount <= 0) return;

        if (item.isStackable)
        {
            var existing = playerItems.Find(i => i.itemName == item.itemName && i.currentStack < item.maxStack);
            if (existing != null)
            {
                existing.currentStack += amount;

                // Giới hạn max stack theo thông số của item
                if (existing.currentStack > item.maxStack)
                {
                    existing.currentStack = item.maxStack;
                }

                if (isOpen) RenderPage();
                Debug.Log($"[Inventory] ➕ STACKED: {item.itemName} (+{amount})");
                return;
            }
            else
            {
                // Thêm item stackable mới và thiết lập số lượng
                item.currentStack = amount > item.maxStack ? item.maxStack : amount;
                playerItems.Add(item);

                if (isOpen) RenderPage();
                Debug.Log($"[Inventory] ✅ ADDED: {item.itemName} (x{amount})");
                return;
            }
        }

        // Nếu là vũ khí/giáp (không stack) thì add nhiều lần vào danh sách
        for (int i = 0; i < amount; i++)
        {
            playerItems.Add(item);
        }

        if (isOpen) RenderPage();
        Debug.Log($"[Inventory] ✅ ADDED: {amount}x {item.itemName}");
    }
    // =========================================================================

    public List<ItemData> GetEquippedItems()
    {
        var all = new List<ItemData>(equippedWeapons);
        all.AddRange(equippedArmor.Values);
        all.AddRange(equippedAccessories);
        return all;
    }

    public bool IsItemEquipped(ItemData item) => IsEquipped(item);

    // ── Helpers ───────────────────────────────────────────────────────────
    private Color RarityColor(ItemRarity r) => r switch
    {
        ItemRarity.Common => new Color(0.65f, 0.65f, 0.65f),
        ItemRarity.Uncommon => new Color(0.30f, 0.80f, 0.30f),
        ItemRarity.Rare => new Color(0.30f, 0.50f, 1.00f),
        ItemRarity.Epic => new Color(0.65f, 0.30f, 0.90f),
        ItemRarity.Legendary => new Color(1.00f, 0.60f, 0.10f),
        _ => Color.white
    };

    private void OnDestroy()
    {
        if (StatsManager.instance != null)
            StatsManager.instance.OnStatsChangedEvent -= RefreshStats;
    }
}