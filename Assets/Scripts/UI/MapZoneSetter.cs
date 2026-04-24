using UnityEngine;

public class MapZoneSetter : MonoBehaviour
{
    [Tooltip("Must exactly match the zoneName in WorldMapManager")]
    public string zoneName;

    void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.CompareTag("Player"))
            WorldMapManager.Instance.SetCurrentZone(zoneName);
    }
}