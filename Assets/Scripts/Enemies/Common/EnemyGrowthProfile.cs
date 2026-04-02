/// <summary>
/// Defines how much each stat grows per level.
/// Total points spent per level should equal PointsPerLevel.
/// </summary>
[System.Serializable]
public class EnemyGrowthProfile
{
    // How many of those points go to each stat
    public float Exp = 10f;
    public float MaxHP = 8f;
    public float Strength = 3f;
    public float Magic = 3f;
    public float Defense = 3f;
    public float MagicResist = 3f;
    public float Speed = 0.02f; 
    public float AttackRange = 0f;   // fixed
    public float AttackCooldown = 0f; // negative or fixed
}