using UnityEngine;

/// <summary>
/// Utility methods for flipping 2D enemies toward or away from a target.
/// Keeps the localScale contract consistent.
/// </summary>
public static class TransformHelper
{
    /// <summary>
    /// Flips the transform to face towards the target on the X axis.
    /// </summary>
    /// <param name="self">The transform of the object to flip.</param>
    /// <param name="target">The transform of the target to face.</param>
    /// <param name="currentFacing">The current facing direction (1 for right, -1 for left).</param>
    /// <returns>The new facing direction after flipping.</returns>
    public static int FlipTowards(Transform self, Transform target, int currentFacing)
    {
        if (self == null || target == null)
            return currentFacing;

        bool shouldFlipRight = target.position.x > self.position.x && currentFacing == -1;
        bool shouldFlipLeft = target.position.x < self.position.x && currentFacing == 1;

        if (shouldFlipRight || shouldFlipLeft)
        {
            currentFacing *= -1;
            Vector3 localScale = self.localScale;
            localScale.x *= -1;
            self.localScale = localScale;
        }

        return currentFacing;
    }

    /// <summary>
    /// Flips the transform to face towards the target on the X axis.
    /// </summary>
    /// <param name="self">The transform of the object to flip.</param>
    /// <param name="target">The transform of the target to face.</param>
    /// <param name="currentFacing">The current facing direction (1 for right, -1 for left).</param>
    /// <returns>The new facing direction after flipping.</returns>
    public static int FlipTowards(Transform self, Vector3 target, int currentFacing)
    {
        if (self == null || target == null)
            return currentFacing;

        bool shouldFlipRight = target.x > self.position.x && currentFacing == -1;
        bool shouldFlipLeft = target.x < self.position.x && currentFacing == 1;

        if (shouldFlipRight || shouldFlipLeft)
        {
            currentFacing *= -1;
            Vector3 localScale = self.localScale;
            localScale.x *= -1;
            self.localScale = localScale;
        }

        return currentFacing;
    }

    /// <summary>
    /// Flips the transform to face away from the target on the X axis.
    /// </summary>
    /// <param name="self">The transform of the object to flip.</param>
    /// <param name="target">The transform of the target to face.</param>
    /// <param name="currentFacing">The current facing direction (1 for right, -1 for left).</param>
    /// <returns></returns>
    public static int FlipAway(Transform self, Transform target, int currentFacing)
    {
        if (self == null || target == null)
            return currentFacing;

        bool shouldFlipRight = target.position.x > self.position.x && currentFacing == 1;
        bool shouldFlipLeft = target.position.x < self.position.x && currentFacing == -1;

        if (shouldFlipRight || shouldFlipLeft)
        {
            currentFacing *= -1;
            Vector3 localScale = self.localScale;
            localScale.x *= -1;
            self.localScale = localScale;
        }

        return currentFacing;
    }

    /// <summary>
    /// Flips the transform to face away from the target on the X axis.
    /// </summary>
    /// <param name="self">The transform of the object to flip.</param>
    /// <param name="target">The transform of the target to face.</param>
    /// <param name="currentFacing">The current facing direction (1 for right, -1 for left).</param>
    /// <returns></returns>
    public static int FlipAway(Transform self, Vector3 target, int currentFacing)
    {
        if (self == null || target == null)
            return currentFacing;

        bool shouldFlipRight = target.x > self.position.x && currentFacing == 1;
        bool shouldFlipLeft = target.x < self.position.x && currentFacing == -1;

        if (shouldFlipRight || shouldFlipLeft)
        {
            currentFacing *= -1;
            Vector3 localScale = self.localScale;
            localScale.x *= -1;
            self.localScale = localScale;
        }

        return currentFacing;
    }

    /// <summary>
    /// Finds the closest transform to the center within the radius that is on the specified layer mask.
    /// </summary>
    /// <param name="center">The center point to search from.</param>
    /// <param name="radius">The radius within which to search.</param>
    /// <param name="layerMask">The layer mask to filter the search.</param>
    /// <returns>The closest transform found, or null if none are found.</returns>
    public static Transform FindClosestInRange(Vector3 center, float radius, LayerMask layerMask)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius, layerMask);
        if (hits == null || hits.Length == 0)
            return null;

        float best = float.MaxValue;
        Transform closest = null;

        foreach (var col in hits)
        {
            float sq = (col.transform.position - center).sqrMagnitude;
            if (sq < best)
            {
                best = sq;
                closest = col.transform;
            }
        }

        return closest;
    }

    /// <summary>
    /// Returns true if the source transform is facing towards the target on the X axis.
    /// Assumes positive localScale.x means facing right and negative means facing left.
    /// Returns false if either transform is null.
    /// </summary>
    public static bool IsFacingTarget2D(Transform source, Transform target)
    {
        if (source == null || target == null)
            return false;

        float dx = target.position.x - source.position.x;
        float facingSign = Mathf.Sign(source.localScale.x);

        return (facingSign > 0f && dx > 0f) || (facingSign < 0f && dx < 0f);
    }
}