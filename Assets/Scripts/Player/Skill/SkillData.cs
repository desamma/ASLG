using UnityEngine;

[CreateAssetMenu(fileName = "NewSkillData", menuName = "RPG/Skill Data")]
public class SkillData : ScriptableObject
{
    [Header("Identity")]
    public string skillName;
    [TextArea] public string description;
    public Sprite icon;

    [Header("Base cost & cooldown")]
    public float manaCost = 30f;
    public float cooldown = 8f;
    [Tooltip("Damage = player base damage × this value")]
    public float damagePercent = 1.5f;

    [Header("Knight — charged slash")]
    public GameObject slashPrefab;
    public float slashSpeed = 10f;
    public float slashDuration = 1f;
    public float chargeTime = 0.4f;      // lock duration before slash fires

    [Header("Archer — fan shot")]
    public GameObject arrowPrefab;       // reuse your ArrowProjectile prefab
    public int arrowCount = 8;
    public float fanAngle = 120f;        // total spread in degrees
    public float arrowSpeed = 16f;
    public float arrowLifetime = 2f;

    [Header("Rogue — shadow step")]
    public float teleportRange = 7f;
    public GameObject teleportFxPrefab; // optional burst effect at origin + destination

    [Header("Upgrades (up to 3 tiers)")]
    public SkillUpgradeTier[] upgrades = new SkillUpgradeTier[3];
}