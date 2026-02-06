using UnityEngine;

public class Enemy_ArgeonHighmayne_LionHeartBlessing : MonoBehaviour
{
    [Header("Spell Settings")]
    [SerializeField] private float magicDamage = 200f;
    [SerializeField] private float destroyTime = 5.5f;

    [Header("Entrance Effects")]
    [SerializeField] private GameObject argeonHighmayne;
    [SerializeField] private Collider2D spellCollider;

    [Header("Audio")]
    [SerializeField] private AudioClip audioClip;
    [SerializeField] private float volume = 1f;
    private bool hitPlayer = false;
    private void Start()
    {
        if (argeonHighmayne == null)
        {
            Debug.LogError("Enemy Prefabs is not assigned!");
        }

        var difficultyModifier = DifficultyManager.Instance.CurrentDifficulty;
        magicDamage *= difficultyModifier.Resolve(difficultyModifier.MagicMultiplier);

        if (spellCollider == null)
            spellCollider = GetComponent<Collider2D>();

        if (audioClip != null)
            SoundFXManager.Instance.PlaySoundFXClip(audioClip, transform, volume);

        Destroy(gameObject, destroyTime);
    }
    public void EntranceSpawn()
    {
        float xOffset = transform.position.x < transform.position.x ? -0.1f : 0.1f;
        float yOffset = -2f;
        var spawnPosition = new Vector3(transform.position.x + xOffset, transform.position.y + yOffset, transform.position.z);

        Instantiate(argeonHighmayne, spawnPosition, Quaternion.identity);
    }

    public void EnableTrigger()
    {
        if (spellCollider != null)
        {
            spellCollider.isTrigger = true;
            spellCollider.enabled = true;
        }

        Destroy(gameObject, destroyTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision == null)
            return;

        if (collision.CompareTag("Player") && !hitPlayer)
        {
            hitPlayer = true;
            //var playerHealth = collision.GetComponent<PlayerHealth>();
            //if (playerHealth != null)
            //{
            //    playerHealth.ChangeHealth(-magicDamage);
            //}
        }
    }
}
