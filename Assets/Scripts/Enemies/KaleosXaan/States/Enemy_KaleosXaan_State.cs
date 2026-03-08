
public enum Enemy_KaleosXaan_State
{

    [StateAnimator("isAttack")]
    Attack,

    [StateAnimator("isArcaneHeart")]
    ArcaneHeart,

    [StateAnimator("isBlinkEnhance")]
    BlinkEnhance,

    [StateAnimator("isSummonCompanion")]
    SummonCompanion,

    [StateAnimator("isDaemonicLure")]
    DaemonicLure,

    [StateAnimator("isDeath")]
    Death,

    [StateAnimator("isWalk")]
    Chase,

    [StateAnimator("isIdle")]
    Idle,

    Knockback,
}
