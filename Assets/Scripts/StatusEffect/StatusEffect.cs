using UnityEngine;

public enum StatusEffectType
{
    Buff,
    Debuff,
    Neutral
}

public enum StackBehavior
{
    RefreshDuration,    // Resets timer on reapply
    AddDuration,        // Adds duration on reapply
    AddStack,           // Increases stack count (up to maxStacks)
    Ignore              // Does nothing if already active
}

/// <summary>
/// ScriptableObject defining a status effect's properties.
/// Create via: Assets > Create > StatusEffectSystem > StatusEffect
/// </summary>
[CreateAssetMenu(fileName = "New StatusEffect", menuName = "StatusEffectSystem/StatusEffect")]
public class StatusEffect : ScriptableObject
{
    [Header("Identity")]
    public string effectId;                         // Unique ID, e.g. "poison", "shield_buff"
    public string displayName;
    [TextArea(2, 4)]
    public string description;
    public StatusEffectType effectType = StatusEffectType.Debuff;

    [Header("Visuals")]
    [Tooltip("Prefab instantiated inside the HUD slot. Can have Animator, particles, sprites — anything.")]
    public GameObject iconPrefab;                   // Animated GameObject shown in the HUD slot
    public GameObject vfxPrefab;                    // Optional VFX spawned on the character body
    public Color borderColor = Color.white;

    [Header("Icon Prefab Overrides")]
    [Tooltip("Local position offset applied to the spawned icon inside the slot.")]
    public Vector3 iconLocalPosition = Vector3.zero;
    [Tooltip("Local scale of the spawned icon. Use to fit your art into the slot.")]
    public Vector3 iconLocalScale = Vector3.one;

    [Header("Duration")]
    public bool isPermanent = false;                // Never expires
    public float baseDuration = 5f;                 // Seconds

    [Header("Stacking")]
    public StackBehavior stackBehavior = StackBehavior.RefreshDuration;
    public int maxStacks = 1;

    [Header("Tick")]
    public bool hasTick = false;                    // Does this effect do something each tick?
    public float tickInterval = 1f;                 // Seconds between ticks

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(effectId))
            effectId = name.ToLower().Replace(" ", "_");
    }
}