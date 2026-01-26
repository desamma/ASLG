public enum Enemy_Slime_State
{
    [StateAnimator("isIdle")]
    Idle,

    [StateAnimator("isSpin")]
    Spin,

    [StateAnimator("isWalk")]
    Chase,

    [StateAnimator("isJump")]
    Jump,

    [StateAnimator("isSleep")]
    Sleep,

    [StateAnimator("isWalk")]
    Patrol,

    Knockback,
}
