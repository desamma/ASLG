using UnityEngine;
using System.IO; // THÊM thư viện đọc/ghi file

public class StatsManager : MonoBehaviour
{
    public static StatsManager instance { get; private set; }

    [Header("Player")]
    [SerializeField] private string _playerName;

    [Header("Session Data")]
    [SerializeField] private string authToken;
    [SerializeField] private string userId;

    public string AuthToken { get => authToken; private set => authToken = value; }
    public string UserId { get => userId; private set => userId = value; }

    public string playerName { get => _playerName; set { _playerName = value; OnStatsChanged(); } }

    [Header("Health")]
    [SerializeField] private float _maxHealth = 2000f;
    public float maxHealth
    {
        get => _maxHealth;
        set { _maxHealth = Mathf.Max(0f, value); OnStatsChanged(); }
    }

    [SerializeField] private float _currentHealth;
    public float currentHealth
    {
        get => _currentHealth;
        set { _currentHealth = Mathf.Clamp(value, 0f, maxHealth); OnStatsChanged(); }
    }

    [Header("Defence")]
    [SerializeField] private float _defence = 10f;
    [SerializeField] private float _magicResist = 5f;

    public float defence { get => _defence; set { _defence = Mathf.Max(0f, value); OnStatsChanged(); } }
    public float magicResist { get => _magicResist; set { _magicResist = Mathf.Max(0f, value); OnStatsChanged(); } }

    [Header("Mana")]
    [SerializeField] private float _maxMana = 100f;
    public float maxMana
    {
        get => _maxMana;
        set { _maxMana = Mathf.Max(0f, value); OnStatsChanged(); }
    }

    [SerializeField] private float _currentMana;
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

    public float moveSpeed { get => _moveSpeed; set { _moveSpeed = value; OnStatsChanged(); } }
    public float maxStamina { get => _maxStamina; set { _maxStamina = Mathf.Max(0f, value); OnStatsChanged(); } }
    public float staminaRegenRate { get => _staminaRegenRate; set { _staminaRegenRate = value; OnStatsChanged(); } }
    public float staminaCost { get => _staminaCost; set { _staminaCost = value; OnStatsChanged(); } }
    public float dashDelay { get => _dashDelay; set { _dashDelay = value; OnStatsChanged(); } }
    public float dashDuration { get => _dashDuration; set { _dashDuration = value; OnStatsChanged(); } }

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

    public int damage { get => _damage; set { _damage = value; OnStatsChanged(); } }
    public float weaponRange { get => _weaponRange; set { _weaponRange = value; OnStatsChanged(); } }
    public float knockbackForce { get => _knockbackForce; set { _knockbackForce = value; OnStatsChanged(); } }
    public float knockbackTime { get => _knockbackTime; set { _knockbackTime = value; OnStatsChanged(); } }
    public float stunTime { get => _stunTime; set { _stunTime = value; OnStatsChanged(); } }
    public float cooldown { get => _cooldown; set { _cooldown = Mathf.Max(0.1f, value); OnStatsChanged(); } }

    [Header("Experience & Levelling")]
    [SerializeField] private int _level = 1;
    [SerializeField] private int _expToNextLevel = 100;
    [SerializeField] private int _currentExp = 0;
    [SerializeField] private int _upgradePoints = 0;
    [SerializeField] private string _authToken;
    [SerializeField] private string _userId;

    public int level { get => _level; private set { _level = Mathf.Max(1, value); OnStatsChanged(); } }
    public int expToNextLevel { get => _expToNextLevel; private set { _expToNextLevel = Mathf.Max(1, value); OnStatsChanged(); } }
    public int currentExp { get => _currentExp; private set { _currentExp = Mathf.Max(0, value); OnStatsChanged(); } }
    public int upgradePoints { get => _upgradePoints; private set { _upgradePoints = Mathf.Max(0, value); OnStatsChanged(); } }

    // --- SỰ KIỆN ---
    public event System.Action OnStatsChangedEvent;
    public event System.Action OnLevelUpEvent;
    public event System.Action OnPlayerDeathEvent;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            // THÊM: Khôi phục Session khi load lại game
            AuthToken = PlayerPrefs.GetString("AuthToken", string.Empty);
            UserId = PlayerPrefs.GetString("UserId", string.Empty);

            InitialiseStats();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // =========================================================
    // HÀM LƯU / XÓA SESSION (GỌI TỪ AUTH MANAGER)
    // =========================================================
    public void SaveSession(string token, string userId)
    {
        AuthToken = token;
        UserId = userId;

        PlayerPrefs.SetString("AuthToken", token);
        PlayerPrefs.SetString("UserId", userId);
        PlayerPrefs.Save();

        // Ghi ra file Text
        string logPath = Application.dataPath + "/Scripts/Authen/session_log.txt";
        try
        {
            string content = $"[TIME: {System.DateTime.Now}]\nUSER ID: {userId}\nTOKEN: {token}\n-------------------";
            File.WriteAllText(logPath, content);
            Debug.Log($"[StatsManager] Đã ghi Session ra file tại: {logPath}");

#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[StatsManager] Lỗi ghi file text: {e.Message}");
        }
    }

    public void ClearSession()
    {
        AuthToken = string.Empty;
        UserId = string.Empty;

        PlayerPrefs.DeleteKey("AuthToken");
        PlayerPrefs.DeleteKey("UserId");
        PlayerPrefs.Save();

        string logPath = Application.dataPath + "/Scripts/Authen/session_log.txt";
        if (File.Exists(logPath))
        {
            File.WriteAllText(logPath, "Đã đăng xuất - Phiên làm việc kết thúc.");
        }
    }

    public bool HasToken()
    {
        return !string.IsNullOrEmpty(AuthToken);
    }
    // =========================================================

    private void InitialiseStats()
    {
        _currentHealth = _maxHealth;
        _currentMana = _maxMana;
        _currentStamina = _maxStamina;
    }

    public void ResetStats()
    {
        _currentHealth = _maxHealth;
        _currentMana = _maxMana;
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

    public bool SpendUpgradePoints(int amount)
    {
        if (_upgradePoints < amount) return false;
        _upgradePoints = Mathf.Max(0, _upgradePoints - amount);
        OnStatsChanged();
        return true;
    }

    // ── ITEM STATS BONUSES ────────────────────────────────────────────────
    public void ApplyItemBonus(ItemData item)
    {
        if (item == null || item.statBonuses.Count == 0)
            return;

        Debug.Log($"[StatsManager] ✅ Applying bonuses from: {item.itemName}");

        foreach (var bonus in item.statBonuses)
        {
            ApplyStatBonus(bonus.statName, bonus.value);
        }

        OnStatsChanged();
    }

    public void RemoveItemBonus(ItemData item)
    {
        if (item == null || item.statBonuses.Count == 0)
            return;

        Debug.Log($"[StatsManager] ❌ Removing bonuses from: {item.itemName}");

        foreach (var bonus in item.statBonuses)
        {
            ApplyStatBonus(bonus.statName, -bonus.value);
        }

        OnStatsChanged();
    }

    private void ApplyStatBonus(string statName, float bonus)
    {
        if (bonus == 0) return;

        switch (statName.ToLower())
        {
            case "health":
            case "maxhealth":
                maxHealth += bonus;
                Debug.Log($"  📊 MaxHealth: +{bonus} → {maxHealth}");
                break;

            case "mana":
            case "maxmana":
                maxMana += bonus;
                Debug.Log($"  📊 MaxMana: +{bonus} → {maxMana}");
                break;

            case "stamina":
            case "maxstamina":
                maxStamina += bonus;
                Debug.Log($"  📊 MaxStamina: +{bonus} → {maxStamina}");
                break;

            case "damage":
                damage += (int)bonus;
                Debug.Log($"  📊 Damage: +{bonus} → {damage}");
                break;

            case "movespeed":
            case "speed":
                moveSpeed += bonus;
                Debug.Log($"  📊 MoveSpeed: +{bonus} → {moveSpeed}");
                break;

            case "range":
            case "weaponrange":
                weaponRange += bonus;
                Debug.Log($"  📊 WeaponRange: +{bonus} → {weaponRange}");
                break;

            case "cooldown":
                cooldown -= bonus;
                cooldown = Mathf.Max(0.1f, cooldown);
                Debug.Log($"  📊 Cooldown: -{bonus} → {cooldown}s");
                break;

            case "knockback":
            case "knockbackforce":
                knockbackForce += bonus;
                Debug.Log($"  📊 KnockbackForce: +{bonus} → {knockbackForce}");
                break;

            case "regenerate":
            case "regen":
            case "staminaregenrate":
                staminaRegenRate += bonus;
                Debug.Log($"  📊 StaminaRegenRate: +{bonus} → {staminaRegenRate}");
                break;

            case "defence":
            case "defense":
            case "armor":
                defence += bonus;
                Debug.Log($"  📊 Defence: +{bonus} → {defence}");
                break;

            case "magicresist":
            case "magic_resist":
            case "magicresistance":
                magicResist += bonus;
                Debug.Log($"  📊 MagicResist: +{bonus} → {magicResist}");
                break;

            default:
                Debug.LogWarning($"[StatsManager] ⚠️ Unknown stat: {statName}");
                break;
        }
    }
    private void OnStatsChanged()
    {
        OnStatsChangedEvent?.Invoke();
    }

    private void HandleDeath()
    {
        Debug.Log("[StatsManager] Player died!");
        OnPlayerDeathEvent?.Invoke();
        Destroy(gameObject);
        Time.timeScale = 0f;
    }

    private int CalculateExpToNextLevel(int nextLevel) =>
    Mathf.RoundToInt(100 * Mathf.Pow(nextLevel, 1.5f));
}