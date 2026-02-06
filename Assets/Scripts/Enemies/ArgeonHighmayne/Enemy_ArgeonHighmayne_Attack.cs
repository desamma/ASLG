using UnityEditor.Timeline;
using UnityEngine;

public class Enemy_ArgeonHighmayne_Attack : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Animator animator;
    [SerializeField] private Enemy_ArgeonHighmayne_Health health;

    [Header("Attack Settings")]
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private Transform player;
    private float castRange;

    [Header("NormalAttack")]
    [SerializeField] private GameObject normalAttackHitEffect;
    [SerializeField] private Transform normalAttackPoint;
    [SerializeField] private float normalAttackRadius = 1f;

    [Header("WarSurge")]
    [SerializeField] private GameObject warSurgeEffect;
    [SerializeField] private Transform warSurgeAttackPoint;
    [SerializeField] private float warSurgeRadius = 1f;

    [Header("AurynNexus")]
    [SerializeField] private GameObject aurynNexusEffect;

    [Header("SunBloom")]
    [SerializeField] private GameObject sunBloomEffect;

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

    private void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (health == null)
            health = GetComponent<Enemy_ArgeonHighmayne_Health>();
        castRange = health.stats.AttackRange * 3f;
    }

    public void NormalAttack()
    {
        var hits = Physics2D.OverlapCircleAll(normalAttackPoint.position, normalAttackRadius, playerLayer);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                player = hit.transform;
                if (!hitPlayer)
                {
                    SoundFXManager.Instance.PlaySoundFXClip(normalAttackHitAudioClip, transform, volume);
                    hitPlayer = true;
                }

                Instantiate(normalAttackHitEffect, player.position, Quaternion.identity, player.transform);
                //TODO: Deal Damage to Player
            }
        }
        hitPlayer = false;
    }
    public void PlayNormalAttackSwingAudio()
    {
        SoundFXManager.Instance.PlaySoundFXClip(normalAttackSwingAudioClip, transform, volume);
    }

    public void WarSurge()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        if (player != null)
        {
            var hits = Physics2D.OverlapCircleAll(warSurgeAttackPoint.position, warSurgeRadius, playerLayer);

            if (hits.Length > 0)
            {
                //TODO: Deal Damage to Player
                return;
            }

            // Instantiate as inactive
            GameObject spellInstance = Instantiate(warSurgeEffect, player.position, Quaternion.identity);

            var warSurgeComponent = spellInstance.GetComponent<Enemy_ArgeonHighmayne_WarSurge>();
            if (warSurgeComponent != null && health != null)
            {
                warSurgeComponent.Initialize(health.stats);
            }

            spellInstance.SetActive(true);
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

        spellInstance.SetActive(true);
    }
    public void Decimate()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        if (player != null)
        {
            var hits = Physics2D.OverlapCircleAll(warSurgeAttackPoint.position, warSurgeRadius, playerLayer);

            if (hits.Length > 0)
            {
                //TODO: Deal Damage to Player
                return;
            }
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

    public void PlayDecimateAudio(int num)
    {
        if (num == 3 && decimateAudio3 != null)
            SoundFXManager.Instance.PlaySoundFXClip(decimateAudio3, transform, volume);

        else if (num == 1 && decimateAudio1 != null)
            SoundFXManager.Instance.PlaySoundFXClip(decimateAudio1, transform, volume);
    }
}
