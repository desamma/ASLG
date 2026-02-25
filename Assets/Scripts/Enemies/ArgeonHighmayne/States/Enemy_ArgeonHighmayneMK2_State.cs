public enum Enemy_ArgeonHighmayneMK2_State
{
    [StateAnimator("isAttack")]
    Attack,

    [StateAnimator("isDecimate")]
    Decimated,

    [StateAnimator("isWarSurge")]
    WarSurge,

    [StateAnimator("isDualCast")]
    DualCast,

    [StateAnimator("isDeath")]
    Death,

    [StateAnimator("isWalk")]
    Chase,

    [StateAnimator("isIdle")]
    Idle,

    Knockback,
}