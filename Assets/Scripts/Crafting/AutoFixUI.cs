using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class AutoFixUI : MonoBehaviour
{
    public enum RowPreset
    {
        Small,
        Medium,
        Large
    }

    [Header("Preset")]
    [SerializeField] private RowPreset preset = RowPreset.Small;

    [Header("Layout")]
    [SerializeField] private float rowHeight = 34f;
    [SerializeField] private float iconSize = 18f;
    [SerializeField] private float leftPadding = 8f;
    [SerializeField] private float rightPadding = 8f;
    [SerializeField] private float contentTopBottomPadding = 2f;

    [ContextMenu("Apply Preset")]
    public void ApplyPreset()
    {
        switch (preset)
        {
            case RowPreset.Small:
                rowHeight = 32f;
                iconSize = 16f;
                leftPadding = 8f;
                rightPadding = 8f;
                contentTopBottomPadding = 2f;
                break;

            case RowPreset.Medium:
                rowHeight = 38f;
                iconSize = 20f;
                leftPadding = 10f;
                rightPadding = 10f;
                contentTopBottomPadding = 3f;
                break;

            case RowPreset.Large:
                rowHeight = 44f;
                iconSize = 24f;
                leftPadding = 12f;
                rightPadding = 12f;
                contentTopBottomPadding = 4f;
                break;
        }

        FixMyUI();
    }

    [ContextMenu("Apply Small")]
    public void ApplySmall()
    {
        preset = RowPreset.Small;
        ApplyPreset();
    }

    [ContextMenu("Apply Medium")]
    public void ApplyMedium()
    {
        preset = RowPreset.Medium;
        ApplyPreset();
    }

    [ContextMenu("Apply Large")]
    public void ApplyLarge()
    {
        preset = RowPreset.Large;
        ApplyPreset();
    }

    [ContextMenu("Fix Recipe Item UI")]
    public void FixMyUI()
    {
        // 1) Root height
        var rootRt = GetComponent<RectTransform>();
        var le = GetComponent<LayoutElement>();
        if (le == null) le = gameObject.AddComponent<LayoutElement>();

        le.minHeight = rowHeight;
        le.preferredHeight = rowHeight;
        le.flexibleHeight = 0f;

        if (rootRt != null)
            rootRt.sizeDelta = new Vector2(rootRt.sizeDelta.x, rowHeight);

        // 2) Parent VerticalLayoutGroup (avoid oversized rows)
        if (transform.parent != null)
        {
            var vlg = transform.parent.GetComponent<VerticalLayoutGroup>();
            if (vlg != null)
            {
                vlg.childControlHeight = true;
                vlg.childForceExpandHeight = false;
                if (vlg.spacing > 2f) vlg.spacing = 2f;
            }
        }

        // 3) Background full stretch
        var bg = transform.Find("Background");
        if (bg != null)
        {
            var bgRt = bg.GetComponent<RectTransform>();
            if (bgRt != null)
            {
                bgRt.anchorMin = new Vector2(0f, 0f);
                bgRt.anchorMax = new Vector2(1f, 1f);
                bgRt.offsetMin = Vector2.zero;
                bgRt.offsetMax = Vector2.zero;
                bgRt.localPosition = new Vector3(bgRt.localPosition.x, bgRt.localPosition.y, 0f);
            }

            var bgImage = bg.GetComponent<Image>();
            if (bgImage != null)
                bgImage.raycastTarget = true;
        }

        // 4) IconImage
        var icon = transform.Find("IconImage");
        if (icon != null)
        {
            var rt = icon.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0f, 0.5f);
                rt.anchorMax = new Vector2(0f, 0.5f);
                rt.pivot = new Vector2(0f, 0.5f);
                rt.sizeDelta = new Vector2(iconSize, iconSize);
                rt.anchoredPosition = new Vector2(leftPadding, 0f);
            }
        }

        // 5) InfoGroup (stretch to available space)
        var info = transform.Find("InfoGroup");
        if (info != null)
        {
            var rt = info.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(0f, 0.5f);

                var left = leftPadding + iconSize + 8f;
                var right = rightPadding + 12f;

                rt.offsetMin = new Vector2(left, contentTopBottomPadding);
                rt.offsetMax = new Vector2(-right, -contentTopBottomPadding);
                rt.localScale = Vector3.one;
            }
        }

        // 6) StatusDot
        var dot = transform.Find("StatusDot");
        if (dot != null)
        {
            var rt = dot.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(1f, 0.5f);
                rt.anchorMax = new Vector2(1f, 0.5f);
                rt.pivot = new Vector2(1f, 0.5f);
                rt.sizeDelta = new Vector2(6f, 6f);
                rt.anchoredPosition = new Vector2(-rightPadding, 0f);
            }
        }

        Debug.Log($"[AutoFixUI] Applied {preset} preset. RowHeight={rowHeight}, Icon={iconSize}");
    }
}