public enum Enemy_FaieBloodwingMK2_State
{
    [StateAnimator("isAttack")]
    Attack,

    [StateAnimator("isBladeShootAttack")]
    BladeShootAttack,

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
