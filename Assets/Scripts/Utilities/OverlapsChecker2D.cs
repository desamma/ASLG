using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Utility for checking if a Transform overlaps with terrain or colliders in 2D.
/// </summary>
public class OverlapChecker2D
{
    /// <summary>
    /// Returns true if anything overlaps within a circle radius around the target.
    /// Ignores the target and its children.
    /// </summary>
    public bool IsOverlappingAnything(Transform target, float radius = 0.5f)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(target.position, radius);

        foreach (Collider2D hit in hits)
        {
            if (IsSelfOrChild(hit.transform, target)) continue;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Returns true if the target overlaps any Collider2D on the given layers.
    /// </summary>
    public bool IsOverlappingLayer(Transform target, LayerMask mask, float radius = 0.5f)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(target.position, radius, mask);

        foreach (Collider2D hit in hits)
        {
            if (IsSelfOrChild(hit.transform, target)) continue;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Returns true if the target overlaps any Collider2D on the given layer names.
    /// </summary>
    public bool IsOverlappingLayer(Transform target, float radius = 0.5f, params string[] layerNames)
    {
        LayerMask mask = LayerMask.GetMask(layerNames);
        return IsOverlappingLayer(target, mask, radius);
    }

    /// <summary>
    /// Returns all Collider2Ds overlapping within a circle radius, excluding self and children.
    /// Useful for debugging or applying effects to all overlapped objects.
    /// </summary>
    public Collider2D[] GetOverlappingColliders(Transform target, float radius = 0.5f, LayerMask? mask = null)
    {
        Collider2D[] hits = mask.HasValue
            ? Physics2D.OverlapCircleAll(target.position, radius, mask.Value)
            : Physics2D.OverlapCircleAll(target.position, radius);

        List<Collider2D> results = new List<Collider2D>();

        foreach (Collider2D hit in hits)
        {
            if (IsSelfOrChild(hit.transform, target)) continue;
            results.Add(hit);
        }

        return results.ToArray();
    }

    private bool IsSelfOrChild(Transform hit, Transform target)
    {
        return hit == target || hit.IsChildOf(target);
    }
}