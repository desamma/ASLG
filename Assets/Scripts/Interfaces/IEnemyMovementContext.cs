using UnityEngine;

// Quick copy
//public Transform PlayerTransform { get; set; }
//public bool IsRecovering { get; set; }
//public int FacingDirection { get; set; }
//public Rigidbody2D Rb => rb;
//public BehaviorProfile Behavior => behavior;
//public EnemyStats Stats => stats;
//public Transform DetectionPoint => detectionPoint;
//public LayerMask PlayerLayer => playerLayer;
//public Transform SelfTransform => transform;

/// <summary>
/// Interface for helper class
/// </summary>
public interface IEnemyMovementContext
{
    Transform PlayerTransform { get; set; }
    Rigidbody2D Rb { get; }
    BehaviorProfile Behavior { get; }
    EnemyStats Stats { get; }
    Transform DetectionPoint { get; }
    LayerMask PlayerLayer { get; }
    int FacingDirection { get; set; }
    Transform SelfTransform { get; }

    bool IsInAnyAttackState();
    bool IsRecovering { get; }
    void ChangeToChaseState();
    void ChangeToIdleState();
}