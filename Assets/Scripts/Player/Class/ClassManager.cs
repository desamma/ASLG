using UnityEngine;

/// <summary>
/// Persists across scenes. Select the class before entering gameplay,
/// then ApplyToStatsManager() is called automatically on Start.
/// </summary>
public class ClassManager : MonoBehaviour
{
    public static ClassManager Instance { get; private set; }

    [Header("Class Data Assets")]
    [SerializeField] private PlayerClassData knightData;
    [SerializeField] private PlayerClassData archerData;
    [SerializeField] private PlayerClassData rogueData;
    [SerializeField] private PlayerClassData summonerData;
    //[SerializeField] private PlayerClassData testData;

    public PlayerClassData CurrentClassData { get; private set; }
    public PlayerClass SelectedClass { get; private set; } = PlayerClass.Knight;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            //SelectClass(SelectedClass);
        }
        else
        {
            Destroy(gameObject);
        }
    }


    public void SelectClass(PlayerClass playerClass)
    {
        SelectedClass = playerClass;
        CurrentClassData = playerClass switch
        {
            PlayerClass.Knight => knightData,
            PlayerClass.Archer => archerData,
            PlayerClass.Rogue => rogueData,
            //PlayerClass.Test => testData,
            PlayerClass.Summoner => summonerData != null ? summonerData : knightData,
            _ => knightData
        };

        if (CurrentClassData == null)
            Debug.LogWarning($"[ClassManager] No data asset assigned for {playerClass}.");
    }

    public PlayerClassData GetDataFor(PlayerClass playerClass) => playerClass switch
    {
        PlayerClass.Knight => knightData,
        PlayerClass.Archer => archerData,
        PlayerClass.Rogue => rogueData,
        //PlayerClass.Test => testData,
        PlayerClass.Summoner => summonerData != null ? summonerData : knightData,
        _ => knightData
    };

    public void ApplyToStatsManager()
    {
        if (CurrentClassData == null || StatsManager.instance == null)
        {
            Debug.LogWarning("[ClassManager] Cannot apply stats — data or StatsManager missing.");
            return;
        }

        var s = StatsManager.instance;
        var d = CurrentClassData;

        s.baseMaxHealth = d.maxHealth;
        s.baseDefence = d.defence;
        s.baseMoveSpeed = d.moveSpeed;
        s.baseMaxStamina = d.maxStamina;
        s.staminaCost = d.staminaCost;
        s.dashDuration = d.dashDuration;
        s.dashDelay = d.dashDelay;
        s.baseDamage = d.damage;
        s.weaponRange = d.weaponRange;
        s.baseCooldown = d.attackCooldown;
        s.knockbackForce = d.knockbackForce;
        s.knockbackTime = d.knockbackTime;
        s.stunTime = d.stunTime;

        s.ResetStats();

        Debug.Log($"[ClassManager] Applied {d.playerClass} stats to StatsManager.");
    }

    public bool IsRangedClass() => CurrentClassData != null &&
                                   CurrentClassData.attackType == AttackType.Ranged;
}