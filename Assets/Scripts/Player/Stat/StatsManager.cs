using UnityEngine;

public class StatsManager : MonoBehaviour
{
    public static StatsManager instance { get; private set; }

    [Header("References")]
    //[SerializeField] private ExpManager expManager;
    //[SerializeField] private StatsUI statsUI;

    [Header("Health")]
    [SerializeField] private float _maxHealth = 2000f;
    public float maxHealth
    {
        get => _maxHealth;
        set { _maxHealth = Mathf.Max(0f, value); OnStatsChanged(); }
    }

    private float _currentHealth;
    public float currentHealth
    {
        get => _currentHealth;
        set { _currentHealth = Mathf.Clamp(value, 0f, maxHealth); OnStatsChanged(); }
    }

    [Header("Mana")]
    [SerializeField] private float _maxMana = 100f;
    public float maxMana
    {
        get => _maxMana;
        set { _maxMana = Mathf.Max(0f, value); OnStatsChanged(); }
    }

    private float _currentMana;
    public float currentMana
    {
        get => _currentMana;
        set { _currentMana = Mathf.Clamp(value, 0f, maxMana); OnStatsChanged(); }
    }

    public bool IsDead => currentHealth <= 0f;

    [Header("Movement & Stamina")]
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _maxStamina = 100f;
    [SerializeField] private float _staminaRegenRate = 10f;
    [SerializeField] private float _staminaCost = 25f;
    [SerializeField] private float _dashDelay = 0.5f;
    [SerializeField] private float _dashDuration = 0.2f;

    public float moveSpeed { get => _moveSpeed; set { OnStatsChanged(); } }
    public float maxStamina { get => _maxStamina; set { _maxStamina = Mathf.Max(0f, value); OnStatsChanged(); } }
    public float staminaRegenRate { get => _staminaRegenRate; set { OnStatsChanged(); } }
    public float staminaCost { get => _staminaCost; set { OnStatsChanged(); } }
    public float dashDelay { get => _dashDelay; set { OnStatsChanged(); } }
    public float dashDuration { get => _dashDuration; set { OnStatsChanged(); } }

    private float _currentStamina;
    public float currentStamina
    {
        get => _currentStamina;
        set { _currentStamina = Mathf.Clamp(value, 0f, maxStamina); OnStatsChanged(); }
    }

    [Header("Combat")]
    [SerializeField] private int _damage = 10;
    [SerializeField] private float _weaponRange = 1.5f;
    [SerializeField] private float _knockbackForce = 5f;
    [SerializeField] private float _knockbackTime = 0.2f;
    [SerializeField] private float _stunTime = 0.3f;
    [SerializeField] private float _cooldown = 0.5f;

    public int damage { get => _damage; set { OnStatsChanged(); } }
    public float weaponRange { get => _weaponRange; set { OnStatsChanged(); } }
    public float knockbackForce { get => _knockbackForce; set { OnStatsChanged(); } }
    public float knockbackTime { get => _knockbackTime; set { OnStatsChanged(); } }
    public float stunTime { get => _stunTime; set { OnStatsChanged(); } }
    public float cooldown { get => _cooldown; set { OnStatsChanged(); } }

    [Header("Experience & Levelling")]
    [SerializeField] private int _level = 1;
    [SerializeField] private int _expToNextLevel = 100;
    [SerializeField] private int _currentExp = 0;
    [SerializeField] private int _upgradePoints = 0;

    public int level { get => _level; private set { _level = Mathf.Max(1, value); OnStatsChanged(); } }
    public int expToNextLevel { get => _expToNextLevel; private set { _expToNextLevel = Mathf.Max(1, value); OnStatsChanged(); } }
    public int currentExp { get => _currentExp; private set { _currentExp = Mathf.Max(0, value); OnStatsChanged(); } }
    public int upgradePoints { get => _upgradePoints; private set { _upgradePoints = Mathf.Max(0, value); OnStatsChanged(); } }

    // --- SỰ KIỆN ---
    public event System.Action OnStatsChangedEvent;
    public event System.Action OnLevelUpEvent;
    public event System.Action OnPlayerDeathEvent; // Báo tử

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            InitialiseStats();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitialiseStats()
    {
        _currentHealth = _maxHealth;
        _currentStamina = _maxStamina;
    }

    // HÀM MỚI: Dùng để hồi sinh khi người chơi bấm Play lại từ Scene Start
    public void ResetStats()
    {
        _currentHealth = _maxHealth;
        _currentStamina = _maxStamina;
        OnStatsChanged();
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= Mathf.Abs(amount);
        if (IsDead) HandleDeath();
    }

    public void Heal(float amount) => currentHealth += Mathf.Abs(amount);

    public void AddExp(int amount)
    {
        currentExp += amount;
        while (currentExp >= expToNextLevel)
        {
            currentExp -= expToNextLevel;
            level += 1;
            upgradePoints += 1;
            expToNextLevel = CalculateExpToNextLevel(level);
            OnLevelUpEvent?.Invoke();
        }
        OnStatsChanged();
    }

    private void OnStatsChanged()
    {
        //statsUI?.Refresh();
        OnStatsChangedEvent?.Invoke();
    }

    private void HandleDeath()
    {
        Debug.Log("Player has died.");
        OnPlayerDeathEvent?.Invoke(); // Kích hoạt sự kiện để UI bắt lấy
    }

    private int CalculateExpToNextLevel(int nextLevel) =>
        Mathf.RoundToInt(100 * Mathf.Pow(nextLevel, 1.5f));
}