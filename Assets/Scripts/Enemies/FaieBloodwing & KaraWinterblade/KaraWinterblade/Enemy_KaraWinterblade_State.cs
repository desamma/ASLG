public enum Enemy_KaraWinterblade_State
{
    [StateAnimator("isAttack")]
    Attack,

    [StateAnimator("isThreeHitAttack")]
    ThreeHitAttack,

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
