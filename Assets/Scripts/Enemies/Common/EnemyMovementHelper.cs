using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Reusable helper class for enemy movement logic.
/// Optional btw
/// </summary>
public static class EnemyMovementHelper
{
    public static void CheckForPlayer(IEnemyMovementContext context, Action<float> OnPlayerFound = null, bool patrolInstead = false, Action OnPatrolInsteadOfIdle = null)
    {
        if (context.PlayerTransform != null)
        {
            float dist = Vector2.Distance(context.SelfTransform.position, context.PlayerTransform.position);
            if (dist > context.Behavior.DetectionRange)
                context.PlayerTransform = null;
        }
        else
        {
            context.PlayerTransform = TransformHelper.FindClosestInRange(
                context.DetectionPoint.position, context.Behavior.DetectionRange, context.PlayerLayer);
        }

        if (context.PlayerTransform != null)
        {
            float dist = Vector2.Distance(context.SelfTransform.position, context.PlayerTransform.position);
            OnPlayerFound?.Invoke(dist);
        }
        else if (patrolInstead)
        {
            OnPatrolInsteadOfIdle?.Invoke();
        }
        else
        {
            context.Rb.velocity = Vector2.zero;
            context.ChangeToIdleState();
        }

    }
    public static void Chase(IEnemyMovementContext context, float speedMultiplier = 1f, float attackRangeOverride = -1,
        bool isStopOnAttackRange = true, Action OnEnterAttackRange = null, Func<Vector2> positionOverride = null)
    {
        if (context.PlayerTransform == null) return;

        Vector2 selfPosition = positionOverride?.Invoke() ?? (Vector2)context.SelfTransform.position;
        float dist = Vector2.Distance(selfPosition, context.PlayerTransform.position);
        float attackRange = attackRangeOverride > 0f ? attackRangeOverride : context.Stats.AttackRange;

        if (isStopOnAttackRange && dist <= attackRange)
        {
            context.Rb.velocity = Vector2.zero;
            OnEnterAttackRange?.Invoke();
            return;
        }

        context.FacingDirection = TransformHelper.FlipTowards(context.SelfTransform, context.PlayerTransform, context.FacingDirection);

        Vector2 direction = ((Vector2)context.PlayerTransform.position - selfPosition).normalized;
        context.Rb.velocity = context.Behavior.Aggression * context.Stats.Speed * speedMultiplier * direction;
    }

    public static void DecideAttackType<TState>(IEnemyMovementContext context, IReadOnlyList<AttackCategory<TState>> categories, EnemyMoveCooldownTracker<TState> cooldowns,
        Action<TState> OnAttackSelected, Func<TState, bool> extraFilter = null, Action OnPriorityCodeBeforeRoll = null) where TState : Enum
    {
        if (context.PlayerTransform == null) return;

        OnPriorityCodeBeforeRoll?.Invoke();

        float dist = Vector2.Distance(context.SelfTransform.position, context.PlayerTransform.position);
        float roll = UnityEngine.Random.value;
        float cumulative = 0f;

        foreach (var category in categories)
        {
            cumulative += category.Frequency;
            if (roll >= cumulative) continue;

            var available = category.Attacks
                .Where(a => dist <= a.Range
                         && !cooldowns.IsOnCooldown(a.State)
                         && (extraFilter == null || !extraFilter(a.State)))
                .ToArray();

            if (available.Length > 0)
            {
                var selected = available[UnityEngine.Random.Range(0, available.Length)];
                OnAttackSelected(selected.State);
                return;
            }
        }
    }

    public static void Patrol(IEnemyMovementContext context, Vector2[] patrolPoints, ref int currentPatrolIndex, ref bool isWaiting,
        ref float waitTimer, ref float unstuckTimer, float idleWaitTime, float unstuckWaitTime,
        Action onSetIdle, Action onSetPatrol)
    {
        if (isWaiting)
        {
            onSetIdle?.Invoke();
            waitTimer += Time.deltaTime;
            if (waitTimer >= idleWaitTime)
            {
                isWaiting = false;
                waitTimer = 0f;
                int newIndex;
                do { newIndex = UnityEngine.Random.Range(0, patrolPoints.Length); }
                while (newIndex == currentPatrolIndex);
                currentPatrolIndex = newIndex;
                onSetPatrol?.Invoke();
            }
            return;
        }

        if (unstuckTimer > 0)
        {
            unstuckTimer -= Time.deltaTime;
            Vector3 target = patrolPoints[currentPatrolIndex];
            context.Rb.velocity = (target - context.SelfTransform.position).normalized * context.Stats.Speed;

            context.FacingDirection = TransformHelper.FlipTowards(context.SelfTransform, target, context.FacingDirection);

            if (Vector2.Distance(context.SelfTransform.position, target) < 0.1f)
            {
                context.Rb.velocity = Vector2.zero;
                isWaiting = true;
            }
        }
        else
        {
            context.Rb.velocity = Vector2.zero;
            unstuckTimer = unstuckWaitTime;
            isWaiting = true;
        }
    }
}