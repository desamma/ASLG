public enum Enemy_ArgeonHighmayne_State
{
    [StateAnimator("isAttack")]
    Attack,

    [StateAnimator("isDecimate")]
    Decimated,

    [StateAnimator("isWarSurge")]
    WarSurge,

    [StateAnimator("isEntropicDecay")]
    EntropicDecay,

    [StateAnimator("isAurynNexus")]
    AurynNexus,

    [StateAnimator("isSunBloom")]
    SunBloom,

    [StateAnimator("isDeath")]
    Death,

    [StateAnimator("isWalk")]
    Chase,

    [StateAnimator("isIdle")]
    Idle,

    Knockback,
}