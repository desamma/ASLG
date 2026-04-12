public enum Enemy_Chakkram_State
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
