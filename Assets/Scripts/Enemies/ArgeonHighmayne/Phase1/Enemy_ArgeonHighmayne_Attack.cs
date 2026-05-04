using Unity.VisualScripting;
using UnityEngine;

[DisallowMultipleComponent]
public class Enemy_ArgeonHighmayne_Attack : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Animator animator;
    [SerializeField] private Enemy_ArgeonHighmayne_Health health;

    [Header("Attack Settings")]
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private Transform player;

    [Header("NormalAttack")]
    [SerializeField] private GameObject normalAttackHitEffect;
    [SerializeField] private Transform normalAttackPoint;
    [SerializeField] private float normalAttackRadius = 1f;

    [Header("WarSurge")]
    [SerializeField] private Transform warSurgeAttackPoint;
    [SerializeField] private float warSurgeDamageMultiplier = 1.2f;
    [SerializeField] private float warSurgeRadius = 1f;

    [Header("AurynNexus")]
    [SerializeField] private GameObject aurynNexusEffect;
    [SerializeField] private StatusEffect aurynBuffEffect;

    [Header("SunBloom")]
    [SerializeField] private GameObject sunBloomEffect;
    [SerializeField] private StatusEffect damageIncrease;
    [SerializeField] private StatusEffect magicIncrease;

    [Header("Decimate")]
    [SerializeField] private GameObject decimateEffect;
    [SerializeField] private GameObject decimateChargeUpEffect;

    [Header("Audio")]
    [SerializeField] private AudioClip normalAttackSwingAudioClip;
    [SerializeField] private AudioClip normalAttackHitAudioClip;
    [SerializeField] private AudioClip decimateAudio1;
    [SerializeField] private AudioClip decimateAudio3;
    [SerializeField] private AudioClip aurynNexusAudioClip;
    [SerializeField] private float volume = 1f;

    private bool hitPlayer = false;
    private StatusEffectManager effectManager;
    private void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (effectManager == null)
            effectManager = GetComponent<StatusEffectManager>();

        if (health == null)
            health = GetComponent<Enemy_ArgeonHighmayne_Health>();
    }

    public void NormalAttack()
    {
        var hits = Physics2D.OverlapCircleAll(normalAttackPoint.position, normalAttackRadius, playerLayer);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player") || hit.CompareTag("NPC"))
            {
                player = hit.transform;
                if (!hitPlayer)
                {
                    SoundFXManager.Instance.PlaySoundFXClip(normalAttackHitAudioClip, transform, volume);
                    hitPlayer = true;
                }

                Instantiate(normalAttackHitEffect, player.position, Quaternion.identity, player.transform);
                DealDamage(false, 1f, true);
            }
        }
        hitPlayer = false;
    }

    public void WarSurgeAttack()
    {
        var hits = Physics2D.OverlapCircleAll(warSurgeAttackPoint.position, warSurgeRadius, playerLayer);

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player") || hit.CompareTag("NPC"))
            {
                player = hit.transform;
                if (!hitPlayer)
                {
                    SoundFXManager.Instance.PlaySoundFXClip(normalAttackHitAudioClip, transform, volume);
                    hitPlayer = true;
                }
                Instantiate(normalAttackHitEffect, player.position, Quaternion.identity, player.transform);
                DealDamage(false, warSurgeDamageMultiplier, true);
            }
        }
    }

    public void AurynNexus()
    {
        SoundFXManager.Instance.PlaySoundFXClip(aurynNexusAudioClip, transform, volume);

        GameObject spellInstance = Instantiate(aurynNexusEffect, transform.position, Quaternion.identity);

        var aurynNexusComponent = spellInstance.GetComponent<Enemy_ArgeonHighmayne_AurynNexus>();
        if (aurynNexusComponent != null && health != null)
        {
            aurynNexusComponent.Initialize(health.stats, health);
        }

        effectManager.ApplyEffect(aurynBuffEffect);
        spellInstance.SetActive(true);
    }

    public void SunBloom()
    {
        GameObject spellInstance = Instantiate(sunBloomEffect, transform.position, Quaternion.identity);

        var sunBloomComponent = spellInstance.GetComponent<Enemy_ArgeonHighmayne_SunBloom>();
        if (sunBloomComponent != null && health != null)
        {
            sunBloomComponent.Initialize(health.stats, health);
        }

        effectManager.ApplyEffect(damageIncrease, false, 15f)
            .WithStatLines(new string[] { "+ 20% Strength" });

        effectManager.ApplyEffect(magicIncrease, false, 15f)
            .WithStatLines(new string[] { "+ 20% Magic" });

        spellInstance.SetActive(true);
    }
    public void Decimate()
    {
        if (TryGetComponent<Enemy_ArgeonHighmayne_Movement>(out var movement))
        {
            player = movement.PlayerTransform;
        }
        
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        if (player != null)
        {
            GameObject spellInstance = Instantiate(decimateEffect, player.position, Quaternion.identity);

            var decimateComponent = spellInstance.GetComponent<Enemy_ArgeonHighmayne_Decimate>();
            if (decimateComponent != null && health != null)
            {
                decimateComponent.Initialize(health.stats);
            }
            spellInstance.SetActive(true);
        }
    }

    public void PlayDecimateChargeUp()
    {
        if (decimateChargeUpEffect == null)
        {
            Debug.LogWarning("Decimate Charge Up Effect is not assigned!");
            return;
        }
        float offsetY = -1.2f;
        Instantiate(decimateChargeUpEffect, transform.position + new Vector3(0, offsetY, 0), Quaternion.identity);
    }

    private void DealDamage(bool isMagic = false, float damageMultiplier = 1f, bool isKnockback = true)
    {
        if (player == null) return;
        
        float damage = isMagic ? health.stats.Magic * damageMultiplier : health.stats.Strength * damageMultiplier;

        if (player.CompareTag("Player"))
        {
            StatsManager.instance.TakeDamage(damage);
            if (isKnockback && player.TryGetComponent<PlayerMovement>(out var playerMovement))
            {
                playerMovement.KnockBack(transform, health.stats.KnockbackForce, health.stats.KnockbackTime, health.stats.StunTime);
            }
        }
        else if (player.CompareTag("NPC"))
        {
            if (player.TryGetComponent<NPCCompanion>(out var npc))
            {
                npc.TakeDamage(damage, false);
            }
        }
    }

    public void PlayAudio(int num)
    {
        switch (num)
        {
            case 1:
                if (decimateAudio1 != null)
                    SoundFXManager.Instance.PlaySoundFXClip(decimateAudio1, transform, volume);
                break;
            case 2:
                if (decimateAudio3 != null)
                    SoundFXManager.Instance.PlaySoundFXClip(decimateAudio3, transform, volume);
                break;
            case 3:
                if (normalAttackSwingAudioClip != null)
                    SoundFXManager.Instance.PlaySoundFXClip(normalAttackSwingAudioClip, transform, volume);
                break;
            case 4:
                if (aurynNexusAudioClip != null)
                    SoundFXManager.Instance.PlaySoundFXClip(aurynNexusAudioClip, transform, volume);
                break;
            default:
                Debug.LogWarning("Invalid audio number: " + num);
                break;
        }
    }
}