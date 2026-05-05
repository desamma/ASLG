using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; 

[System.Serializable]
public class MapZone
{
    public string zoneName;
    public string sceneName; 
    public Button teleportButton; 
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
    public RectTransform playerPin;    
    public TMP_Text locationLabel;     
    public Vector2 locationLabelOffset = new Vector2(0f, 50f);

    [Header("Marker Bounce")]
    public float bounceSpeed = 3f;
    public float bounceHeight = 10f;

    [Header("Fog Fade")]
    public float fogFadeDuration = 3f;

    private bool isMapOpen = false;
    private RectTransform activeMarker;
    private Vector2 activeMarkerBase;

    private bool isTeleportMode = false; 

    public bool IsMapOpen => isMapOpen;

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

        foreach (var zone in zones)
        {
            if (zone.teleportButton != null)
            {
                zone.teleportButton.gameObject.SetActive(false);
                zone.teleportButton.onClick.RemoveAllListeners();
                
                MapZone currentZone = zone; 
                zone.teleportButton.onClick.AddListener(() => OnTeleportButtonClicked(currentZone));
            }
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M) && !LLMChatManager.Instance.IsChatting)
            ToggleMap();

        if (activeMarker != null && isMapOpen)
        {
            float offsetY = Mathf.Sin(Time.time * bounceSpeed) * bounceHeight;
            activeMarker.anchoredPosition = activeMarkerBase + new Vector2(0f, offsetY);

            if (locationLabel != null)
                locationLabel.rectTransform.anchoredPosition = activeMarker.anchoredPosition + locationLabelOffset;
        }
    }

  
    public void OpenMapForTeleport()
    {
        isTeleportMode = true;
        
        if (!isMapOpen) ToggleMap();
        else UpdateTeleportButtons(); 
        
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) player.GetComponent<PlayerMovement>()?.SetMovementLock(true);
    }

    private void UpdateTeleportButtons()
    {
        foreach (var zone in zones)
        {
            if (zone.teleportButton != null)
            {
                if (isTeleportMode && isMapOpen)
                {
                    bool isDiscovered = SaveManager.Instance != null && 
                                        SaveManager.Instance.currentSaveData.discoveredZones != null && 
                                        SaveManager.Instance.currentSaveData.discoveredZones.Contains(zone.zoneName);
                    
                    zone.teleportButton.gameObject.SetActive(isDiscovered);
                }
                else
                {
                    zone.teleportButton.gameObject.SetActive(false);
                }
            }
        }
    }

    private void OnTeleportButtonClicked(MapZone zone)
    {
        if (!isTeleportMode) return;
        if (string.IsNullOrEmpty(zone.sceneName) || zone.sceneName == SceneManager.GetActiveScene().name) return;

        MapSceneTransitionState.BeginTransition(2f, "PORTAL_TRAVEL");
        
        ToggleMap(); 
        SceneManager.LoadScene(zone.sceneName);
    }

    public void ToggleMap()
    {
        SetMapOpen(!isMapOpen);
    }

    public void OpenMap()
    {
        SetMapOpen(true);
    }

    public void CloseMap()
    {
        SetMapOpen(false);
    }

    public void SetMapOpen(bool visible)
    {
        isMapOpen = visible;

        if (!isMapOpen) 
        {
            isTeleportMode = false;
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) player.GetComponent<PlayerMovement>()?.SetMovementLock(false);
        }

        Canvas c = this.GetComponent<Canvas>();
        c.sortingOrder = isMapOpen ? 100 : 0; 

        worldMapPanel.SetActive(isMapOpen);
        minimapPanel.SetActive(!isMapOpen);   

        if (!isMapOpen && playerPin != null)
            playerPin.anchoredPosition = activeMarkerBase;

        UpdateTeleportButtons(); 
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

                if (SaveManager.Instance != null)
                {
                    SaveManager.Instance.currentSaveData.discoveredZones ??= new List<string>();

                    if (!SaveManager.Instance.currentSaveData.discoveredZones.Contains(zoneName))
                        SaveManager.Instance.currentSaveData.discoveredZones.Add(zoneName);

                    // Force immediate disk write — don't wait for autosave
                    SaveManager.Instance.ForceSaveNow();
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