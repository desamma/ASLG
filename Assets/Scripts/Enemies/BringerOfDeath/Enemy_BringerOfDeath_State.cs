public enum Enemy_BringerOfDeath_State
{
    [StateAnimator("Idle")]
    Idle,

    [StateAnimator("Walk")]
    Chase,

    [StateAnimator("Hurt")]
    Hurt,

    [StateAnimator("Death")]
    Death,

    [StateAnimator("Cast")]
    Cast,

    [StateAnimator("Attack")]
    Attack,

    [StateAnimator("Hurt")]
    Knockback,

    [StateAnimator("Walk")]
    Patrol,

    [StateAnimator("Spell")]
    Spell
}
