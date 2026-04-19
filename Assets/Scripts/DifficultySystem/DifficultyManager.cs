using System;
using UnityEngine;

/// <summary>
/// Singleton manager that holds the current difficulty settings for all enemies.
/// Now supports both stat modifiers and behavior profile changes for dynamic difficulty adjustment.
/// All multipliers can be configured via Inspector for easy difficulty tuning.
/// </summary>
public class DifficultyManager : MonoBehaviour
{
    private static DifficultyManager instance;

    [Header("Difficulty Configuration")]
    [Tooltip("Overall difficulty multiplier (applies when specific multipliers are null)")]
    [SerializeField] private float overall = 1f;

    [Header("Enemy Stat Multipliers")]
    [Tooltip("Leave at 0 to use Overall multiplier")]
    [SerializeField] private float maxHPMultiplier;
    
    [Tooltip("Leave at 0 to use Overall multiplier")]
    [SerializeField] private float strengthMultiplier;
    
    [Tooltip("Leave at 0 to use Overall multiplier")]
    [SerializeField] private float magicMultiplier;
    
    [Tooltip("Leave at 0 to use Overall multiplier")]
    [SerializeField] private float defenseMultiplier;
    
    [Tooltip("Leave at 0 to use Overall multiplier")]
    [SerializeField] private float magicResistMultiplier;
    
    [Tooltip("Leave at 0 to use Overall multiplier")]
    [SerializeField] private float speedMultiplier;
    
    [Tooltip("Leave at 0 to use Overall multiplier")]
    [SerializeField] private float attackSpeedMultiplier;

    [Tooltip("Leave at 0 to use Overall multiplier")]
    [SerializeField] private float goldRewardMultiplier;

    [Header("Behavior Profile Multipliers")]
    [Tooltip("Leave at 0 to use Overall multiplier")]
    [SerializeField] private float detectionRangeMultiplier;
    
    [Tooltip("Leave at 0 to use Overall multiplier")]
    [SerializeField] private float chaseRangeMultiplier;
    
    [Tooltip("Leave at 0 to use Overall multiplier")]
    [SerializeField] private float specialAttackFrequencyMultiplier;
    
    [Tooltip("Leave at 0 to use Overall multiplier")]
    [SerializeField] private float ultimateAttackFrequencyMultiplier;
    
    [Tooltip("Leave at 0 to use Overall multiplier")]
    [SerializeField] private float aggressionMultiplier;
    
    [Tooltip("Leave at 0 to use Overall multiplier")]
    [SerializeField] private float minimumDistanceMultiplier;
    
    [Tooltip("Leave at 0 to use Overall multiplier")]
    [SerializeField] private float mobilityUsageFrequencyMultiplier;
    
    [Tooltip("Leave at 0 to use Overall multiplier")]
    [SerializeField] private float enrageThresholdMultiplier;

    private DifficultyModifier currentDifficulty;

    public DifficultyModifier CurrentDifficulty
    {
        get
        {
            if (currentDifficulty == null)
            {
                BuildDifficultyModifier();
            }
            return currentDifficulty;
        }
    }

    public event Action<DifficultyModifier> OnDifficultyChanged;

    public static DifficultyManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<DifficultyManager>();

                if (instance == null)
                {
                    GameObject go = new GameObject("DifficultyManager");
                    instance = go.AddComponent<DifficultyManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        BuildDifficultyModifier();
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            BuildDifficultyModifier();
            OnDifficultyChanged?.Invoke(currentDifficulty);
        }
    }

    private void BuildDifficultyModifier()
    {
        currentDifficulty = new DifficultyModifier
        {
            Overall = overall,
            MaxHPMultiplier = maxHPMultiplier > 0 ? maxHPMultiplier : null,
            StrengthMultiplier = strengthMultiplier > 0 ? strengthMultiplier : null,
            MagicMultiplier = magicMultiplier > 0 ? magicMultiplier : null,
            DefenseMultiplier = defenseMultiplier > 0 ? defenseMultiplier : null,
            MagicResistMultiplier = magicResistMultiplier > 0 ? magicResistMultiplier : null,
            SpeedMultiplier = speedMultiplier > 0 ? speedMultiplier : null,
            AttackSpeedMultiplier = attackSpeedMultiplier > 0 ? attackSpeedMultiplier : null,
            GoldRewardMultiplier = goldRewardMultiplier > 0 ? goldRewardMultiplier : null,
            DetectionRangeMultiplier = detectionRangeMultiplier > 0 ? detectionRangeMultiplier : null,
            ChaseRangeMultiplier = chaseRangeMultiplier > 0 ? chaseRangeMultiplier : null,
            SpecialAttackFrequencyMultiplier = specialAttackFrequencyMultiplier > 0 ? specialAttackFrequencyMultiplier : null,
            UltimateAttackFrequencyMultiplier = ultimateAttackFrequencyMultiplier > 0 ? ultimateAttackFrequencyMultiplier : null,
            AggressionMultiplier = aggressionMultiplier > 0 ? aggressionMultiplier : null,
            MinimumDistanceMultiplier = minimumDistanceMultiplier > 0 ? minimumDistanceMultiplier : null,
            MobilityUsageFrequencyMultiplier = mobilityUsageFrequencyMultiplier > 0 ? mobilityUsageFrequencyMultiplier : null,
            EnrageThresholdMultiplier = enrageThresholdMultiplier > 0 ? enrageThresholdMultiplier : null
        };
    }

    /// <summary>
    /// Change the current difficulty dynamically and notify subscribers.
    /// </summary>
    public void SetDifficulty(DifficultyModifier newDifficulty)
    {
        currentDifficulty = newDifficulty;
        
        overall = newDifficulty.Overall;
        maxHPMultiplier = newDifficulty.MaxHPMultiplier ?? 0;
        strengthMultiplier = newDifficulty.StrengthMultiplier ?? 0;
        magicMultiplier = newDifficulty.MagicMultiplier ?? 0;
        defenseMultiplier = newDifficulty.DefenseMultiplier ?? 0;
        magicResistMultiplier = newDifficulty.MagicResistMultiplier ?? 0;
        speedMultiplier = newDifficulty.SpeedMultiplier ?? 0;
        attackSpeedMultiplier = newDifficulty.AttackSpeedMultiplier ?? 0;
        goldRewardMultiplier = newDifficulty.GoldRewardMultiplier ?? 0;
        detectionRangeMultiplier = newDifficulty.DetectionRangeMultiplier ?? 0;
        chaseRangeMultiplier = newDifficulty.ChaseRangeMultiplier ?? 0;
        specialAttackFrequencyMultiplier = newDifficulty.SpecialAttackFrequencyMultiplier ?? 0;
        ultimateAttackFrequencyMultiplier = newDifficulty.UltimateAttackFrequencyMultiplier ?? 0;
        aggressionMultiplier = newDifficulty.AggressionMultiplier ?? 0;
        minimumDistanceMultiplier = newDifficulty.MinimumDistanceMultiplier ?? 0;
        mobilityUsageFrequencyMultiplier = newDifficulty.MobilityUsageFrequencyMultiplier ?? 0;
        enrageThresholdMultiplier = newDifficulty.EnrageThresholdMultiplier ?? 0;

        OnDifficultyChanged?.Invoke(currentDifficulty);
    }
}
