﻿using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class InventoryUI : MonoBehaviour
{
    public static InventoryUI instance { get; private set; }

    [Header("Inventory Panel")]
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private GameObject itemSlotPrefab;
    [SerializeField] private Transform slotGrid;
    [SerializeField] private Button btn_nextPage;
    [SerializeField] private Button btn_prevPage;
    [SerializeField] private TextMeshProUGUI txt_pageIndicator;
    [SerializeField] private int slotsPerPage = 10;

    [Header("Preview Panel")]
    [SerializeField] private GameObject emptyHint;
    [SerializeField] private GameObject previewContent;
    [SerializeField] private Image img_previewIcon;
    [SerializeField] private TextMeshProUGUI txt_previewName;
    [SerializeField] private TextMeshProUGUI txt_previewRarity;
    [SerializeField] private TextMeshProUGUI txt_previewType;
    [SerializeField] private TextMeshProUGUI txt_previewDesc;
    [SerializeField] private TextMeshProUGUI txt_previewStats;
    [SerializeField] private Button btn_equip;
    [SerializeField] private Button btn_drop;

    private bool isOpen;
    private int currentPage;
    private List<GameObject> spawnedSlots = new List<GameObject>();
    private ItemStack selectedStack;

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        btn_nextPage?.onClick.AddListener(NextPage);
        btn_prevPage?.onClick.AddListener(PrevPage);
        btn_equip?.onClick.AddListener(OnEquipUseClicked);
        btn_drop?.onClick.AddListener(DropSelected);

        SpawnSlots();
        inventoryPanel.SetActive(false);

        // Lắng nghe thay đổi từ túi đồ gốc
        if (InventoryManager.instance != null)
            InventoryManager.instance.OnInventoryChanged += RenderPage;
    }

    private void OnDestroy()
    {
        if (InventoryManager.instance != null)
            InventoryManager.instance.OnInventoryChanged -= RenderPage;
    }

    public void Toggle()
    {
        isOpen = !isOpen;
        inventoryPanel.SetActive(isOpen);
        if (isOpen) { currentPage = 0; RenderPage(); ShowEmptyPreview(); }
    }

    private void SpawnSlots()
    {
        foreach (var s in spawnedSlots) Destroy(s);
        spawnedSlots.Clear();
        for (int i = 0; i < slotsPerPage; i++) spawnedSlots.Add(Instantiate(itemSlotPrefab, slotGrid));
    }

    private void RenderPage()
    {
        if (!isOpen || InventoryManager.instance == null) return;
        
        List<ItemStack> bag = InventoryManager.instance.bagItems;

        int total = Mathf.Max(1, Mathf.CeilToInt((float)bag.Count / slotsPerPage));
        currentPage = Mathf.Clamp(currentPage, 0, total - 1);
        txt_pageIndicator?.SetText($"{currentPage + 1} / {total}");
        
        btn_prevPage?.gameObject.SetActive(currentPage > 0);
        btn_nextPage?.gameObject.SetActive(currentPage < total - 1);

        int start = currentPage * slotsPerPage;
        for (int i = 0; i < slotsPerPage; i++)
        {
            ItemSlot slot = spawnedSlots[i].GetComponent<ItemSlot>();
            int idx = start + i;
            
            if (idx < bag.Count)
            {
                ItemStack stack = bag[idx];
                slot.SetItem(stack, () => ShowPreview(stack));
            }
            else slot.SetEmpty();
        }
        
        // Refresh lại Preview nếu món đồ đang chọn bị dùng hoặc trang bị
        if (selectedStack != null) ShowPreview(selectedStack);
    }

    private void NextPage() { currentPage++; RenderPage(); ShowEmptyPreview(); }
    private void PrevPage() { currentPage--; RenderPage(); ShowEmptyPreview(); }

    private void ShowPreview(ItemStack stack)
    {
        if (stack == null || !InventoryManager.instance.bagItems.Contains(stack)) { ShowEmptyPreview(); return; }

        ItemDefinition def = ItemDatabase.GetItem(stack.itemID);
        if (def == null) return;

        selectedStack = stack;
        emptyHint?.SetActive(false);
        previewContent?.SetActive(true);

        if (img_previewIcon) 
        { 
            Sprite s = def.GetIcon();
            img_previewIcon.sprite = s; 
            img_previewIcon.enabled = (s != null); 
        }
        
        txt_previewName?.SetText(def.name);
        
        if (txt_previewRarity) { txt_previewRarity.SetText(def.rarity.ToUpper()); }
        
        if (txt_previewType)
        {
            string typeLabel = def.itemType switch
            {
                "Weapon" => "⚔️ WEAPON",
                "Armor" => $"🛡️ ARMOR – {def.armorSlot.ToUpper()}",
                "Accessory" => "💍 ACCESSORY",
                "Consumable" => "🧪 CONSUMABLE",
                _ => "📦 MISC"
            };
            txt_previewType.SetText(typeLabel);
        }

        txt_previewDesc?.SetText(def.description);

        var sb = new System.Text.StringBuilder();
        foreach (var bonus in def.statBonuses) sb.AppendLine($"<color=#6a5a42>{bonus.Key}</color>   <color=#90c060>+{bonus.Value}</color>");
        txt_previewStats?.SetText(sb.ToString());

        if (btn_equip)
        {
            btn_equip.gameObject.SetActive(true);
            var btnText = btn_equip.GetComponentInChildren<TextMeshProUGUI>();
            
            if (def.itemType == "Consumable") btnText.text = "USE";
            else if (def.itemType == "Weapon" || def.itemType == "Armor" || def.itemType == "Accessory")
            {
                bool isEquipped = InventoryManager.instance.IsEquipped(stack.itemID);
                btnText.text = isEquipped ? "UNEQUIP" : "EQUIP";
            }
            else btn_equip.gameObject.SetActive(false); // Item dạng Misc không có nút Equip
        }
        btn_drop?.gameObject.SetActive(true);
    }

    private void ShowEmptyPreview()
    {
        selectedStack = null;
        emptyHint?.SetActive(true);
        previewContent?.SetActive(false);
        btn_equip?.gameObject.SetActive(false);
        btn_drop?.gameObject.SetActive(false);
    }

    private void OnEquipUseClicked()
    {
        if (selectedStack == null) return;
        ItemDefinition def = ItemDatabase.GetItem(selectedStack.itemID);
        if (def == null) return;

        if (def.itemType == "Consumable") InventoryManager.instance.UseConsumable(selectedStack);
        else InventoryManager.instance.ToggleEquip(selectedStack.itemID);
    }

    public void DropSelected()
    {
        if (selectedStack == null) return;
        
        // Tháo đồ trước khi vứt (nếu đang mặc)
        if (InventoryManager.instance.IsEquipped(selectedStack.itemID))
            InventoryManager.instance.ToggleEquip(selectedStack.itemID); 

        InventoryManager.instance.RemoveBagItem(selectedStack);
        ShowEmptyPreview();
    }
}