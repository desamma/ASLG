using UnityEngine;
using System.IO;
using System.Collections.Generic;

// ĐÃ XÓA DÒNG KHAI BÁO enum StatType Ở ĐÂY ĐỂ TRÁNH TRÙNG LẶP VỚI FILE StatType.cs CỦA BẠN

public class StatsManager : MonoBehaviour
{
    public static StatsManager instance { get; private set; }

    [Header("Player")]
    [SerializeField] private string _playerName;

    // --- BIẾN ĐIỀU PHỐI ĐỘ KHÓ CỦA AI DIRECTOR ---
    [Header("Difficulty Multipliers")]
    public float damageDealtMultiplier = 1f; // Nhân vào sát thương Player gây ra
    public float damageTakenMultiplier = 1f; // Nhân vào sát thương Player nhận vào

    [Header("Session Data")]
    [SerializeField] private string authToken;
    [SerializeField] private string userId;

    public string AuthToken { get => authToken; private set => authToken = value; }
    public string UserId { get => userId; private set => userId = value; }

    public string playerName { get => _playerName; set { _playerName = value; OnStatsChanged(); } }

    [Header("Health")]
    [SerializeField] private float _maxHealth = 2000f;
    [SerializeField] private float _baseMaxHealth = 2000f;
    [SerializeField] private float _bonusMaxHealth = 0f;
    public float maxHealth
    {
        get => _baseMaxHealth + _bonusMaxHealth;
        set { _baseMaxHealth = Mathf.Max(0f, value); OnStatsChanged(); }
    }
    public float baseMaxHealth
    {
        get => _baseMaxHealth;
        set { _baseMaxHealth = Mathf.Max(0f, value); OnStatsChanged(); }
    }

    [SerializeField] private float _currentHealth;
    public float currentHealth
    {
        get => _currentHealth;
        set { _currentHealth = Mathf.Clamp(value, 0f, maxHealth); OnStatsChanged(); }
    }

    [Header("Defence")]
    [SerializeField] private float _defence = 10f;
    [SerializeField] private float _baseDefence = 10f;
    [SerializeField] private float _bonusDefence = 0f;

    public float defence
    {
        get => _baseDefence + _bonusDefence;
        set { _baseDefence = Mathf.Max(0f, value); OnStatsChanged(); }
    }
    public float baseDefence
    {
        get => _baseDefence;
        set { _baseDefence = Mathf.Max(0f, value); OnStatsChanged(); }
    }

    [Header("Mana")]
    [SerializeField] private float _maxMana = 100f;
    [SerializeField] private float _baseMaxMana = 100f;
    [SerializeField] private float _bonusMaxMana = 0f;
    public float maxMana
    {
        get => _baseMaxMana + _bonusMaxMana;
        set { _baseMaxMana = Mathf.Max(0f, value); OnStatsChanged(); }
    }
    public float baseMaxMana
    {
        get => _baseMaxMana;
        set { _baseMaxMana = Mathf.Max(0f, value); OnStatsChanged(); }
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
    [SerializeField] private float _baseMoveSpeed = 5f;
    [SerializeField] private float _bonusMoveSpeed = 0f;
    [SerializeField] private float _maxStamina = 100f;
    [SerializeField] private float _baseMaxStamina = 100f;
    [SerializeField] private float _bonusMaxStamina = 0f;
    [SerializeField] private float _staminaRegenRate = 1f;
    [SerializeField] private float _bonusStaminaRegenRate = 0f;
    [SerializeField] private float _staminaCost = 25f;
    [SerializeField] private float _dashDelay = 0.5f;
    [SerializeField] private float _dashDuration = 0.35f;

    public float moveSpeed
    {
        get => _baseMoveSpeed + _bonusMoveSpeed;
        set { _baseMoveSpeed = Mathf.Max(0f, value); OnStatsChanged(); }
    }
    public float baseMoveSpeed
    {
        get => _baseMoveSpeed;
        set { _baseMoveSpeed = Mathf.Max(0f, value); OnStatsChanged(); }
    }
    public float maxStamina
    {
        get => _baseMaxStamina + _bonusMaxStamina;
        set { _baseMaxStamina = Mathf.Max(0f, value); OnStatsChanged(); }
    }
    public float baseMaxStamina
    {
        get => _baseMaxStamina;
        set { _baseMaxStamina = Mathf.Max(0f, value); OnStatsChanged(); }
    }
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
    [SerializeField] private float _damage = 10f;
    [SerializeField] private float _baseDamage = 10f;
    [SerializeField] private float _bonusDamage = 0f;
    [SerializeField] private float _weaponRange = 1.5f;
    [SerializeField] private float _bonusWeaponRange = 0f;
    [SerializeField] private float _knockbackForce = 5f;
    [SerializeField] private float _bonusKnockbackForce = 0f;
    [SerializeField] private float _knockbackTime = 0.2f;
    [SerializeField] private float _stunTime = 0.3f;
    [SerializeField] private float _baseCooldown = 0.5f;
    [SerializeField] private float _bonusCooldown = 0f;

    // ĐÃ SỬA: Sát thương đầu ra giờ sẽ được nhân thêm hệ số của AI Director
    public float damage
    {
        get => (_baseDamage + _bonusDamage) * damageDealtMultiplier;
        set { _baseDamage = Mathf.Max(0f, value); OnStatsChanged(); }
    }
    public float baseDamage
    {
        get => _baseDamage;
        set { _baseDamage = Mathf.Max(0f, value); OnStatsChanged(); }
    }
    public float weaponRange
    {
        get => _weaponRange + _bonusWeaponRange;
        set { _weaponRange = Mathf.Max(0f, value); OnStatsChanged(); }
    }
    public float knockbackForce
    {
        get => _knockbackForce + _bonusKnockbackForce;
        set { _knockbackForce = Mathf.Max(0f, value); OnStatsChanged(); }
    }
    public float knockbackTime { get => _knockbackTime; set { _knockbackTime = value; OnStatsChanged(); } }
    public float stunTime { get => _stunTime; set { _stunTime = value; OnStatsChanged(); } }
    public float cooldown
    {
        get => Mathf.Max(0.1f, _baseCooldown + _bonusCooldown);
        set { _baseCooldown = Mathf.Max(0.1f, value); OnStatsChanged(); }
    }
    public float baseCooldown
    {
        get => Mathf.Max(0.1f, _baseCooldown);
        set { _baseCooldown = Mathf.Max(0.1f, value); OnStatsChanged(); }
    }

    // ── PUBLIC BONUS STAT ACCESSORS ──
    public float bonusMaxHealth { get => _bonusMaxHealth; set { _bonusMaxHealth = Mathf.Max(0f, value); OnStatsChanged(); } }
    public float bonusMaxMana { get => _bonusMaxMana; set { _bonusMaxMana = Mathf.Max(0f, value); OnStatsChanged(); } }
    public float bonusMaxStamina { get => _bonusMaxStamina; set { _bonusMaxStamina = Mathf.Max(0f, value); OnStatsChanged(); } }
    public float bonusDamage { get => _bonusDamage; set { _bonusDamage = Mathf.Max(0f, value); OnStatsChanged(); } }
    public float bonusMoveSpeed { get => _bonusMoveSpeed; set { _bonusMoveSpeed = Mathf.Max(0f, value); OnStatsChanged(); } }
    public float bonusDefence { get => _bonusDefence; set { _bonusDefence = Mathf.Max(0f, value); OnStatsChanged(); } }
    public float bonusWeaponRange { get => _bonusWeaponRange; set { _bonusWeaponRange = Mathf.Max(0f, value); OnStatsChanged(); } }
    public float bonusCooldown { get => _bonusCooldown; set { _bonusCooldown = Mathf.Max(0f, value); OnStatsChanged(); } }
    public float bonusKnockbackForce { get => _bonusKnockbackForce; set { _bonusKnockbackForce = Mathf.Max(0f, value); OnStatsChanged(); } }
    public float bonusStaminaRegenRate { get => _bonusStaminaRegenRate; set { _bonusStaminaRegenRate = Mathf.Max(0f, value); OnStatsChanged(); } }

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

    [Header("Buffs")]
    public bool isInvincible = false; // Phục vụ cho Skill của Johnson

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            transform.SetParent(null); // Đảm bảo object là root để DontDestroyOnLoad hoạt động
            DontDestroyOnLoad(gameObject);

            AuthToken = PlayerPrefs.GetString("AuthToken", string.Empty);
            UserId = PlayerPrefs.GetString("UserId", string.Empty);

            ResetStats();
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
    public void LoadSavedStats(int savedLevel, int savedExp, int savedPts, int savedGold, float hp, float mana, float stam, string pName, float baseMaxHealth, float baseMaxMana, float baseMaxStamina, float baseDamage, float baseDefence, float baseCooldown, float baseMoveSpeed)
    {
        _level = savedLevel;
        _currentExp = savedExp;
        _upgradePoints = savedPts;
        _gold = savedGold;
        _expToNextLevel = CalculateExpToNextLevel(savedLevel);

        // Base Stats
        _baseMaxHealth = baseMaxHealth;
        _baseMaxMana = baseMaxMana;
        _baseMaxStamina = baseMaxStamina;
        _baseDamage = baseDamage;
        _baseDefence = baseDefence;
        _baseCooldown = baseCooldown;
        _baseMoveSpeed = baseMoveSpeed;
        _baseDefence = baseDefence;

        // Bonus Stats
        _bonusMaxHealth = bonusMaxHealth;
        _bonusMaxMana = bonusMaxMana;
        _bonusMaxStamina = bonusMaxStamina;
        _bonusDamage = bonusDamage;
        _bonusCooldown = bonusCooldown;
        _bonusMoveSpeed = bonusMoveSpeed;
        _bonusDefence = bonusDefence;
        _bonusWeaponRange = bonusWeaponRange;
        _bonusKnockbackForce = bonusKnockbackForce;
        _bonusStaminaRegenRate = bonusStaminaRegenRate;

        // Current Resources
        _currentHealth = maxHealth;
        _currentMana = maxMana;
        _currentStamina = maxStamina;
        _playerName = pName;

        OnStatsChanged();
    }

    public void ResetStats()
    {

        _bonusMaxHealth = 0f;
        _bonusMaxMana = 0f;
        _bonusMaxStamina = 0f;
        _bonusDamage = 0f;
        _bonusDefence = 0f;
        _bonusMoveSpeed = 0f;
        _bonusWeaponRange = 0f;
        _bonusCooldown = 0f;
        _bonusKnockbackForce = 0f;
        _bonusStaminaRegenRate = 0f;

        _currentHealth = maxHealth;
        _currentStamina = maxStamina;
        _currentMana = maxMana;
        
        // Reset luôn cả hệ số AI Director khi khởi tạo
        damageDealtMultiplier = 1f;
        damageTakenMultiplier = 1f;
        
        OnStatsChanged();
    }

    public void TakeDamage(float amount)
    {
        if (isInvincible) return; // Đang được Johnson buff vô địch, bỏ qua sát thương!

        // ĐÃ SỬA: Tính sát thương sau khi trừ giáp, sau đó nhân hệ số của AI Director
        float damageAfterDefend = (amount - defence) * damageTakenMultiplier;
        float actualDamage = Mathf.Max(0f, damageAfterDefend); // Đảm bảo sát thương >= 0 (không bị bơm máu ngược)
        
        // Báo cho AI biết Player vừa mất bao nhiêu máu
        if (AIDifficultyManager.Instance != null) 
            AIDifficultyManager.Instance.LogDamageTaken(actualDamage);

        currentHealth -= actualDamage;
        if (IsDead) HandleDeath();
    }

    public void Heal(float amount) => currentHealth += Mathf.Abs(amount);

    public void AddExp(int amount)
    {
        // ĐÃ SỬA: Cứ mỗi lần nhận EXP tức là quái đã chết, báo cho AI ghi nhận số Kill
        if (AIDifficultyManager.Instance != null) 
            AIDifficultyManager.Instance.LogKill();

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
        if (!SpendUpgradePoints(amount)) return;

        switch (statType)
        {
            case StatType.MaxHealth: _baseMaxHealth += Mathf.Ceil(_baseMaxHealth * 0.15f); break;
            case StatType.MaxMana: _baseMaxMana += Mathf.Ceil(_baseMaxMana * 0.15f); break;
            case StatType.MaxStamina: _baseMaxStamina += _baseMaxStamina * 0.15f; break;
            case StatType.Damage: _baseDamage += Mathf.Ceil(_baseDamage * 0.15f); break;
            case StatType.Defence: _baseDefence += Mathf.Ceil(_baseDefence * 0.15f); break;
            case StatType.MoveSpeed: _baseMoveSpeed += _baseMoveSpeed * 0.01f; break;
        }

        OnStatsChanged();
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
            case "maxhealth": _bonusMaxHealth += bonus; break;
            case "maxmana": _bonusMaxMana += bonus; break;
            case "maxstamina": _bonusMaxStamina += bonus; break;
            case "damage": _bonusDamage += bonus; break;
            case "movespeed": case "speed": _bonusMoveSpeed += bonus; break;
            case "range": case "weaponrange": _bonusWeaponRange += bonus; break;
            case "knockback": case "knockbackforce": _bonusKnockbackForce += bonus; break;
            case "regenerate": case "regen": _bonusStaminaRegenRate += bonus; break;
            case "defence": case "defense": case "armor": _bonusDefence += bonus; break;
            case "cooldown": 
                _bonusCooldown += bonus;
                break;

            // CÁC CHỈ SỐ HIỆN TẠI (Dành cho Bình Máu/Bình Mana)
            case "currenthealth": case "hp": case "health": currentHealth += bonus; break;
            case "currentmana": case "mp": case "mana": currentMana += bonus; break;
            case "currentstamina": case "sp": case"stamina": currentStamina += bonus; break;
            
            // ĐIỂM KINH NGHIỆM
            case "currentexp": case "exp": AddExp(Mathf.RoundToInt(bonus)); break;
            case "upgradepoints": _upgradePoints += Mathf.RoundToInt(bonus); break;

            default:
                Debug.LogWarning($"[StatsManager] ⚠️ Không nhận diện được chỉ số: {statName}");
                break;
        }
        OnStatsChanged();
    }

    private void OnStatsChanged()
    {
        _maxHealth = maxHealth;
        _defence = defence;
        _maxMana = maxMana;
        _moveSpeed = moveSpeed;
        _maxStamina = maxStamina;
        _damage = damage;
        _weaponRange = weaponRange;
        _knockbackForce = knockbackForce;
        _baseCooldown = cooldown;

        OnStatsChangedEvent?.Invoke();
    }

    private void HandleDeath()
    {
        Debug.Log("[StatsManager] Player died!");
        
        // ĐÃ SỬA: Báo cho AI biết Player vừa tèo
        if (AIDifficultyManager.Instance != null) 
            AIDifficultyManager.Instance.LogDeath();

        OnPlayerDeathEvent?.Invoke();
        Destroy(GameObject.FindGameObjectWithTag("Player"));
        //Time.timeScale = 0f;
    }

    private int CalculateExpToNextLevel(int nextLevel) => Mathf.RoundToInt(100 * Mathf.Pow(nextLevel, 1.5f));
}