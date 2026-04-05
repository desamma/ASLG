public enum Enemy_DrakeDowager_State
{
    [StateAnimator("isIdle")]
    Idle,

    [StateAnimator("isWalk")]
    Chase,

    [StateAnimator("isDeath")]
    Death,

    [StateAnimator("isAttack")]
    Attack,

    [StateAnimator("isWalk")]
    Patrol,

    Knockback,
}
