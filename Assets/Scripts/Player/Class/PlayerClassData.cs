using UnityEngine;

[CreateAssetMenu(fileName = "NewClassData", menuName = "RPG/Player Class Data")]
public class PlayerClassData : ScriptableObject
{
    [Header("Prefab")]
    public GameObject playerPrefab;

    [Header("Identity")]
    public PlayerClass playerClass;
    public string displayName;
    [TextArea] public string description;

    [Header("Health & Defence")]
    public float maxHealth = 2000f;
    public float defence = 0f;           // flat damage reduction

    [Header("Movement")]
    public float moveSpeed = 5f;
    public float maxStamina = 100f;
    public float staminaCost = 25f;
    public float dashDuration = 0.2f;
    public float dashDelay = 0.5f;

    [Header("Combat")]
    public AttackType attackType = AttackType.Melee;
    public float damage = 10;
    public float attackCooldown = 0.5f;
    public float weaponRange = 1f;       // melee radius OR projectile lifetime (seconds)
    public float knockbackForce = 5f;
    public float knockbackTime = 0.2f;
    public float stunTime = 0.3f;

    [Header("Skill")]
    public SkillData skillData;

    [Header("Ranged (Archer only)")]
    public float projectileSpeed = 12f;

    [Header("Visuals / Prefabs")]
    public GameObject hitEffectPrefab;
    public GameObject projectilePrefab;  // ArrowProjectile prefab (Archer)

    [Header("Audio")]
    public AudioClip swingClip;
    public AudioClip hitClip;
}