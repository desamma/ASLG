public enum Enemy_Andromeda_State
{
    [StateAnimator("isAttack")]
    Attack,

    [StateAnimator("isDeath")]
    Death,

    [StateAnimator("isSpell")]
    Spell,

    [StateAnimator("isWalk")]
    Chase,

    [StateAnimator("isCast")]
    Cast,

    [StateAnimator("isIdle")]
    Idle,

    [StateAnimator("isWalk")]
    Patrol,

    Knockback,
}
