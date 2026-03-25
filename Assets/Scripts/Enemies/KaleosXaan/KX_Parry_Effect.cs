using System.Collections;
using UnityEngine;

public class KX_Parry_Effect : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float setInactiveTime = 1.5f;

    [Header("Effect Settings")]
    [SerializeField] private Animator animator;
    [SerializeField] private KX_ParryEffect_State state;
    private StateManager<KX_ParryEffect_State> stateManager;

    private void Start()
    {
        stateManager = new StateManager<KX_ParryEffect_State>(animator, KX_ParryEffect_State.Empty);
    }

    private void OnEnable()
    {
        stateManager = new StateManager<KX_ParryEffect_State>(animator, KX_ParryEffect_State.Empty);
        StartCoroutine(AnimationDelay());
        if (setInactiveTime > 0f)
        {
            StartCoroutine(SetInactiveAfterDelay());
        }
    }

    private IEnumerator SetInactiveAfterDelay()
    {
        yield return new WaitForSeconds(setInactiveTime);
        gameObject.SetActive(false);
    }

    private IEnumerator AnimationDelay()
    {
        var delayTime = GetDelayTime(state);
        yield return new WaitForSeconds(delayTime);
        stateManager.ChangeState(state);
    }

    private float GetDelayTime(KX_ParryEffect_State state)
    {
        return state switch
        {
            KX_ParryEffect_State.Empty => 0f,
            KX_ParryEffect_State.CrossSlash => 0f,
            KX_ParryEffect_State.CollisionSpark => 0.5f,
            _ => 0f,
        };
    }
}
