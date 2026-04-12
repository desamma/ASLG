using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class Enemy_Coalfist_Attack : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Animator animator;
    [SerializeField] private Enemy_Coalfist_Health health;
    [SerializeField] private Enemy_Coalfist_Movement movement;

    [Header("General Attack Settings")]
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private Transform player;

    [Header("Normal Attack")]
    [SerializeField] private Transform normalAttackPoint;
    [SerializeField] private Vector2 normalAttackHitBox = new(1f, 2f);
    [SerializeField] private GameObject hitEffect;

    [Header("Audio")]
    [SerializeField] private AudioClip punchAudio;
    [SerializeField] private AudioClip hitAudio;
    [SerializeField] private float volume = 1f;

    private DifficultyModifier difficultyModifier;
    private bool hitPlayer = false;

    private void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (health == null)
            health = GetComponent<Enemy_Coalfist_Health>();

        if (movement == null)
            movement = GetComponent<Enemy_Coalfist_Movement>();

        if (playerLayer != LayerMask.GetMask("Player"))
            playerLayer = LayerMask.GetMask("Player");

        difficultyModifier = DifficultyManager.Instance.CurrentDifficulty;
    }

    public void NormalAttack()
    {
        hitPlayer = true;
        PlayAudio(0);
        var hits = Physics2D.OverlapBoxAll(normalAttackPoint.position, normalAttackHitBox, 0f, playerLayer);

        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Player")) continue;

            player = hit.transform;
            if (hitPlayer)
            {
                PlayAudio(1);
                hitPlayer = false;
            }

            DealDamage();
            hitEffect.SetActive(true);
        }
        hitPlayer = true;
    }

    private void DealDamage()
    {
        if (player == null) return;
        float damage = health.stats.Strength * difficultyModifier.Resolve(difficultyModifier.StrengthMultiplier);
        StatsManager.instance.TakeDamage(damage);
        if(player.TryGetComponent<PlayerMovement>(out var playerMovement))
        {
            playerMovement.KnockBack(transform, health.stats.KnockbackForce, health.stats.KnockbackTime, health.stats.StunTime);
        }
    }

    public void PlayAudio(int num, float volumeOverride = -1f)
    {
        switch (num)
        {
            case 0:
                SoundFXManager.Instance.PlaySoundFXClip(punchAudio, transform, volumeOverride > 0 ? volumeOverride : volume);
                break;
            case 1:
                SoundFXManager.Instance.PlaySoundFXClip(hitAudio, transform, volumeOverride > 0 ? volumeOverride : volume);
                break;
            default:
                Debug.LogWarning("Invalid audio number: " + num);
                break;
        }
    }
}
