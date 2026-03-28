using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Reusable knockback + stun sequence.
/// </summary>
public class KnockbackHandler
{
    private readonly MonoBehaviour owner;
    private readonly Rigidbody2D rb;

    public KnockbackHandler(MonoBehaviour owner, Rigidbody2D rb)
    {
        this.owner = owner;
        this.rb = rb;
    }

    public void ApplyKnockback(Transform enemyTransform,Transform player,float knockbackForce,float knockbackTime,float stunTime,
        Action onKnockbackStart, Action onStunEnd)
    {
        if (enemyTransform == null || player == null || rb == null || owner == null)
            return;

        onKnockbackStart?.Invoke();

        Vector2 knockbackDirection = (enemyTransform.position - player.position).normalized;
        rb.velocity = knockbackDirection * knockbackForce;

        owner.StartCoroutine(KnockBackRoutine(knockbackTime, stunTime, onStunEnd));
    }

    private IEnumerator KnockBackRoutine(float knockbackTime, float stunTime, Action onStunEnd)
    {
        yield return new WaitForSeconds(knockbackTime);
        rb.velocity = Vector2.zero;

        yield return new WaitForSeconds(stunTime);
        onStunEnd?.Invoke();
    }
}