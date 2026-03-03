using UnityEngine;

public enum Enemy_Okkadok_State
{
    [StateAnimator("isIdle")]
    Idle,

    [StateAnimator("isWalk")]
    Chase,

    [StateAnimator("isWalk")]
    Retreat,

    [StateAnimator("isDeath")]
    Death,

    [StateAnimator("isAttack")]
    Attack,

    [StateAnimator("isWalk")]
    Patrol,

    [StateAnimator("isScream")]
    Scream,

    Knockback,
}
