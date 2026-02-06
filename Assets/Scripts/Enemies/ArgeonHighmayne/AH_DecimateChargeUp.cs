using System.Collections;
using UnityEngine;

public class AH_DecimateChargeUp : MonoBehaviour
{
    [SerializeField] private AH_DecimateChargeUp_State state;
    [SerializeField] private Animator animator;
    [SerializeField] private float destroyTime;
    private StateManager<AH_DecimateChargeUp_State> stateManager;

    private void Start()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
        stateManager = new StateManager<AH_DecimateChargeUp_State>(animator, AH_DecimateChargeUp_State.Empty);

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

    private float GetDelayTime(AH_DecimateChargeUp_State state)
    {
        return state switch
        {
            AH_DecimateChargeUp_State.MartyDome => 0f,
            AH_DecimateChargeUp_State.Buff => 0.3f,
            AH_DecimateChargeUp_State.Smoke => 0.5f,
            AH_DecimateChargeUp_State.TeleportRecall => 0.7f,
            _ => 0f,
        };
    }
}
