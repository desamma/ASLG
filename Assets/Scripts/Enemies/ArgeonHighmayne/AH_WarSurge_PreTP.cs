using System.Collections;
using UnityEngine;

public class AH_WarSurge_PreTP : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private AH_WarSurge_PreTP_State state;
    [SerializeField] private float destroyTime = 5f;
    [SerializeField] private Animator animator;
    private StateManager<AH_WarSurge_PreTP_State> stateManager;

    private void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        stateManager = new StateManager<AH_WarSurge_PreTP_State>(animator, AH_WarSurge_PreTP_State.Empty);

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

    private float GetDelayTime(AH_WarSurge_PreTP_State state)
    {
        return state switch
        {
            AH_WarSurge_PreTP_State.Empty => 0f,
            AH_WarSurge_PreTP_State.Smoke2 => 0f,
            AH_WarSurge_PreTP_State.EnergyHalo => 0.5f,
            AH_WarSurge_PreTP_State.Buff => 0.7f,
            _ => 0f,
        };
    }
}
