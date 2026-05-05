using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Ensures Physics2D colliders (especially tilemap colliders) are properly initialized after scene loads.
/// This fixes an issue where tilemap boundaries don't work correctly in builds after scene transitions.
/// 
/// Root Cause: After loading a new scene, the Physics2D system needs to rebake/refresh colliders,
/// particularly tilemap colliders. Without this refresh, collision detection can fail intermittently.
/// </summary>
public class Physics2DInitializer : MonoBehaviour
{
    private static Physics2DInitializer instance;

    private void Awake()
    {
        // Ensure only one instance exists
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        // Register for scene load events
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Small delay to allow scene to fully initialize before refreshing Physics2D
        StartCoroutine(RefreshPhysics2DCoroutine());
    }

    private System.Collections.IEnumerator RefreshPhysics2DCoroutine()
    {
        // Wait one frame for scene objects to initialize
        yield return null;

        // Refresh Physics2D by temporarily enabling/disabling physics simulation
        // This forces Unity to rebake all colliders, especially tilemap colliders
        Physics2D.Simulate(0f);

        // Alternative: Force all colliders to update their state
        Collider2D[] allColliders = FindObjectsOfType<Collider2D>();
        foreach (Collider2D collider in allColliders)
        {
            if (collider != null && collider.isActiveAndEnabled)
            {
                // Force the collider to refresh by toggling its enabled state
                collider.enabled = false;
                collider.enabled = true;
            }
        }
    }
}
