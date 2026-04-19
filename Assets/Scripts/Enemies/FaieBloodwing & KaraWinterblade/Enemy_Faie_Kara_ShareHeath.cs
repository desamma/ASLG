using System;
using UnityEngine;

public class Enemy_Faie_Kara_ShareHealth : MonoBehaviour
{
    private static Enemy_Faie_Kara_ShareHealth _instance;

    [Header("Shared HP Pool")]
    public float maxHP;
    public float currentHP;

    [Header("Phase 2")]
    [SerializeField] private GameObject faiePhase2Prefab;
    [SerializeField] private GameObject karaPhase2Prefab;

    private Vector3 _faieSpawnPos;
    private Vector3 _karaSpawnPos;
    private bool _faiePosRegistered;
    private bool _karaPosRegistered;
    private int _expReward;
    private int _goldReward;
    private bool _goldRewardGiven;

    public bool IsDead { get; private set; }
    public bool IsPhase2 { get; private set; }

    public event Action OnDeath;
    private BossHealthUI bossHealthUI;

    public void InitHP(float sharedMaxHP)
    {
        maxHP = sharedMaxHP;
        currentHP = sharedMaxHP;
        IsDead = false;
        _goldRewardGiven = false;
        bossHealthUI = FindFirstObjectByType<BossHealthUI>(FindObjectsInactive.Include);

        if (bossHealthUI != null)
        {
            ColorUtility.TryParseHtmlString("#008CFF", out Color top);
            ColorUtility.TryParseHtmlString("#A2DAFF", out Color text);
            bossHealthUI.Initialize("FaieBloodwing & KaraWinterblade", maxHP, text, top, Color.white);
            bossHealthUI.Show();
        }
    }

    public void InitPhase2HP(float sharedMaxHP)
    {
        maxHP = sharedMaxHP;
        currentHP = sharedMaxHP;
        IsDead = false;
        IsPhase2 = true;
        _goldRewardGiven = false;
        if (bossHealthUI == null)
            bossHealthUI = FindFirstObjectByType<BossHealthUI>(FindObjectsInactive.Include);
        if (bossHealthUI != null)
        {
            ColorUtility.TryParseHtmlString("#C519EA", out Color topLeft);
            ColorUtility.TryParseHtmlString("#F63187", out Color topRight);
            ColorUtility.TryParseHtmlString("#00FEF0", out Color bottomLeft);
            ColorUtility.TryParseHtmlString("#FED9B5", out Color bottomRight);
            bossHealthUI.Initialize("FaieBloodwing & KaraWinterblade", maxHP, Color.white, topLeft, topRight, bottomLeft, bottomRight);
            bossHealthUI.Show();
        }
    }

    public static Enemy_Faie_Kara_ShareHealth GetOrCreate()
    {
        if (_instance != null) return _instance;

        _instance = FindObjectOfType<Enemy_Faie_Kara_ShareHealth>();
        if (_instance != null) return _instance;

        var go = new GameObject("Enemy_Faie_Kara_ShareHealth");
        _instance = go.AddComponent<Enemy_Faie_Kara_ShareHealth>();
        return _instance;
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

    public void ChangeHealth(float amount)
    {
        if (IsDead) return;

        currentHP = Mathf.Clamp(currentHP + amount, 0, maxHP);

        bossHealthUI.UpdateHealth(currentHP);

        CheckPhaseTransition();

        if (currentHP <= 0)
        {
            IsDead = true;
            if (!_goldRewardGiven)
            {
                _goldRewardGiven = true;
                StatsManager.instance?.AddExp(_expReward);
                StatsManager.instance?.AddGold(_goldReward);
            }
            OnDeath?.Invoke();
            bossHealthUI.Hide();
        }
    }

    public void SetExpReward(float expReward)
    {
        _expReward = Mathf.Max(_expReward, Mathf.RoundToInt(expReward));
    }

    public void SetGoldReward(float goldReward)
    {
        _goldReward = Mathf.Max(_goldReward, Mathf.RoundToInt(goldReward));
    }

    private void CheckPhaseTransition()
    {
        if (IsPhase2) return;

        if (currentHP <= 0)
        {
            IsPhase2 = true;
            OnDeath?.Invoke();
        }
    }

    public void RegisterPosition(bool isFaie, Vector3 position, GameObject phase2GO)
    {
        if (phase2GO == null) return;

        if (isFaie)
        {
            _faieSpawnPos = position;
            _faiePosRegistered = true;
            faiePhase2Prefab = phase2GO;
        }
        else
        {
            _karaSpawnPos = position;
            _karaPosRegistered = true;
            karaPhase2Prefab = phase2GO;
        }
    }

    public void SpawnPhase2()
    {
        if (!_faiePosRegistered || !_karaPosRegistered)
            return;

        _faiePosRegistered = false;
        _karaPosRegistered = false;

        if (faiePhase2Prefab != null)
            Instantiate(faiePhase2Prefab, _faieSpawnPos, Quaternion.identity);

        if (karaPhase2Prefab != null)
            Instantiate(karaPhase2Prefab, _karaSpawnPos, Quaternion.identity);
    }

    public void DestroySharedHealth() => Destroy(gameObject);
}