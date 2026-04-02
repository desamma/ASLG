public enum Enemy_PeaceKeeper_State
{
    [StateAnimator("isAttack")]
    Attack,

    [StateAnimator("isDeath")]
    Death,

    [StateAnimator("isWalk")]
    Chase,

    [StateAnimator("isIdle")]
    Idle,

    [StateAnimator("isWalk")]
    Patrol,

    Knockback,
}
