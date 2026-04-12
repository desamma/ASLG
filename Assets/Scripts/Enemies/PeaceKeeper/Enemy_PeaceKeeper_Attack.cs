using UnityEngine;

[DisallowMultipleComponent]
public class Enemy_PeaceKeeper_Attack : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Animator animator;
    [SerializeField] private Enemy_PeaceKeeper_Health health;

    [Header("General Attack Settings")]
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private Transform player;

    [Header("Normal Attack")]
    [SerializeField] private Transform normalAttackPoint;
    [SerializeField] private Vector2 normalAttackHitBox = new(2f, 3f);
    [SerializeField] private GameObject hitEffect;

    [Header("Audio")]
    [SerializeField] private AudioClip shieldBashAudioClip;
    [SerializeField] private float volume = 1f;

    private DifficultyModifier difficultyModifier;
    private bool hitPlayer = false;

    private void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (health == null)
            health = GetComponent<Enemy_PeaceKeeper_Health>();

        if (playerLayer != LayerMask.GetMask("Player"))
            playerLayer = LayerMask.GetMask("Player");

        difficultyModifier = DifficultyManager.Instance.CurrentDifficulty;
    }

    public void NormalAttack()
    {
        var hits = Physics2D.OverlapBoxAll(normalAttackPoint.position, normalAttackHitBox, playerLayer);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                player = hit.transform;
                if (!hitPlayer)
                {
                    PlayAudio(0, volume * 0.4f);
                    hitPlayer = true;
                }
                DealDamage(true);
            }
        }
        hitPlayer = false;
    }

    private void DealDamage(bool isMagic, float damageMultiplier = 1f)
    {
        if (isMagic)
        {
            StatsManager.instance.TakeDamage(health.stats.Magic * damageMultiplier * difficultyModifier.Resolve(difficultyModifier.MagicMultiplier));
        }
        else
        {
            StatsManager.instance.TakeDamage(health.stats.Strength * damageMultiplier * difficultyModifier.Resolve(difficultyModifier.StrengthMultiplier));
        }

        player.TryGetComponent<PlayerMovement>(out var playerMovement);

        if (playerMovement != null)
        {
            playerMovement.KnockBack(transform, health.stats.KnockbackForce, health.stats.KnockbackTime, health.stats.StunTime);
        }

        hitEffect.SetActive(true);
    }

    public void PlayAudio(int num, float volumeOverride = -1f)
    {
        switch (num)
        {
            case 0:
                SoundFXManager.Instance.PlaySoundFXClip(shieldBashAudioClip, transform, volumeOverride > 0 ? volumeOverride : volume);
                break;
            default:
                Debug.LogWarning("Invalid audio number: " + num);
                break;
        }
    }
}