using UnityEngine;

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
}