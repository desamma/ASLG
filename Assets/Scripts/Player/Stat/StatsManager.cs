using UnityEngine;
using System.IO;
using System.Collections.Generic;

// ĐÃ XÓA DÒNG KHAI BÁO enum StatType Ở ĐÂY ĐỂ TRÁNH TRÙNG LẶP VỚI FILE StatType.cs CỦA BẠN

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
    [SerializeField] private float _staminaRegenRate = 1f;
    [SerializeField] private float _staminaCost = 25f;
    [SerializeField] private float _dashDelay = 0.5f;
    [SerializeField] private float _dashDuration = 0.35f;

    public float moveSpeed { get => _moveSpeed; set { _moveSpeed = value; OnStatsChanged(); } }
    public float maxStamina { get => _maxStamina; set { _maxStamina = Mathf.Max(0f, value); OnStatsChanged(); } }
    public float staminaRegenRate { get => _staminaRegenRate; set { _staminaRegenRate = value; OnStatsChanged(); } }
    public float staminaCost { get => _staminaCost; set { _staminaCost = value; OnStatsChanged(); } }
    public float dashDelay { get => _dashDelay; set { _dashDelay = value; OnStatsChanged(); } }
    public float dashDuration { get => _dashDuration; set { _dashDuration = value; OnStatsChanged(); } }

    [SerializeField] private float _currentStamina;
    public float currentStamina
    {
        get => _currentStamina;
        set { _currentStamina = Mathf.Clamp(value, 0f, maxStamina); OnStatsChanged(); }
    }

    [Header("Combat")]
    [SerializeField] private float _damage = 10;
    [SerializeField] private float _weaponRange = 1.5f;
    [SerializeField] private float _knockbackForce = 5f;
    [SerializeField] private float _knockbackTime = 0.2f;
    [SerializeField] private float _stunTime = 0.3f;
    [SerializeField] private float _cooldown = 0.5f;

    public float damage { get => _damage; set { _damage = value; OnStatsChanged(); } }
    public float weaponRange { get => _weaponRange; set { _weaponRange = value; OnStatsChanged(); } }
    public float knockbackForce { get => _knockbackForce; set { _knockbackForce = value; OnStatsChanged(); } }
    public float knockbackTime { get => _knockbackTime; set { _knockbackTime = value; OnStatsChanged(); } }
    public float stunTime { get => _stunTime; set { _stunTime = value; OnStatsChanged(); } }
    public float cooldown { get => _cooldown; set { _cooldown = Mathf.Max(0.1f, value); OnStatsChanged(); } }

    [Header("Experience & Levelling")]
    [SerializeField] private int _level = 1;
    [SerializeField] private int _expToNextLevel = 100;
    [SerializeField] private int _currentExp = 0;
    [SerializeField] private int _gold = 0;
    [SerializeField] private int _upgradePoints = 0;

    public int level { get => _level; private set { _level = Mathf.Max(1, value); OnStatsChanged(); } }
    public int expToNextLevel { get => _expToNextLevel; private set { _expToNextLevel = Mathf.Max(1, value); OnStatsChanged(); } }
    public int currentExp { get => _currentExp; private set { _currentExp = Mathf.Max(0, value); OnStatsChanged(); } }
    public int gold { get => _gold; private set { _gold = Mathf.Max(0, value); OnStatsChanged(); } }
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
    // HÀM LƯU / XÓA SESSION
    // =========================================================
    public void SaveSession(string token, string userId)
    {
        AuthToken = token;
        UserId = userId;

        PlayerPrefs.SetString("AuthToken", token);
        PlayerPrefs.SetString("UserId", userId);
        PlayerPrefs.Save();

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
        catch (System.Exception e) { Debug.LogError($"[StatsManager] Lỗi ghi file text: {e.Message}"); }
    }

    public void ClearSession()
    {
        AuthToken = string.Empty;
        UserId = string.Empty;

        PlayerPrefs.DeleteKey("AuthToken");
        PlayerPrefs.DeleteKey("UserId");
        PlayerPrefs.Save();

        string logPath = Application.dataPath + "/Scripts/Authen/session_log.txt";
        if (File.Exists(logPath)) File.WriteAllText(logPath, "Đã đăng xuất - Phiên làm việc kết thúc.");
    }

    public bool HasToken() => !string.IsNullOrEmpty(AuthToken);

    // ==========================================
    // DÀNH CHO SAVE MANAGER NẠP DỮ LIỆU
    // ==========================================
    public void LoadSavedStats(int savedLevel, int savedExp, int savedPts, float hp, float mana, float stam, string pName)
    {
        _level = savedLevel;
        _currentExp = savedExp;
        _upgradePoints = savedPts;
        _expToNextLevel = CalculateExpToNextLevel(savedLevel);
        
        _currentHealth = hp;
        _currentMana = mana;
        _currentStamina = stam;
        _playerName = pName;
        
        OnStatsChanged();
    }

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
        float damageAfterDefend = amount - defence;
        currentHealth -= Mathf.Abs(damageAfterDefend);
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

    public void AddGold(int amount)
    {
        gold += amount;
        OnStatsChanged();
    }

    public bool SpendUpgradePoints(int amount)
    {
        if (_upgradePoints < amount) return false;
        _upgradePoints = Mathf.Max(0, _upgradePoints - amount);
        OnStatsChanged();
        return true;
    }

    public void TryUpgradeStat(StatType statType, int amount)
    {
        if (SpendUpgradePoints(amount))
        {
            switch (statType)
            {
                case StatType.MaxHealth: maxHealth += Mathf.Ceil(maxHealth * 0.15f); break;
                case StatType.MaxMana: maxMana += Mathf.Ceil(maxMana * 0.15f); break;
                case StatType.MaxStamina: maxStamina += maxStamina * 0.15f; break;
                case StatType.Damage: damage += Mathf.Ceil(damage * 0.15f); break;
                case StatType.Defence: defence += Mathf.Ceil(defence * 0.15f); break;
                case StatType.MoveSpeed: moveSpeed += moveSpeed * 0.01f; break;
            }

            OnStatsChanged();
        }
    }

    // ── ITEM STATS BONUSES (HỆ THỐNG JSON MỚI) ───────────────────────────────────
    public void ApplyItemBonus(string itemID)
    {
        ItemDefinition def = ItemDatabase.GetItem(itemID);
        if (def == null || def.statBonuses.Count == 0) return;

        Debug.Log($"[StatsManager] ✅ Đang cộng chỉ số trang bị: {def.name}");
        foreach (var bonus in def.statBonuses)
        {
            ApplyStatBonus(bonus.Key, bonus.Value);
        }
        OnStatsChanged();
    }

    public void RemoveItemBonus(string itemID)
    {
        ItemDefinition def = ItemDatabase.GetItem(itemID);
        if (def == null || def.statBonuses.Count == 0) return;

        Debug.Log($"[StatsManager] ❌ Đang gỡ bỏ chỉ số trang bị: {def.name}");
        foreach (var bonus in def.statBonuses)
        {
            ApplyStatBonus(bonus.Key, -bonus.Value);
        }
        OnStatsChanged();
    }

    public void ApplyStatBonus(string statName, float bonus)
    {
        if (bonus == 0) return;

        switch (statName.ToLower())
        {
            // CÁC CHỈ SỐ GỐC (Dành cho Trang bị)
            case "maxhealth": maxHealth += bonus; break;
            case "maxmana": maxMana += bonus; break;
            case "maxstamina": maxStamina += bonus; break;
            case "damage": damage += bonus; break;
            case "movespeed": case "speed": moveSpeed += bonus; break;
            case "range": case "weaponrange": weaponRange += bonus; break;
            case "knockback": case "knockbackforce": knockbackForce += bonus; break;
            case "regenerate": case "regen": staminaRegenRate += bonus; break;
            case "defence": case "defense": case "armor": defence += bonus; break;
            case "magicresist": magicResist += bonus; break;
            
            case "cooldown": 
                cooldown -= bonus; // Bonus số dương sẽ GIẢM cooldown
                cooldown = Mathf.Max(0.1f, cooldown); 
                break;

            // CÁC CHỈ SỐ HIỆN TẠI (Dành cho Bình Máu/Bình Mana)
            case "currenthealth": case "hp": currentHealth += bonus; break;
            case "currentmana": case "mp": currentMana += bonus; break;
            case "currentstamina": case "sp": currentStamina += bonus; break;
            
            // ĐIỂM KINH NGHIỆM
            case "currentexp": case "exp": AddExp(Mathf.RoundToInt(bonus)); break;
            case "upgradepoints": _upgradePoints += Mathf.RoundToInt(bonus); break;

            default:
                Debug.LogWarning($"[StatsManager] ⚠️ Không nhận diện được chỉ số: {statName}");
                break;
        }
        OnStatsChanged();
    }

    private void OnStatsChanged() => OnStatsChangedEvent?.Invoke();

    private void HandleDeath()
    {
        Debug.Log("[StatsManager] Player died!");
        OnPlayerDeathEvent?.Invoke();
        Destroy(gameObject);
        Time.timeScale = 0f;
    }

    private int CalculateExpToNextLevel(int nextLevel) => Mathf.RoundToInt(100 * Mathf.Pow(nextLevel, 1.5f));
}