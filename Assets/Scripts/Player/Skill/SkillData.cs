using UnityEngine;
using UnityEngine.UI;

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
    public float chargeTime = 0.4f;
    public GameObject slashVfxPrefab;      
    public int slashVfxCount = 3;           
    public float slashVfxStepDistance = 1.5f; 
    public float slashVfxStepDelay = 0.25f;  
    public float slashHitRadius = 0.8f;     
    public float slashVfxDuration = 1f;      

    [Header("Archer — fan shot")]
    public GameObject arrowPrefab;       
    public int arrowCount = 8;
    public float fanAngle = 120f;        
    public float arrowSpeed = 16f;
    public float arrowLifetime = 2f;

    [Header("Rogue — shadow step")]
    public float teleportRange = 7f;
    public GameObject teleportFxPrefab; 

    [Header("Upgrades (up to 3 tiers)")]
    public SkillUpgradeTier[] upgrades = new SkillUpgradeTier[3];
}