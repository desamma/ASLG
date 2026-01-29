public enum Enemy_Pax_State
{
    [StateAnimator("isAttack")]
    Attack,

    [StateAnimator("isDeath")]
    Death,

    [StateAnimator("isWalk")]
    Chase,

    [StateAnimator("isIdle")]
    Idle,

    [StateAnimator("isLickPaw")]
    LickPaw,

    [StateAnimator("isWalk")]
    Patrol,

    Knockback,
}
