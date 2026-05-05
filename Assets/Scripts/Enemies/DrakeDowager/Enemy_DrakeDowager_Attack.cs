using UnityEngine;

[DisallowMultipleComponent]
public class Enemy_DrakeDowager_Attack : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Animator animator;
    [SerializeField] private Enemy_DrakeDowager_Health health;
    [SerializeField] private Enemy_DrakeDowager_Movement movement;

    [Header("Attack Settings")]
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private Transform player;
    [SerializeField] private GameObject chainLightning;
    [SerializeField] private Transform chainLightningSpawnPoint;

    [Header("Audio")]
    [SerializeField] private AudioClip shootAudio;
    [SerializeField] private float volume = 1f;

    private void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (health == null)
            health = GetComponent<Enemy_DrakeDowager_Health>();

        if (movement == null)
            movement = GetComponent<Enemy_DrakeDowager_Movement>();
    }

    public void NormalAttack()
    {
        if (player == null)
        {
            if (movement.PlayerTransform != null)
                player = movement.PlayerTransform;
            else
            {
                var hits = Physics2D.OverlapCircleAll(movement.DetectionPoint.position, health.stats.AttackRange, playerLayer);

                foreach (var hit in hits)
                {
                    if (!hit.CompareTag("Player") && !hit.CompareTag("NPC")) continue;

                    player = hit.transform;
                    break;
                }
            }
        }
        Vector2 direction = (player.position - chainLightningSpawnPoint.position).normalized;

        var spell = Instantiate(chainLightning, chainLightningSpawnPoint.position, Quaternion.identity);
        spell.SetActive(false);

        var spellComponent = spell.GetComponent<Enemy_DrakeDowager_ChainLightning>();
        if (spellComponent != null && health != null)
        {
            spellComponent.Initialize(health.stats, direction);
        }

        spell.SetActive(true);
    }

    public void PlayAudio(int num)
    {
        switch (num)
        {
            case 0:
                SoundFXManager.Instance.PlaySoundFXClip(shootAudio, transform, volume);
                break;
            default:
                Debug.LogWarning("Invalid audio number passed to PlayAudio: " + num);
                break;
        }
    }
}
