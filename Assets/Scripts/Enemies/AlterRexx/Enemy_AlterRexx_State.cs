public enum Enemy_AlterRexx_State
{
    [StateAnimator("isAttack")]
    Attack,

    [StateAnimator("isDeath")]
    Death,

    [StateAnimator("isWalk")]
    Chase,

    [StateAnimator("isIdle")]
    Idle,

    Knockback,
}
