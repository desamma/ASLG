using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class MapZone
{
    public string zoneName;    
    public RectTransform marker;
    public Image fogCloud;      
    [HideInInspector] public bool discovered = false;
}

public class WorldMapManager : MonoBehaviour
{
    public static WorldMapManager Instance { get; private set; }

    [Header("Panels")]
    public GameObject worldMapPanel;  
    public GameObject minimapPanel; 

    [Header("Zones")]
    public MapZone[] zones;

    [Header("Player Pin")]
    public RectTransform playerPin;    // Small icon showing current location
    public TMP_Text locationLabel;     // Text showing current zone name
    public Vector2 locationLabelOffset = new Vector2(0f, 50f);

    [Header("Marker Bounce")]
    public float bounceSpeed = 3f;
    public float bounceHeight = 10f;  

    [Header("Fog Fade")]
    public float fogFadeDuration = 3f;

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
        worldMapPanel.SetActive(false);
        minimapPanel.SetActive(true);   
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

            if (locationLabel != null)
                locationLabel.rectTransform.anchoredPosition = activeMarker.anchoredPosition + locationLabelOffset;
        }
    }

    public void ToggleMap()
    {
        isMapOpen = !isMapOpen;

        Canvas c = this.GetComponent<Canvas>();
        c.sortingOrder = isMapOpen ? 100 : 0; // Ensure map renders above other UI when open  

        worldMapPanel.SetActive(isMapOpen);
        minimapPanel.SetActive(!isMapOpen);   // hide minimap while map is open

        // Reset marker to base so it doesn't freeze mid-bounce when closing
        if (!isMapOpen && playerPin != null)
            playerPin.anchoredPosition = activeMarkerBase;
    }

    /// <summary>
    /// Call this whenever the player enters a new area.
    /// Place a ZoneTrigger on each map area and it will call this automatically.
    /// </summary>
    public void SetCurrentZone(string zoneName)
    {
        if (playerPin != null)
            playerPin.anchoredPosition = activeMarkerBase;

        foreach (var zone in zones)
        {
            if (zone.zoneName != zoneName) continue;

            if (!zone.discovered)
            {
                zone.discovered = true;
                if (zone.fogCloud != null)
                    StartCoroutine(FadeOutFog(zone.fogCloud));

                //write discovery to save data immediately
                if (SaveManager.Instance != null &&
                    !SaveManager.Instance.currentSaveData.discoveredZones.Contains(zoneName))
                {
                    SaveManager.Instance.currentSaveData.discoveredZones.Add(zoneName);
                }
            }

            if (playerPin != null)
            {
                playerPin.anchoredPosition = zone.marker.anchoredPosition;
                if (locationLabel != null)
                    locationLabel.rectTransform.anchoredPosition = playerPin.anchoredPosition + locationLabelOffset;
            }

            if (locationLabel != null)
                locationLabel.text = zone.zoneName;

            activeMarker = playerPin;
            activeMarkerBase = playerPin != null ? playerPin.anchoredPosition : Vector2.zero;
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