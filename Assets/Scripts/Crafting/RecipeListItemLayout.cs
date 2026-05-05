using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Gắn script này vào root GameObject của prefab RecipeListItem.
/// tự động set Anchors + RectTransform cho tất cả các child
/// </summary>
[ExecuteAlways]
public class RecipeListItemLayout : MonoBehaviour
{
    [Header("Row")]
    public float rowHeight = 52f;         

    [Header("Icon")]
    public float iconSize   = 36f;          
    public float iconLeft   = 8f;           

    [Header("Text")]
    public float textLeft   = 54f;          
    public float textRight  = 28f;          
    public float nameHeight = 22f;          
    public float subHeight  = 18f;          

    [Header("Status Dot")]
    public float dotSize    = 10f;
    public float dotRight   = 10f;          

    [Header("References (tuỳ chọn – tự tìm nếu để trống)")]
    public RectTransform iconRect;
    public RectTransform nameRect;
    public RectTransform typeRarityRect;
    public RectTransform dotRect;
    public RectTransform backgroundRect;
    public RectTransform highlightRect;

    void Awake()  => Apply();
    void OnValidate() => Apply();   

    [ContextMenu("Apply Layout Now")]
    public void Apply()
    {
        var item = GetComponent<RecipeListItem>();
        if (item != null) AutoResolveRefs(item);

        var selfRect = GetComponent<RectTransform>();
        if (selfRect == null) return;

        SetStretchH(selfRect, 0, 0);
        selfRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, rowHeight);

        if (backgroundRect)
            SetFill(backgroundRect);

        if (highlightRect)
            SetFill(highlightRect);

        if (iconRect)
        {
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot     = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = new Vector2(iconLeft, 0f);
            iconRect.sizeDelta = new Vector2(iconSize, iconSize);
        }

        float textBlockH = nameHeight + subHeight;
        float textBlockY = textBlockH * 0.5f;   

        if (nameRect)
        {
            nameRect.anchorMin = new Vector2(0f, 0.5f);
            nameRect.anchorMax = new Vector2(1f, 0.5f);
            nameRect.pivot     = new Vector2(0f, 0f);
            nameRect.offsetMin = new Vector2(textLeft,  textBlockY - textBlockH);
            nameRect.offsetMax = new Vector2(-textRight, textBlockY - subHeight);
        }

        if (typeRarityRect)
        {
            typeRarityRect.anchorMin = new Vector2(0f, 0.5f);
            typeRarityRect.anchorMax = new Vector2(1f, 0.5f);
            typeRarityRect.pivot     = new Vector2(0f, 0f);
            typeRarityRect.offsetMin = new Vector2(textLeft,  textBlockY - textBlockH);
            typeRarityRect.offsetMax = new Vector2(-textRight, textBlockY - nameHeight);
        }

        if (dotRect)
        {
            dotRect.anchorMin = new Vector2(1f, 0.5f);
            dotRect.anchorMax = new Vector2(1f, 0.5f);
            dotRect.pivot     = new Vector2(1f, 0.5f);
            dotRect.anchoredPosition = new Vector2(-dotRight, 0f);
            dotRect.sizeDelta = new Vector2(dotSize, dotSize);
        }
    }


    /// Stretch full width (horizontal), giữ nguyên Y
    private static void SetStretchH(RectTransform rt, float left, float right)
    {
        rt.anchorMin = new Vector2(0f, rt.anchorMin.y);
        rt.anchorMax = new Vector2(1f, rt.anchorMax.y);
        rt.offsetMin = new Vector2(left,  rt.offsetMin.y);
        rt.offsetMax = new Vector2(-right, rt.offsetMax.y);
    }

    /// Fill toàn bộ parent
    private static void SetFill(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    /// Tự tìm references qua RecipeListItem component
    private void AutoResolveRefs(RecipeListItem item)
    {
        if (!iconRect        && item.iconImage)         iconRect        = item.iconImage.rectTransform;
        if (!nameRect        && item.nameText)          nameRect        = item.nameText.rectTransform;
        if (!typeRarityRect  && item.typeRarityText)    typeRarityRect  = item.typeRarityText.rectTransform;
        if (!dotRect         && item.statusDot)         dotRect         = item.statusDot.rectTransform;
        if (!backgroundRect  && item.background)        backgroundRect  = item.background.rectTransform;
        if (!highlightRect   && item.selectionHighlight) highlightRect  = item.selectionHighlight.rectTransform;
    }
}
