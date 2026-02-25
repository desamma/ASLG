using System.Collections;
using UnityEngine;

public class AH_WarSurge_MarkEffect : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private AH_WarSurge_Mark_State state;
    [SerializeField] private float destroyTime = 5f;
    [SerializeField] private Animator animator;
    private StateManager<AH_WarSurge_Mark_State> stateManager;

    [Header("Audio")]
    [SerializeField] private AudioClip chantSound;
    [SerializeField] private AudioClip swordClash;
    [SerializeField] private float volume = 1f;

    private void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        stateManager = new StateManager<AH_WarSurge_Mark_State>(animator, AH_WarSurge_Mark_State.Empty);

        StartCoroutine(AnimationDelay());
        if (destroyTime > 0f)
            Destroy(gameObject, destroyTime);
    }
    public void PlayChantSound()
    {
        if (chantSound != null)
            SoundFXManager.Instance.PlaySoundFXClip(chantSound, transform, volume);
    }

    public void PlaySwordClash()
    {
        if (swordClash != null)
            SoundFXManager.Instance.PlaySoundFXClip(swordClash, transform, volume);
    }

    private IEnumerator AnimationDelay()
    {
        var delayTime = GetDelayTime(state);
        yield return new WaitForSeconds(delayTime);
        stateManager.ChangeState(state);
    }

    private float GetDelayTime(AH_WarSurge_Mark_State state)
    {
        return state switch
        {
            AH_WarSurge_Mark_State.Empty => 0f,
            AH_WarSurge_Mark_State.WarSurge => 0f,
            AH_WarSurge_Mark_State.BladeBreaker => 0.7f,
            _ => 0f,
        };
    }
}
