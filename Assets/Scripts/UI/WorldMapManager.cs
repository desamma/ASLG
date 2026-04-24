using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class MapZone
{
    public string zoneName;         // Display name e.g. "Haunted Dungeon"
    public RectTransform marker;    // The pin/icon on the world map
    public Image fogCloud;          // Cloud image covering this zone (can be null)
    [HideInInspector] public bool discovered = false;
}

public class WorldMapManager : MonoBehaviour
{
    public static WorldMapManager Instance { get; private set; }

    [Header("Panels")]
    public GameObject worldMapPanel;   // Full world map panel — assign in Inspector
    public GameObject minimapPanel;    // Your existing minimap panel — assign in Inspector

    [Header("Zones")]
    public MapZone[] zones;

    [Header("Player Pin")]
    public RectTransform playerPin;    // Small icon showing current location
    public TMP_Text locationLabel;     // Text showing current zone name

    [Header("Marker Bounce")]
    public float bounceSpeed = 3f;
    public float bounceHeight = 10f;   // pixels up/down

    [Header("Fog Fade")]
    public float fogFadeDuration = 1.5f;

    private bool isMapOpen = false;
    private RectTransform activeMarker;
    private Vector2 activeMarkerBase;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        worldMapPanel.SetActive(false);   // map closed by default
        minimapPanel.SetActive(true);     // minimap visible by default
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
            ToggleMap();

        // Bounce the active marker while map is open
        if (activeMarker != null && isMapOpen)
        {
            float offsetY = Mathf.Sin(Time.time * bounceSpeed) * bounceHeight;
            activeMarker.anchoredPosition = activeMarkerBase + new Vector2(0f, offsetY);
        }
    }

    public void ToggleMap()
    {
        isMapOpen = !isMapOpen;
        worldMapPanel.SetActive(isMapOpen);
        minimapPanel.SetActive(!isMapOpen);   // hide minimap while map is open

        // Reset marker to base so it doesn't freeze mid-bounce when closing
        if (!isMapOpen && activeMarker != null)
            activeMarker.anchoredPosition = activeMarkerBase;
    }

    /// <summary>
    /// Call this whenever the player enters a new area.
    /// Place a ZoneTrigger on each map area and it will call this automatically.
    /// </summary>
    public void SetCurrentZone(string zoneName)
    {
        // Reset previous marker to base position
        if (activeMarker != null)
            activeMarker.anchoredPosition = activeMarkerBase;

        foreach (var zone in zones)
        {
            if (zone.zoneName != zoneName) continue;

            // ── Discover zone & fade fog ──
            if (!zone.discovered)
            {
                zone.discovered = true;
                if (zone.fogCloud != null)
                    StartCoroutine(FadeOutFog(zone.fogCloud));
            }

            // ── Move player pin ──
            if (playerPin != null)
                playerPin.anchoredPosition = zone.marker.anchoredPosition;

            // ── Update label ──
            if (locationLabel != null)
                locationLabel.text = zone.zoneName;

            // ── Track marker for bounce ──
            activeMarker = zone.marker;
            activeMarkerBase = zone.marker.anchoredPosition;

            break;
        }
    }
    IEnumerator FadeOutFog(Image fog)
    {
        float elapsed = 0f;
        Color c = fog.color;

        while (elapsed < fogFadeDuration)
        {
            elapsed += Time.deltaTime;
            fog.color = new Color(c.r, c.g, c.b, Mathf.Lerp(1f, 0f, elapsed / fogFadeDuration));
            yield return null;
        }

        fog.gameObject.SetActive(false);
    }
}