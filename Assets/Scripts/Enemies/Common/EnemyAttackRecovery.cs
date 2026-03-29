using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Handles post-attack recovery windows for enemies.
/// Caller owns state machine enum; this just runs timing and callbacks.
/// </summary>
public class EnemyAttackRecovery
{
    private readonly MonoBehaviour owner;
    private readonly Rigidbody2D rb;

    public EnemyAttackRecovery(MonoBehaviour owner, Rigidbody2D rb)
    {
        this.owner = owner;
        this.rb = rb;
    }

    public void StartRecovery(float duration, Action onStart, Action onEnd)
    {
        if (owner == null) return;
        owner.StartCoroutine(RecoveryRoutine(duration, onStart, onEnd));
    }

    private IEnumerator RecoveryRoutine(float duration, Action onStart, Action onEnd)
    {
        onStart?.Invoke();

        if (rb != null)
            rb.velocity = Vector2.zero;

        yield return new WaitForSeconds(duration);
        onEnd?.Invoke();
    }
}