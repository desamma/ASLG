using UnityEngine;

public class Enemy_KaleosXaan_DaemonicLure : MonoBehaviour
{
    [Header("General Settings")]
    [SerializeField] private float destroyTime = 5.5f;
    [SerializeField] private float magicMultiplier = 0.7f;

    [Header("Catch Settings")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private GameObject catchHitEffect;
    [SerializeField] private Vector3 attackHitBox;

    [Header("Audio")]
    [SerializeField] private AudioClip rotateLassoSound;
    [SerializeField] private float volume = 1f;

    private EnemyStats casterStats;
    public void Initialize(EnemyStats stats)
    {
        casterStats = stats;
    }

    private void Start()
    {
        if (destroyTime > 0f)
            Destroy(gameObject, destroyTime);
        SoundFXManager.Instance.PlaySoundFXClip(rotateLassoSound, transform, volume);
    }

    public void DaemonicLure()
    {
        if (attackPoint == null)
        {
            Debug.LogError("DaemonicLure: attackPoint is not assigned!");
            return;
        }

        var hitColliders = Physics2D.OverlapBoxAll(attackPoint.position, attackHitBox, 0f);
        if (hitColliders.Length > 0)
        {
            foreach (var hitCollider in hitColliders)
            {
                if (hitCollider.CompareTag("Player"))
                {
                    Instantiate(catchHitEffect, hitCollider.transform.position, Quaternion.identity, hitCollider.transform);
                    //TODO: Dealdamge & bind
                    break;
                }
            }
        }
    }
}

