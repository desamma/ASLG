using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// InventoryUI - Quản lý giao diện inventory hoàn chỉnh.
/// Tính năng: Paging, Preview, Equip/Unequip, Drop, Stats sync.
/// Mở bằng: Bấm E (InventoryToggle.cs)
/// </summary>
public class InventoryUI : MonoBehaviour
{
    public static InventoryUI instance { get; private set; }

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
    [SerializeField] private TextMeshProUGUI txt_previewDesc;
    [SerializeField] private TextMeshProUGUI txt_previewStats;
    [SerializeField] private Button btn_equip;
    [SerializeField] private Button btn_drop;

    // ── Runtime ──────────────────────────────────────────────────────────
    private bool isOpen;
    private int currentPage;
    private ItemData selectedItem;
    private List<ItemData> equippedItems = new List<ItemData>();
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
        // ── BUTTON LISTENERS ──────────────────────────────────────────────
        btn_nextPage?.onClick.AddListener(NextPage);
        btn_prevPage?.onClick.AddListener(PrevPage);
        btn_equip?.onClick.AddListener(EquipSelected);
        btn_drop?.onClick.AddListener(DropSelected);

        // ── SET BUTTON TEXT ──────────────────────────────────────────────
        SetupButtonTexts();

        // ── STATS MANAGER ────────────────────────────────────────────────
        if (StatsManager.instance != null)
            StatsManager.instance.OnStatsChangedEvent += RefreshStats;

        SpawnSlots();
        inventoryPanel.SetActive(false);
    }

    // ── Setup Button Text ─────────────────────────────────────────────────
    private void SetupButtonTexts()
    {
        SetButtonText(btn_equip, "EQUIP");
        SetButtonText(btn_drop, "DROP");
    }

    private void SetButtonText(Button button, string text)
    {
        if (button == null) return;
        var btnText = button.GetComponentInChildren<TextMeshProUGUI>();
        if (btnText) btnText.text = text;
    }

    // ── Mở / Đóng ────────────────────────────────────────────────────────
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

    private void NextPage()
    {
        currentPage++;
        RenderPage();
        ShowEmptyPreview();
    }

    private void PrevPage()
    {
        currentPage--;
        RenderPage();
        ShowEmptyPreview();
    }

    // ── Preview ───────────────────────────────────────────────────────────
    private void ShowPreview(ItemData item)
    {
        if (item == null)
        {
            Debug.LogWarning("[Inventory] ShowPreview called with null item.");
            ShowEmptyPreview();
            return;
        }

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

        // Rarity + Color
        if (txt_previewRarity)
        {
            txt_previewRarity.SetText(item.rarity.ToString().ToUpper());
            txt_previewRarity.color = item.rarity switch
            {
                ItemRarity.Common => new Color(0.65f, 0.65f, 0.65f),
                ItemRarity.Uncommon => new Color(0.30f, 0.80f, 0.30f),
                ItemRarity.Rare => new Color(0.30f, 0.50f, 1.00f),
                ItemRarity.Epic => new Color(0.65f, 0.30f, 0.90f),
                ItemRarity.Legendary => new Color(1.00f, 0.60f, 0.10f),
                _ => Color.white
            };
        }

        // Description
        txt_previewDesc?.SetText(item.description);

        // Stat bonuses (NEW: sử dụng List<StatBonus>)
        var sb = new System.Text.StringBuilder();
        foreach (var bonus in item.statBonuses)
        {
            if (!string.IsNullOrEmpty(bonus.statName))
                sb.AppendLine($"<color=#6a5a42>{bonus.statName}</color>   <color=#90c060>+{bonus.value}</color>");
        }
        txt_previewStats?.SetText(sb.ToString());

        // Update equip button
        bool isEquipped = equippedItems.Contains(item);
        if (btn_equip)
        {
            btn_equip.gameObject.SetActive(item.statBonuses.Count > 0);
            var btnText = btn_equip.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText)
                btnText.text = isEquipped ? "UNEQUIP" : "EQUIP";
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

    // ── Equip / Unequip ───────────────────────────────────────────────────
    private void EquipSelected()
    {
        if (selectedItem == null) return;

        if (equippedItems.Contains(selectedItem))
        {
            // UNEQUIP
            equippedItems.Remove(selectedItem);
            Debug.Log($"[Inventory] 📦 UNEQUIPPED: {selectedItem.itemName}");
            StatsManager.instance?.RemoveItemBonus(selectedItem);
        }
        else
        {
            // EQUIP
            equippedItems.Add(selectedItem);
            Debug.Log($"[Inventory] ⚔️ EQUIPPED: {selectedItem.itemName}");
            StatsManager.instance?.ApplyItemBonus(selectedItem);
        }

        ShowPreview(selectedItem);
        RefreshStats();
    }

    // ── Drop ──────────────────────────────────────────────────────────────
    public void DropSelected()
    {
        if (selectedItem == null) return;

        // Unequip nếu đang equip
        if (equippedItems.Contains(selectedItem))
        {
            equippedItems.Remove(selectedItem);
            StatsManager.instance?.RemoveItemBonus(selectedItem);
        }

        Debug.Log($"[Inventory] 🗑️ DROPPED: {selectedItem.itemName}");
        playerItems.Remove(selectedItem);
        ShowEmptyPreview();
        RenderPage();
        RefreshStats();
    }

    // ── Public API ────────────────────────────────────────────────────────
    /// <summary>
    /// Thêm item vào inventory (pickup).
    /// Hỗ trợ stack nếu isStackable = true.
    /// </summary>
    public void AddItem(ItemData item)
    {
        if (item == null)
        {
            Debug.LogWarning("[Inventory] Tried to add null item.");
            return;
        }

        // Stack nếu được
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

    /// <summary>
    /// Lấy danh sách item đang equip.
    /// </summary>
    public List<ItemData> GetEquippedItems()
    {
        return new List<ItemData>(equippedItems);
    }

    /// <summary>
    /// Kiểm tra item có đang equip không.
    /// </summary>
    public bool IsItemEquipped(ItemData item)
    {
        return equippedItems.Contains(item);
    }

    private void OnDestroy()
    {
        if (StatsManager.instance != null)
            StatsManager.instance.OnStatsChangedEvent -= RefreshStats;
    }
}