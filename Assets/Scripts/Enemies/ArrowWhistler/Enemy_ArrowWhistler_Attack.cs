using UnityEngine;

public class Enemy_ArrowWhistler_Attack : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Animator animator;
    [SerializeField] private Enemy_ArrowWhistler_Health health;

    [Header("Attack Settings")]
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private Transform player;
    [SerializeField] private GameObject fireArrow;
    [SerializeField] private Transform arrowSpawnPoint;
    [SerializeField] private GameObject enragedAttackEffect;

    [Header("Audio")]
    [SerializeField] private AudioClip shootAudio;
    [SerializeField] private AudioClip fireAudio;
    [SerializeField] private AudioClip[] drawArrowAudio;
    [SerializeField] private AudioClip bowDraw;
    [SerializeField] private float volume = 1f;
    private bool isEnraged = false;

    private void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (health == null)
            health = GetComponent<Enemy_ArrowWhistler_Health>();
        health.OnEnraged += () => isEnraged = true;
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.OnEnraged -= () => isEnraged = true;
        }
    }

    public void NormalAttack()
    {
        var hits = Physics2D.OverlapCircleAll(transform.position, health.stats.AttackRange, playerLayer);
        if (hits.Length > 0)
        {
            foreach (var hit in hits)
            {
                if (hit.CompareTag("Player"))
                {
                    player = hit.transform;
                    break;
                }
            }
        }

        Vector2 direction = (player.position - arrowSpawnPoint.position).normalized;

        if (enragedAttackEffect != null && fireAudio != null && isEnraged)
        {
            SoundFXManager.Instance.PlaySoundFXClip(fireAudio, transform, volume);
        }

        GameObject arrow = Instantiate(fireArrow, arrowSpawnPoint.position, Quaternion.identity);

        var arrowComponent = arrow.GetComponent<Enemy_ArrowWhistler_Arrow>();
        if (arrowComponent != null && health != null)
        {
            arrowComponent.Initialize(health.stats, direction);
        }

        arrow.SetActive(true);
    }

    public void PlayEnrangedFire()
    {
        if (enragedAttackEffect != null && fireAudio != null && isEnraged)
        {
            var position = transform.position + new Vector3(0, -0.15f, 0);
            Instantiate(enragedAttackEffect, position, Quaternion.identity, transform);
            SoundFXManager.Instance.PlaySoundFXClip(fireAudio, transform, volume);
        }
    }

    public void PlayAudio(int num)
    {
        switch (num)
        {
            case 1:
                if (drawArrowAudio != null && drawArrowAudio.Length > 0)
                    SoundFXManager.Instance.PlayRandomSoundFXClips(drawArrowAudio, transform, volume);
                break;
            case 2:
                if (bowDraw != null)
                    SoundFXManager.Instance.PlaySoundFXClip(bowDraw, transform, volume * 1.5f);
                break;
            case 3:
                if (shootAudio != null)
                    SoundFXManager.Instance.PlaySoundFXClip(shootAudio, transform, volume);
                break;
        }
    }
}
