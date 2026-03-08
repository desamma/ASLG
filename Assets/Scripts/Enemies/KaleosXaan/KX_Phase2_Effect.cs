using System.Collections;
using UnityEngine;

public class KX_Phase2_Effect : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float destroyTime;
    [SerializeField] private float moveSprite;
    [SerializeField] private bool isMoving = true;
    [SerializeField] private GameObject phase2Kaleos;

    [Header("Effect Settings")]
    [SerializeField] private Animator animator;
    [SerializeField] private KX_Phase2Effect_State state;
    private StateManager<KX_Phase2Effect_State> stateManager;

    [Header("Audio")]
    [SerializeField] private AudioClip phase2Sound;
    [SerializeField] private AudioClip explosionAudio;
    [SerializeField] private float volume = 1f;


    private void Start()
    {
        stateManager = new StateManager<KX_Phase2Effect_State>(animator, KX_Phase2Effect_State.Empty);

        StartCoroutine(HandleEffectSequence());
        if(destroyTime > 0f)
            Destroy(gameObject, destroyTime);
    }

    private IEnumerator HandleEffectSequence()
    {
        var delayTime = GetDelayTime(state);
        
        if (delayTime > 0f)
        {
            yield return new WaitForSeconds(delayTime);
            stateManager.ChangeState(state);
        }
        else
        {
            stateManager.ChangeState(state);
        }

        yield return new WaitForSeconds(moveSprite);
        if(isMoving)
        {
            var position = transform.position + new Vector3(0f, 1.3f, 0f);
            transform.position = position;
        }
    }

    private float GetDelayTime(KX_Phase2Effect_State state)
    {
        return state switch
        {
            KX_Phase2Effect_State.Empty => 0f,
            KX_Phase2Effect_State.SpectralBlade => 0f,
            KX_Phase2Effect_State.Smoke2 => 1f,
            KX_Phase2Effect_State.BladeStorm => 1.2f,
            _ => 0f,
        };
    }

    public void SpawnPhase2Kaleos()
    {
        var position = transform.position + new Vector3(0f, 0f, 0f);
        Instantiate(phase2Kaleos, position, Quaternion.identity);
    }

    public void PlayAudio(int num)
    {
        switch (num)
        {
            case 1:
                SoundFXManager.Instance.PlaySoundFXClip(phase2Sound, transform, volume);
                break;
            case 2:
                SoundFXManager.Instance.PlaySoundFXClip(explosionAudio, transform, volume);
                break;
        }
    }
}
