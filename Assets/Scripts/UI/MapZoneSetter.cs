using UnityEngine;
using UnityEngine.SceneManagement;

public class MapZoneSetter : MonoBehaviour
{
    [Tooltip("Used for area triggers.")]
    public string zoneName;

    [Tooltip("Scene to load.")]
    public string targetSceneName;

    [Tooltip("Prevents immediate re-triggering after a scene change.")]
    public float sceneTransitionLockDuration = 5f;

    void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider == null || !collider.CompareTag("Player")) return;
        if (MapSceneTransitionState.IsTriggerLocked) return;

        bool isEdgeTrigger = !string.IsNullOrWhiteSpace(targetSceneName);

        // Only update world map if this is an AREA trigger (no scene change)
        if (!isEdgeTrigger && WorldMapManager.Instance != null && !string.IsNullOrWhiteSpace(zoneName))
            WorldMapManager.Instance.SetCurrentZone(zoneName);

        if (!isEdgeTrigger) return;

        MapSceneTransitionState.BeginTransition(sceneTransitionLockDuration, zoneName);
        SceneManager.LoadScene(targetSceneName);
    }
}