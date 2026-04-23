using UnityEngine;

public class Enemy_ArgeonHighmayne_LionHeartBlessing : MonoBehaviour
{
    [Header("Spell Settings")]
    [SerializeField] private float magicDamage = 200f;
    [SerializeField] private float destroyTime = 5.5f;

    [Header("Entrance Effects")]
    [SerializeField] private GameObject argeonHighmayne;
    [SerializeField] private Collider2D spellCollider;
    [SerializeField] private bool isPhase2Offset = false;
    [Header("Audio")]
    [SerializeField] private AudioClip audioClip;
    [SerializeField] private float volume = 1f;
    private bool hitPlayer = false;
    private SpawnerEnemyDropHandler sourceDropHandler;

    private void Start()
    {

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
        if (isPhase2Offset)
        {
            float xOffset = transform.position.x < transform.position.x ? -0.1f : 0.1f;
            float yOffset = -1.5f;
            var spawnPosition = new Vector3(transform.position.x + xOffset, transform.position.y + yOffset, transform.position.z);

            var phase2Boss = Instantiate(argeonHighmayne, spawnPosition, Quaternion.identity);
            if (phase2Boss != null)
            {
                sourceDropHandler.TransferTo(phase2Boss);
            }
        }
        else
        {
            float xOffset = transform.position.x < transform.position.x ? -0.1f : 0.1f;
            float yOffset = -2f;
            var spawnPosition = new Vector3(transform.position.x + xOffset, transform.position.y + yOffset, transform.position.z);

            var phase2Boss = Instantiate(argeonHighmayne, spawnPosition, Quaternion.identity);
            if (phase2Boss != null)
            {
                sourceDropHandler.TransferTo(phase2Boss);
            }
        }
    }

    public void InitializeDropTransfer(SpawnerEnemyDropHandler dropHandler)
    {
        sourceDropHandler = dropHandler;
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
            StatsManager.instance.TakeDamage(magicDamage);
        }
    }
}
