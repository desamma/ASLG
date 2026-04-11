using UnityEngine;

[System.Serializable]
public class SkillUpgradeTier
{
    [Header("Cost")]
    public int pointCost = 1;
    [TextArea] public string description;

    [Header("Stat modifiers (multiplicative)")]
    [Tooltip("e.g. 0.95 = 5% cheaper mana")]
    public float manaCostMultiplier = 0.95f;
    [Tooltip("e.g. 0.9 = 10% shorter cooldown")]
    public float cooldownMultiplier = 0.9f;
    [Tooltip("Added to damagePercent, e.g. 0.25 = +25% of base damage")]
    public float damagePercentBonus = 0.25f;

    [Header("Knight — slash")]
    [Tooltip("Extra seconds the slash travels")]
    public float slashDurationBonus = 0.3f;
    public float slashSpeedBonus = 1f;

    [Header("Archer — fan shot")]
    public int arrowCountBonus = 1;
    [Tooltip("Extra degrees of total fan spread")]
    public float fanAngleBonus = 20f;

    [Header("Rogue — teleport")]
    public float teleportRangeBonus = 2f;
}