public enum Enemy_ArrowWhistler_State
{
    [StateAnimator("Idle")]
    Idle,

    [StateAnimator("Walk")]
    Chase,

    [StateAnimator("Death")]
    Death,

    [StateAnimator("Attack")]
    Attack,

    [StateAnimator("Walk")]
    Patrol,

    Knockback,
}
