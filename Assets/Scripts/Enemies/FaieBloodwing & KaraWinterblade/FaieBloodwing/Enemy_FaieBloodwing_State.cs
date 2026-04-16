public enum Enemy_FaieBloodwing_State
{
    [StateAnimator("isAttack")]
    Attack,

    [StateAnimator("isCast")]
    Cast,

    [StateAnimator("isDeath")]
    Death,

    [StateAnimator("isWalk")]
    Chase,

    [StateAnimator("isIdle")]
    Idle,

    Knockback,
}
