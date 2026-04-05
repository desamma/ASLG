public enum Enemy_Coalfist_State
{
    [StateAnimator("isAttack")]
    Attack,

    [StateAnimator("isBackStepAttack")]
    BackStepAttack,

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
