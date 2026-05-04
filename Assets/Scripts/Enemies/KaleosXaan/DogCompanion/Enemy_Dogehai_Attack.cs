using UnityEngine;
using System.Collections;

[DisallowMultipleComponent]
public class Enemy_Dogehai_Attack : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Animator animator;
    [SerializeField] private Enemy_Dogehai_Health health;

    [Header("General Attack Settings")]
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private Transform player;

    [Header("Normal Attack")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private Vector3 attackHitBox;
    [SerializeField] private GameObject hitEffect;

    [Header("Audio")]
    [SerializeField] private AudioClip normalAttackSwingAudioClip;
    [SerializeField] private AudioClip normalAttackHitAudioClip;
    [SerializeField] private float volume = 1f;

    private float hitEffectTimer = 0f;
    private void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
        if (health == null)
            health = GetComponent<Enemy_Dogehai_Health>();
    }
    private void Update()
    {
        if (hitEffectTimer > 0)
        {
            hitEffectTimer -= Time.deltaTime;
        }
    }
    public void NormalAttack()
    {
        var hits = Physics2D.OverlapBoxAll(attackPoint.position, attackHitBox, 0f, playerLayer);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player") || hit.CompareTag("NPC"))
            {
                player = hit.transform;

                if (hitEffectTimer <= 0)
                {
                    hitEffectTimer = 2f;
                    SoundFXManager.Instance.PlaySoundFXClip(normalAttackHitAudioClip, transform, volume);
                    Instantiate(hitEffect, player.position, Quaternion.identity, player.transform);
                    
                    float damage = health.stats.Strength;
                    if (player.CompareTag("Player"))
                    {
                        StatsManager.instance.TakeDamage(damage);
                    }
                    else if (player.CompareTag("NPC"))
                    {
                        if (player.TryGetComponent<NPCCompanion>(out var npc))
                        {
                            npc.TakeDamage(damage, false);
                        }
                    }
                }
            }
        }
    }

    public void PlayAudio()
    {
        if(normalAttackSwingAudioClip != null)
            SoundFXManager.Instance.PlaySoundFXClip(normalAttackSwingAudioClip, transform, volume * 0.2f);
    }
}