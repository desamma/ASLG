public enum Enemy_Dogehai_State
{

    [StateAnimator("isAttack")]
    Attack,

    [StateAnimator("isDeath")]
    Death,

    [StateAnimator("isWalk")]
    Chase,

    [StateAnimator("isIdle")]
    Idle,

    [StateAnimator("isDig")]
    Dig,

    Knockback,
}
