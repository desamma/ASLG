using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public static class MapSceneTransitionState
{
    private static float sceneTriggerLockUntilTime;
    public static bool IsTriggerLocked => Time.unscaledTime < sceneTriggerLockUntilTime;

    public static string OriginZoneName { get; private set; } = "";

    public static void BeginTransition(float lockDuration, string originZone = "")
    {
        sceneTriggerLockUntilTime = Time.unscaledTime + Mathf.Max(0f, lockDuration);
        OriginZoneName = originZone;
    }

    /// <summary>
    /// Refreshes Physics2D system to ensure all colliders (especially tilemaps) are properly recognized.
    /// Call this after scene loads to fix issues where colliders don't work in builds.
    /// </summary>
    public static void RefreshPhysics2D()
    {
        Physics2D.Simulate(0f);

        // Force all colliders to refresh their state
        Collider2D[] allColliders = Object.FindObjectsOfType<Collider2D>();
        foreach (Collider2D collider in allColliders)
        {
            if (collider != null && collider.isActiveAndEnabled)
            {
                collider.enabled = false;
                collider.enabled = true;
            }
        }
    }
}