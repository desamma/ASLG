using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;

public class RecipeListItem : MonoBehaviour,
    IPointerClickHandler,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [Header("UI")]
    public Image iconImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI typeRarityText;
    public Image statusDot;
    public Image background;
    public Image selectionHighlight;

    [Header("Colors – Row")]
    public Color bgNormal = new Color(0.70f, 0.59f, 0.42f, 1f);
    public Color bgSelected = new Color(0.55f, 0.39f, 0.22f, 1f);
    public Color bgHover = new Color(0.73f, 0.63f, 0.47f, 1f);

    [Header("Colors – Status")]
    public Color dotCanCraft = new Color(0.30f, 1.00f, 0.40f, 1f);
    public Color dotCantCraft = new Color(1.00f, 0.30f, 0.30f, 1f);

    [Header("Colors – Rarity")]
    public Color colorCommon = new Color(0.80f, 0.80f, 0.80f);
    public Color colorUncommon = new Color(0.20f, 0.80f, 0.20f);
    public Color colorRare = new Color(0.20f, 0.60f, 1.00f);
    public Color colorEpic = new Color(0.80f, 0.20f, 0.95f);
    public Color colorLegendary = new Color(1.00f, 0.70f, 0.00f);

    [Range(0.3f, 1f)] public float alphaCantCraft = 0.55f;

    [Header("Layout")]
    [SerializeField] private float rowHeight = 36f;
    [SerializeField] private float iconSize = 24f;
    [SerializeField] private float leftPadding = 8f;
    [SerializeField] private float rightPadding = 10f;
    [SerializeField] private float nameFontSize = 14f;
    [SerializeField] private float rarityFontSize = 11f;

    private Action _onClick;
    private bool _isSelected;

    private void Awake() { EnsureRaycastSurface(); ApplyCompactLayout(); }

    private void EnsureRaycastSurface()
    {
        if (background == null) { var t = transform.Find("Background"); if (t != null) background = t.GetComponent<Image>(); }
        if (background == null) background = GetComponent<Image>();
        if (background == null) background = gameObject.AddComponent<Image>();
        background.color = bgNormal; background.raycastTarget = true;
        if (selectionHighlight != null) selectionHighlight.enabled = false;
        var cg = GetComponent<CanvasGroup>(); if (cg != null) { cg.interactable = true; cg.blocksRaycasts = true; }
    }

    private static void SetStretch(RectTransform rt, float l, float r, float t, float b)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f); rt.anchoredPosition = Vector2.zero; rt.sizeDelta = Vector2.zero;
        rt.offsetMin = new Vector2(l, b); rt.offsetMax = new Vector2(-r, -t); rt.localScale = Vector3.one;
    }

    private static void SetAnchored(RectTransform rt, Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 size, Vector2 pos)
    { rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = pivot; rt.sizeDelta = size; rt.anchoredPosition = pos; rt.localScale = Vector3.one; }

    private void ApplyCompactLayout()
    {
        var le = GetComponent<LayoutElement>() ?? gameObject.AddComponent<LayoutElement>();
        le.minHeight = le.preferredHeight = rowHeight; le.flexibleHeight = 0f;

        var bgT = transform.Find("Background") ?? (background != null && background.transform != transform ? background.transform : null);
        if (bgT != null) { var rt = bgT.GetComponent<RectTransform>(); if (rt) SetStretch(rt, 0, 0, 0, 0); bgT.SetAsFirstSibling(); }

        float eff = Mathf.Min(iconSize, rowHeight - 4f), dotW = 20f, dotH = 20f;
        float dotX = -(rightPadding + dotW * 0.5f + 2f);

        var iconT = transform.Find("IconImage");
        if (iconT != null)
        {
            var rt = iconT.GetComponent<RectTransform>();
            if (rt) SetAnchored(rt, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(eff, eff), new Vector2(leftPadding + eff * 0.5f, 0));
            var img = iconT.GetComponent<Image>(); if (img) img.preserveAspect = true;
        }

        var infoT = transform.Find("InfoGroup");
        if (infoT != null)
        {
            var hlg = infoT.GetComponent<HorizontalLayoutGroup>(); if (hlg) hlg.enabled = false;
            var vlg = infoT.GetComponent<VerticalLayoutGroup>(); if (vlg) vlg.enabled = false;
            var rt = infoT.GetComponent<RectTransform>(); if (rt) SetStretch(rt, leftPadding + eff + 6f, rightPadding + dotW + 8f, 0, 0);

            var nameT = infoT.Find("NameText") ?? (nameText != null ? nameText.transform : null);
            if (nameT != null)
            {
                var nRT = nameT.GetComponent<RectTransform>();
                if (nRT) { nRT.anchorMin = new Vector2(0, 0.5f); nRT.anchorMax = new Vector2(1, 1); nRT.pivot = new Vector2(0, 1); nRT.anchoredPosition = new Vector2(0, -1); nRT.sizeDelta = Vector2.zero; nRT.offsetMin = Vector2.zero; nRT.offsetMax = new Vector2(0, -1); nRT.localScale = Vector3.one; }
                var tmp = nameT.GetComponent<TextMeshProUGUI>();
                if (tmp) { tmp.fontSize = nameFontSize; tmp.fontStyle = FontStyles.Bold; tmp.alignment = TextAlignmentOptions.BottomLeft; tmp.overflowMode = TextOverflowModes.Ellipsis; tmp.enableWordWrapping = false; tmp.raycastTarget = false; }
            }

            var rarT = infoT.Find("TypeRarityText") ?? (typeRarityText != null ? typeRarityText.transform : null);
            if (rarT != null)
            {
                var rRT = rarT.GetComponent<RectTransform>();
                if (rRT) { rRT.anchorMin = new Vector2(0, 0); rRT.anchorMax = new Vector2(1, 0.5f); rRT.pivot = new Vector2(0, 0); rRT.anchoredPosition = new Vector2(0, 1); rRT.sizeDelta = Vector2.zero; rRT.offsetMin = new Vector2(0, 1); rRT.offsetMax = Vector2.zero; rRT.localScale = Vector3.one; }
                var tmp = rarT.GetComponent<TextMeshProUGUI>();
                if (tmp) { tmp.fontSize = rarityFontSize; tmp.fontStyle = FontStyles.Normal; tmp.alignment = TextAlignmentOptions.TopLeft; tmp.overflowMode = TextOverflowModes.Ellipsis; tmp.enableWordWrapping = false; tmp.raycastTarget = false; }
            }
        }

        var dotT = transform.Find("StatusDot");
        if (dotT != null)
        {
            var rt = dotT.GetComponent<RectTransform>();
            if (rt) SetAnchored(rt, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(dotW, dotH), new Vector2(dotX, 0));
        }
    }

    /// <summary>Setup dùng RecipeDefinition từ RecipeDatabase (JSON).</summary>
    public void Setup(RecipeDefinition recipe, bool canCraft, bool isSelected, Action onClick)
    {
        EnsureRaycastSurface(); ApplyCompactLayout();
        _onClick = onClick; _isSelected = isSelected;
        if (recipe == null) return;

        var def = recipe.GetResultDefinition();
        if (def == null) { if (nameText) nameText.text = recipe.resultItemKey; return; }

        if (iconImage) { var icon = def.GetIcon(); iconImage.sprite = icon; iconImage.enabled = icon != null; }
        if (nameText) nameText.text = def.name;
        if (typeRarityText) { typeRarityText.text = $"{def.itemType} · {def.rarity}"; typeRarityText.color = GetRarityColor(def.rarity); }
        if (statusDot) statusDot.color = canCraft ? dotCanCraft : dotCantCraft;
        if (background) { background.color = isSelected ? bgSelected : bgNormal; background.raycastTarget = true; }
        if (selectionHighlight) selectionHighlight.enabled = isSelected;

        var cg = GetComponent<CanvasGroup>();
        if (cg != null) { cg.alpha = canCraft ? 1f : alphaCantCraft; cg.interactable = true; cg.blocksRaycasts = true; }
    }

    public void OnPointerClick(PointerEventData e) { _onClick?.Invoke(); }
    public void OnPointerEnter(PointerEventData e) { if (!_isSelected && background) background.color = bgHover; }
    public void OnPointerExit(PointerEventData e) { if (!_isSelected && background) background.color = _isSelected ? bgSelected : bgNormal; }

    private Color GetRarityColor(string rarity) => rarity?.ToLower() switch
    {
        "common" => colorCommon,
        "uncommon" => colorUncommon,
        "rare" => colorRare,
        "epic" => colorEpic,
        "legendary" => colorLegendary,
        _ => Color.white
    };
}

public static class CanvasGroupExtensions
{
    public static void SetAlpha(this CanvasGroup cg, float alpha) { if (cg != null) cg.alpha = alpha; }
}
