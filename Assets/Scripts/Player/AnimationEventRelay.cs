using UnityEngine;

public class AnimationEventRelay : MonoBehaviour
{
    private PlayerAttack playerAttack;
    private PlayerMovement playerMovement;

    private void Start()
    {
        playerAttack = GetComponentInParent<PlayerAttack>();
        playerMovement = GetComponentInParent<PlayerMovement>();
    }

    public void OnAttackHitFrame()
    {
        if (playerAttack != null) playerAttack.OnAttackHitFrame();
    }

    public void OnAttackAnimationComplete()
    {
        if (playerAttack != null) playerAttack.OnAttackAnimationComplete();
        if (playerMovement != null) playerMovement.OnAttackAnimationComplete();
    }
}