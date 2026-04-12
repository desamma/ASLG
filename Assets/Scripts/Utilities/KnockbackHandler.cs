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

    /// <summary>
    /// Applies knockback to a target.
    /// </summary>
    /// <param name="target">target getting knocked back</param>
    /// <param name="applier">object applying the knockback</param>
    /// <param name="knockbackForce">force of the knockback</param>
    /// <param name="knockbackTime">duration of the knockback</param>
    /// <param name="stunTime">duration of the stun</param>
    /// <param name="onKnockbackStart">callback for when knockback starts</param>
    /// <param name="onStunEnd">callback for when stun ends</param>
    public void ApplyKnockback(Transform target, Transform applier, float knockbackForce,
    float knockbackTime, float stunTime, Action onKnockbackStart, Action onStunEnd)
    {
        if (target == null || applier == null || rb == null || owner == null || !IsOwnerAlive())
            return;

        onKnockbackStart?.Invoke();
        Vector2 knockbackDirection = (target.position - applier.position).normalized;

        owner.StartCoroutine(KnockBackRoutine(knockbackDirection * knockbackForce, knockbackTime, stunTime, onStunEnd));
    }

    private IEnumerator KnockBackRoutine(Vector2 knockbackVelocity, float knockbackTime,
        float stunTime, Action onStunEnd)
    {
        yield return new WaitForFixedUpdate(); // wait for FixedUpdate to finish th frame
        rb.velocity = knockbackVelocity;

        yield return new WaitForSeconds(knockbackTime);
        rb.velocity = Vector2.zero;

        yield return new WaitForSeconds(stunTime);
        onStunEnd?.Invoke();
    }

    /// <summary>
    /// Reflection since it was not in interface
    /// </summary>
    /// <returns>is owner alive</returns>
    private bool IsOwnerAlive()
    {
        if (owner == null) return false;

        if (owner.CompareTag("Player"))
            return StatsManager.instance != null && !StatsManager.instance.IsDead;

        if (owner.TryGetComponent<IEnemy_Health>(out var enemyHealth))
        {
            var healthType = enemyHealth.GetType();

            var isDeadField = healthType.GetField("isDead");
            if (isDeadField != null && isDeadField.FieldType == typeof(bool))
                return !(bool)isDeadField.GetValue(enemyHealth);
        }
        return true;
    }
}