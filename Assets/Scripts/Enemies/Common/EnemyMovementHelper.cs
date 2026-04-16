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
        else if (patrolInstead || OnPatrolInsteadOfIdle != null)
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
        bool isStopOnAttackRange = true, Action OnEnterAttackRange = null, Func<Vector2> positionOverride = null, Transform destinationOverride = null)
    {
        if (context.PlayerTransform == null) return;
        var destination = destinationOverride != null ? (Vector2)destinationOverride.position : (Vector2)context.PlayerTransform.position;
        Vector2 selfPosition = positionOverride?.Invoke() ?? (Vector2)context.SelfTransform.position;

        float dist = Vector2.Distance(selfPosition, destination);
        float attackRange = attackRangeOverride > 0f ? attackRangeOverride : context.Stats.AttackRange;

        if (isStopOnAttackRange && dist <= attackRange)
        {
            context.Rb.velocity = Vector2.zero;
            OnEnterAttackRange?.Invoke();
            return;
        }

        context.FacingDirection = TransformHelper.FlipTowards(context.SelfTransform, destination, context.FacingDirection);

        Vector2 direction = (destination - selfPosition).normalized;
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
            if (roll > cumulative) continue;

            var available = category.Attacks
                .Where(atkConfig => dist <= atkConfig.Range
                         && !cooldowns.IsOnCooldown(atkConfig.State)
                         && (extraFilter == null || extraFilter(atkConfig.State)))
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
    ref float waitTimer, ref float stuckCheckTimer, ref Vector2 lastCheckedPosition,
    float idleWaitTime, float unstuckCheckInterval, float stuckThreshold,
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
                stuckCheckTimer = 0f;
                lastCheckedPosition = context.SelfTransform.position;

                int newIndex;
                do { newIndex = UnityEngine.Random.Range(0, patrolPoints.Length); }
                while (newIndex == currentPatrolIndex && patrolPoints.Length > 1);
                currentPatrolIndex = newIndex;
                onSetPatrol?.Invoke();
            }
            return;
        }

        Vector3 target = patrolPoints[currentPatrolIndex];
        float distToTarget = Vector2.Distance(context.SelfTransform.position, target);

        // Arrived at destination normally
        if (distToTarget < 0.1f)
        {
            context.Rb.velocity = Vector2.zero;
            isWaiting = true;
            waitTimer = 0f;
            stuckCheckTimer = 0f;
            lastCheckedPosition = context.SelfTransform.position;
            return;
        }

        // Move toward target
        Vector2 direction = ((Vector2)target - (Vector2)context.SelfTransform.position).normalized;
        context.Rb.velocity = direction * context.Stats.Speed;
        context.FacingDirection = TransformHelper.FlipTowards(context.SelfTransform, target, context.FacingDirection);

        // Stuck detection: periodically check if we've actually moved
        stuckCheckTimer += Time.deltaTime;
        if (stuckCheckTimer >= unstuckCheckInterval)
        {
            float movedDist = Vector2.Distance(context.SelfTransform.position, lastCheckedPosition);
            lastCheckedPosition = context.SelfTransform.position;
            stuckCheckTimer = 0f;

            if (movedDist < stuckThreshold)
            {
                // Stuck — skip to the next patrol point
                context.Rb.velocity = Vector2.zero;

                int newIndex;
                do { newIndex = UnityEngine.Random.Range(0, patrolPoints.Length); }
                while (newIndex == currentPatrolIndex && patrolPoints.Length > 1);
                currentPatrolIndex = newIndex;

                // Go straight to moving, no idle pause on stuck
                onSetPatrol?.Invoke();
                return;
            }
        }
    }
}