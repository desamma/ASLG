public enum PlayerState
{
    [StateAnimator("isIdle")] Idle,
    [StateAnimator("isMoving")] Move,
    [StateAnimator("isAttacking")] Attack,
    [StateAnimator("isHurt")] Hurt,
    [StateAnimator("isDeath")] Death,
    [StateAnimator("isDash")] Dash,
    [StateAnimator("isKnockback")] Knockback,
    [StateAnimator("isKnightSkill")] KnightSkill,
    [StateAnimator("isArcherSkill")] ArcherSkill,
    [StateAnimator("isRogueSkill")] RogueSkill,
    [StateAnimator("isSummonerSkill")] SummonerSkill,
    [StateAnimator("isMonolithPickup")] MonolithPickup,
    [StateAnimator("isSlowWalk")] SlowWalk
}