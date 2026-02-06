using System.Collections;
using UnityEngine;

public class AH_NormalAttackEffectHit : MonoBehaviour
{
    [SerializeField] private AH_NormalAttackEffectHit_State state;
    [SerializeField] private Animator animator;
    [SerializeField] private float destroyTime;
    private StateManager<AH_NormalAttackEffectHit_State> stateManager;

    private void Start()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
        stateManager = new StateManager<AH_NormalAttackEffectHit_State>(animator, AH_NormalAttackEffectHit_State.Empty);
        StartCoroutine(AnimationDelay());
        if (destroyTime > 0f)
            Destroy(gameObject, destroyTime);
    }
    private IEnumerator AnimationDelay()
    {
        var delayTime = GetDelayTime(state);
        yield return new WaitForSeconds(delayTime);
        stateManager.ChangeState(state);
    }

    private float GetDelayTime(AH_NormalAttackEffectHit_State state)
    {
        return state switch
        {
            AH_NormalAttackEffectHit_State.CrossSlash => 0f,
            AH_NormalAttackEffectHit_State.Impact => 0.5f,
            AH_NormalAttackEffectHit_State.CollisionSpark => 0.8f,
            _ => 0f,
        };
    }
}