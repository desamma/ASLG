using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class CraftingUI : MonoBehaviour
{
    [Header("Managers")]
    public CraftingManager craftingManager;

    [Header("Prefabs")]
    public GameObject recipeItemPrefab;
    public GameObject statRowPrefab;
    public GameObject matRowPrefab;

    [Header("Left Panel")]
    public TMP_InputField searchInput;
    public Transform recipeListContent;

    [Tooltip("Thứ tự: All, Weapon, Armor, Accessory, Consumable, Misc")]
    public Button[] categoryTabButtons;
    public Color tabActiveColor = new Color(0.16f, 0.16f, 0.16f, 1f);
    public Color tabNormalColor = new Color(0.28f, 0.19f, 0.12f, 1f);

    [Header("Recipe List Layout Fix")]
    [SerializeField] private float recipeRowHeight = 34f;
    [SerializeField] private float recipeRowSpacing = 2f;

    [Header("Mid Panel – Grid 3 Slot")]
    public Image[] gridSlotImages = new Image[3];
    public TextMeshProUGUI[] gridSlotTexts = new TextMeshProUGUI[3];

    [Header("Mid Panel – Result & Craft")]
    public Image resultIconImage;
    public Button craftButton;
    public TextMeshProUGUI craftButtonText;
    public Button qtyMinusButton;
    public Button qtyPlusButton;
    public TextMeshProUGUI qtyText;

    [Header("Right Panel – Info")]
    public Image infoIconImage;
    public TextMeshProUGUI infoNameText;
    public TextMeshProUGUI infoRarityText;
    public TextMeshProUGUI infoDescText;
    public Transform statsContainer;
    public Transform materialsContainer;

    [Header("Rarity Colors")]
    public Color colorCommon = new Color(0.67f, 0.67f, 0.67f);
    public Color colorUncommon = new Color(0.20f, 0.80f, 0.20f);
    public Color colorRare = new Color(0.20f, 0.60f, 1.00f);
    public Color colorEpic = new Color(0.80f, 0.20f, 0.95f);
    public Color colorLegendary = new Color(1.00f, 0.70f, 0.00f);

    [Header("Feedback")]
    public float craftFlashDuration = 0.4f;
    public Color craftSuccessColor = new Color(0.5f, 1f, 0.3f, 0.6f);

    private readonly List<RecipeDefinition> _filteredRecipes = new List<RecipeDefinition>();
    private RecipeDefinition _selectedRecipe;
    private int _craftQty = 1;
    private string _currentCategory = "All";
    private string _searchQuery = "";
    private int _playerLevel = 1;

    private CanvasGroup _canvasGroup;
    private bool _isOpen = false;
    public bool IsOpen => _isOpen;


    private void Start()
    {
        _canvasGroup = GetComponentInParent<CanvasGroup>();
        EnsureRecipeListLayout();

        craftButton.onClick.AddListener(OnCraftClicked);
        qtyMinusButton.onClick.AddListener(() => ChangeCraftQty(-1));
        qtyPlusButton.onClick.AddListener(() => ChangeCraftQty(1));
        searchInput.onValueChanged.AddListener(OnSearchChanged);

        foreach (var img in gridSlotImages) if (img) img.enabled = false;
        if (resultIconImage) resultIconImage.enabled = false;
        if (infoIconImage) infoIconImage.enabled = false;

        for (int i = 0; i < categoryTabButtons.Length; i++)
        {
            string cat = i switch
            {
                1 => "Accessory",
                2 => "Weapon",
                3 => "Armor",
                4 => "Consumable",
                5 => "Misc",
                _ => "All"
            };
            int idx = i;
            var btn = categoryTabButtons[idx];
            if (btn == null) continue;

            btn.onClick.AddListener(() => OnCategorySelected(cat, idx));
        }

        OnCategorySelected("All", 0);

        if (InventoryManager.instance != null)
            InventoryManager.instance.OnInventoryChanged += RefreshCurrentSelection;

        if (craftingManager != null)
        {
            craftingManager.OnCraftSuccess += HandleCraftSuccess;
            craftingManager.OnCraftFailed += HandleCraftFailed;
        }

        RefreshRecipeList();
        SetCraftButtonState(false);
        ClearInfo();
    }

    private void Update()
    {
        if (Input.GetButtonDown("Crafting") && !LLMChatManager.Instance.IsChatting) Toggle();
    }

    private void OnDestroy()
    {
        if (InventoryManager.instance != null)
            InventoryManager.instance.OnInventoryChanged -= RefreshCurrentSelection;

        if (craftingManager != null)
        {
            craftingManager.OnCraftSuccess -= HandleCraftSuccess;
            craftingManager.OnCraftFailed -= HandleCraftFailed;
        }
    }


    private void EnsureRecipeListLayout()
    {
        if (recipeListContent == null) return;
        var contentRt = recipeListContent as RectTransform;
        if (contentRt == null) return;

        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.localScale = Vector3.one;

        var grid = recipeListContent.GetComponent<GridLayoutGroup>(); if (grid != null) DestroyImmediate(grid);
        var hlg = recipeListContent.GetComponent<HorizontalLayoutGroup>(); if (hlg != null) DestroyImmediate(hlg);

        var vlg = recipeListContent.GetComponent<VerticalLayoutGroup>();
        if (vlg == null) vlg = recipeListContent.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = recipeRowSpacing;
        vlg.padding = new RectOffset(4, 4, 4, 4);

        var fitter = recipeListContent.GetComponent<ContentSizeFitter>();
        if (fitter == null) fitter = recipeListContent.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }


    private void RefreshRecipeList()
    {
        var toDestroy = new List<GameObject>();
        foreach (Transform child in recipeListContent) toDestroy.Add(child.gameObject);
        foreach (var go in toDestroy) DestroyImmediate(go);

        var allAvailable = craftingManager != null
            ? craftingManager.GetAvailableRecipes(_playerLevel)
            : new List<RecipeDefinition>();

        _filteredRecipes.Clear();
        foreach (var r in allAvailable)
        {
            var def = r.GetResultDefinition();
            if (def == null) continue;

            bool matchCat = _currentCategory == "All" || def.itemType == _currentCategory;
            bool matchSearch = string.IsNullOrEmpty(_searchQuery) ||
                               r.DisplayName.ToLower().Contains(_searchQuery.ToLower());
            if (!matchCat || !matchSearch) continue;
            _filteredRecipes.Add(r);
        }

        foreach (var recipe in _filteredRecipes)
        {
            var go = Instantiate(recipeItemPrefab, recipeListContent);
            var rt = go.GetComponent<RectTransform>();
            if (rt != null) rt.localScale = Vector3.one;

            var le = go.GetComponent<LayoutElement>();
            if (le == null) le = go.AddComponent<LayoutElement>();
            le.minHeight = le.preferredHeight = recipeRowHeight;
            le.flexibleHeight = 0f;

            var row = go.GetComponent<RecipeListItem>() ?? go.GetComponentInChildren<RecipeListItem>(true);
            if (row == null) { Debug.LogError("[CraftingUI] recipeItemPrefab thiếu RecipeListItem."); continue; }

            row.Setup(recipe, recipe.CanCraft(_craftQty), _selectedRecipe == recipe, () => SelectRecipe(recipe));
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(recipeListContent as RectTransform);
        LayoutRebuilder.ForceRebuildLayoutImmediate(recipeListContent as RectTransform);

        var scroll = recipeListContent.GetComponentInParent<ScrollRect>();
        if (scroll != null) scroll.verticalNormalizedPosition = 1f;
    }

    private void OnSearchChanged(string value) { _searchQuery = value; RefreshRecipeList(); }

    private void OnCategorySelected(string cat, int btnIndex)
    {
        _currentCategory = cat;
        for (int i = 0; i < categoryTabButtons.Length; i++)
        {
            var btn = categoryTabButtons[i]; if (btn == null) continue;
            var img = btn.GetComponent<Image>();
            if (img != null) img.color = (i == btnIndex) ? tabActiveColor : tabNormalColor;
        }
        RefreshRecipeList();
    }


    private void SelectRecipe(RecipeDefinition recipe)
    {
        _selectedRecipe = recipe;
        _craftQty = 1;
        UpdateQtyDisplay();
        UpdateGrid(recipe);
        UpdateInfoPanel(recipe);
        RefreshCraftButton();
        RefreshRecipeList();
    }

    private void UpdateGrid(RecipeDefinition recipe)
    {
        int slotCount = gridSlotImages != null ? gridSlotImages.Length : 3;

        for (int i = 0; i < slotCount; i++)
        {
            bool hasItem = i < recipe.ingredients.Count &&
                           !string.IsNullOrEmpty(recipe.ingredients[i].itemKey);

            Sprite icon = null;
            int qty = 0;

            if (hasItem)
            {
                var def = recipe.ingredients[i].GetDefinition();
                icon = def?.GetIcon();
                qty = recipe.ingredients[i].amount;
            }

            var img = (gridSlotImages != null && i < gridSlotImages.Length) ? gridSlotImages[i] : null;
            var txt = (gridSlotTexts != null && i < gridSlotTexts.Length) ? gridSlotTexts[i] : null;

            if (img) { img.sprite = icon; img.enabled = hasItem; }
            if (txt) { txt.text = (hasItem && qty > 1) ? $"x{qty}" : ""; txt.enabled = hasItem; }
        }

        if (resultIconImage)
        {
            var icon = recipe.GetResultDefinition()?.GetIcon();
            resultIconImage.sprite = icon;
            resultIconImage.enabled = icon != null;
        }
    }

    private void UpdateInfoPanel(RecipeDefinition recipe)
    {
        var def = recipe.GetResultDefinition();
        if (def == null) { ClearInfo(); return; }

        if (infoIconImage) { var icon = def.GetIcon(); infoIconImage.sprite = icon; infoIconImage.enabled = icon != null; }
        if (infoNameText) infoNameText.text = def.name;
        if (infoRarityText) { infoRarityText.text = def.rarity; infoRarityText.color = GetRarityColor(def.rarity); }
        if (infoDescText) infoDescText.text = def.description;

        if (statsContainer)
        {
            foreach (Transform c in statsContainer) Destroy(c.gameObject);
            foreach (var bonus in def.statBonuses)
            {
                var row = Instantiate(statRowPrefab, statsContainer);
                var sr = row.GetComponent<StatRowUI>();
                if (sr) sr.Setup(bonus.Key, bonus.Value);
            }
        }

        if (materialsContainer)
        {
            foreach (Transform c in materialsContainer) Destroy(c.gameObject);
            foreach (var ing in recipe.ingredients)
            {
                if (string.IsNullOrEmpty(ing.itemKey)) continue;
                var ingDef = ing.GetDefinition();
                if (ingDef == null) continue;

                var row = Instantiate(matRowPrefab, materialsContainer);
                var mr = row.GetComponent<MaterialRowUI>();
                int have = InventoryManager.instance != null
                    ? InventoryManager.instance.GetItemCount(ing.itemKey) : 0;
                if (mr) mr.Setup(ingDef, ing.amount * _craftQty, have);
            }
        }
    }

    private void ClearInfo()
    {
        if (infoIconImage) { infoIconImage.sprite = null; infoIconImage.enabled = false; }
        if (infoNameText) infoNameText.text = "Select a recipe";
        if (infoRarityText) infoRarityText.text = "—";
        if (infoDescText) infoDescText.text = "Select a recipe to view its details.";
        if (statsContainer) foreach (Transform c in statsContainer) Destroy(c.gameObject);
        if (materialsContainer) foreach (Transform c in materialsContainer) Destroy(c.gameObject);
        if (resultIconImage) { resultIconImage.sprite = null; resultIconImage.enabled = false; }
    }


    private void OnCraftClicked()
    {
        if (_selectedRecipe == null || craftingManager == null) return;
        craftingManager.TryCraft(_selectedRecipe, _craftQty);
    }

    private void HandleCraftSuccess(RecipeDefinition recipe, int qty)
    {
        Debug.Log($"[Crafting] Crafted {qty}x {recipe.DisplayName}");
        StartCoroutine(FlashCraftSuccess());
        RefreshCurrentSelection();
        RefreshRecipeList();
    }

    private void HandleCraftFailed(RecipeDefinition recipe, string reason)
        => Debug.LogWarning($"[Crafting] Failed: {reason}");

    private IEnumerator FlashCraftSuccess()
    {
        if (resultIconImage == null) yield break;
        var original = resultIconImage.color;
        resultIconImage.color = craftSuccessColor;
        yield return new WaitForSeconds(craftFlashDuration);
        resultIconImage.color = original;
    }


    private void ChangeCraftQty(int delta)
    {
        _craftQty = Mathf.Clamp(_craftQty + delta, 1, 99);
        UpdateQtyDisplay();
        if (_selectedRecipe != null) UpdateInfoPanel(_selectedRecipe);
        RefreshCraftButton();
    }

    private void UpdateQtyDisplay() { if (qtyText) qtyText.text = _craftQty.ToString(); }

    private void RefreshCurrentSelection()
    {
        if (_selectedRecipe == null) return;
        UpdateInfoPanel(_selectedRecipe);
        RefreshCraftButton();
    }

    private void RefreshCraftButton()
    {
        bool can = _selectedRecipe != null && _selectedRecipe.CanCraft(_craftQty);
        SetCraftButtonState(can);
    }

    private void SetCraftButtonState(bool interactable)
    {
        if (craftButton) craftButton.interactable = interactable;
        if (craftButtonText) craftButtonText.alpha = interactable ? 1f : 0.5f;
    }

    private Color GetRarityColor(string rarity) => rarity?.ToLower() switch
    {
        "common" => colorCommon,
        "uncommon" => colorUncommon,
        "rare" => colorRare,
        "epic" => colorEpic,
        "legendary" => colorLegendary,
        _ => Color.white
    };


    public void Toggle() => SetOpen(!_isOpen);
    public void Open() => SetOpen(true);
    public void Close() => SetOpen(false);

    public void SetOpen(bool visible)
    {
        _isOpen = visible;
        if (_canvasGroup == null) _canvasGroup = GetComponentInParent<CanvasGroup>();
        if (_canvasGroup == null) return;
        _canvasGroup.alpha = _isOpen ? 1f : 0f;
        _canvasGroup.interactable = _isOpen;
        _canvasGroup.blocksRaycasts = _isOpen;
        Time.timeScale = _isOpen ? 0f : 1f;
    }
}
