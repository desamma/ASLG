using UnityEngine;

public enum StatusEffectType
{
    Buff,
    Debuff,
    Neutral
}

public enum StackBehavior
{
    RefreshDuration,
    AddDuration,
    AddStack,
    Ignore
}

[CreateAssetMenu(fileName = "New StatusEffect", menuName = "StatusEffectSystem/StatusEffect")]
public class StatusEffect : ScriptableObject
{
    [Header("Identity")]
    public string effectId;
    public string displayName;
    public StatusEffectType effectType = StatusEffectType.Debuff;

    [Header("Tooltip Description")]
    [TextArea(2, 4)]
    [Tooltip("Main description shown at the top of the tooltip.")]
    public string description;

    [TextArea(1, 2)]
    [Tooltip("Short flavour line shown below the description in italics. Leave blank to hide.")]
    public string flavourText;

    [Tooltip("Stat bullet lines, e.g. '-5 HP per second', '+30% Move Speed'.\n" +
             "Each entry is one bullet point. Supports rich text tags like <color=red>.")]
    public string[] statLines;

    [Tooltip("Show a live 'Duration: X.Xs' line in the tooltip.")]
    public bool showDurationInTooltip = true;

    [Tooltip("Show a 'Stacks: N' line when stack count > 1.")]
    public bool showStacksInTooltip = true;

    [Header("Visuals")]
    [Tooltip("Prefab instantiated inside the HUD slot.")]
    public GameObject iconPrefab;
    public GameObject vfxPrefab;
    public Color borderColor = Color.white;

    [Header("Tooltip Visuals")]
    [Tooltip("Optional sprite shown in the tooltip header area.")]
    public Sprite tooltipIcon;
    [Tooltip("Header colour in the tooltip. Uses borderColor if left as clear.")]
    public Color tooltipHeaderColor = Color.clear;

    [Header("Icon Prefab Overrides")]
    public Vector3 iconLocalPosition = Vector3.zero;
    public Vector3 iconLocalScale = Vector3.one;

    [Header("Duration")]
    public bool isPermanent = false;
    public float baseDuration = 5f;

    [Header("Stacking")]
    public StackBehavior stackBehavior = StackBehavior.RefreshDuration;
    public int maxStacks = 1;

    /// <summary>
    /// Resolved header colour: tooltipHeaderColor if set, else borderColor.
    /// </summary>
    public Color ResolvedHeaderColor =>
        tooltipHeaderColor == Color.clear ? borderColor : tooltipHeaderColor;

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(effectId))
            effectId = name.ToLower().Replace(" ", "_");
    }
}