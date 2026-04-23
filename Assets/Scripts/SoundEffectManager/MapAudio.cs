using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages ambient/looping audio for a scene or a group of scenes.
/// </summary>
[RequireComponent(typeof(AudioSource))]
[DisallowMultipleComponent]
public class MapMusic : MonoBehaviour
{

    [Header("Audio Clips")]
    [SerializeField] private AudioClip defaultAudioClip;
    [SerializeField] private AudioClip onPlayingAudioClip;

    [Header("Volume Settings")]
    [SerializeField, Range(0f, 1f)] private float defaultVolume = 0.8f;
    [SerializeField, Range(0f, 1f)] private float onPlayingVolume = 1.0f;

    [Header("Fade Settings")]
    [SerializeField, Min(0f)] private float fadeOutDuration = 0.5f;
    [SerializeField, Min(0f)] private float fadeInDuration = 0.5f;

    [Header("Scene Persistence")]
    [Tooltip(
        "Enable to keep this audio alive across multiple scenes.\n" +
        "The GameObject will call DontDestroyOnLoad and survive scene transitions\n" +
        "as long as the loaded scene is listed in Registered Scenes.\n\n" +
        "Disable for single-scene audio that is destroyed with the scene.")]
    [SerializeField] private bool persistAcrossScenes = false;

    [Tooltip(
        "Unique name that identifies this audio group (e.g. 'Auth', 'Combat').\n" +
        "Used to prevent duplicate persistent instances.\n" +
        "Only relevant when Persist Across Scenes is enabled.")]
    [SerializeField] private string groupName = "Default";

    [Tooltip(
        "List of scene names where this audio should keep playing.\n" +
        "When the active scene is NOT on this list the audio fades out\n" +
        "and this GameObject is destroyed.\n" +
        "Only relevant when Persist Across Scenes is enabled.")]
    [SerializeField] private List<string> registeredScenes = new List<string>();

    /// <summary>
    /// Tracks one persistent MapAudio instance per group name.
    /// Cleared automatically when an instance is destroyed.
    /// </summary>
    private static readonly Dictionary<string, MapMusic> s_persistentInstances = new();

    private AudioSource _audioSource;
    private AudioClip _currentClip;
    private Coroutine _fadeCoroutine;
    private bool _pendingDestroy; // prevents double-destroy during fade

    /// <summary>Clip currently loaded (may or may not be playing).</summary>
    public AudioClip CurrentClip => _currentClip;

    /// <summary>Live volume of the AudioSource.</summary>
    public float CurrentVolume => _audioSource != null ? _audioSource.volume : 0f;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        ConfigureAudioSource(_audioSource);

        if (!persistAcrossScenes) return;

        //Duplicate guard
        if (s_persistentInstances.TryGetValue(groupName, out MapMusic existing) && existing != null)
        {
            Destroy(gameObject);
            return;
        }

        s_persistentInstances[groupName] = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        if (_pendingDestroy) return;
        PlayDefaultAudio();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        // Unregister only if this is still the tracked instance
        if (s_persistentInstances.TryGetValue(groupName, out MapMusic tracked) && tracked == this)
            s_persistentInstances.Remove(groupName);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (_pendingDestroy) return;

        bool isRegistered = registeredScenes.Contains(scene.name);

        if (!isRegistered)
        {
            _pendingDestroy = true;
            StartCoroutine(FadeOutAndDestroy());
        }
    }

    /// <summary>
    /// Transitions to the default idle audio clip with a crossfade.
    /// </summary>
    public void PlayDefaultAudio()
    {
        if (defaultAudioClip == null)
        {
            Debug.LogWarning($"[MapAudio:{groupName}] defaultAudioClip is not assigned.");
            return;
        }

        TransitionToClip(defaultAudioClip, defaultVolume);
    }

    /// <summary>
    /// Transitions to the active/on-playing audio clip with a crossfade.
    /// </summary>
    public void PlayOnPlayingAudio()
    {
        if (onPlayingAudioClip == null)
        {
            Debug.LogWarning($"[MapAudio:{groupName}] onPlayingAudioClip is not assigned.");
            return;
        }

        TransitionToClip(onPlayingAudioClip, onPlayingVolume);
    }

    /// <summary>
    /// Useful for contextual audio changes (e.g. entering a zone, boss fight).
    /// </summary>
    /// <param name="newClip">The clip to switch to. If null, defaults to defaultAudioClip.</param>
    /// <param name="targetVolume">Desired volume after fade-in (0–1).</param>
    public void ChangeAudio(AudioClip newClip = null, float targetVolume = 1f)
    {
        if (newClip == null)
        {
            PlayDefaultAudio();
            return;
        }

        TransitionToClip(newClip, Mathf.Clamp01(targetVolume));
    }

    /// <summary>
    /// Fades out and stops playback (does not destroy the GameObject).
    /// </summary>
    public void StopAudio()
    {
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = StartCoroutine(FadeOutAndStop(fadeOutDuration));
    }

    /// <summary>
    /// Immediately stops audio with no fade.
    /// </summary>
    public void StopAudioImmediate()
    {
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        _audioSource.Stop();
        _currentClip = null;
    }

    /// <summary>
    /// Registers an additional scene name at runtime so this audio group
    /// extends to cover it (e.g. dynamically unlocked level).
    /// </summary>
    public void RegisterScene(string sceneName)
    {
        if (!registeredScenes.Contains(sceneName))
            registeredScenes.Add(sceneName);
    }

    /// <summary>
    /// Removes a scene from the registered list at runtime.
    /// If the scene is currently active this will trigger a fade-out + destroy.
    /// </summary>
    public void UnregisterScene(string sceneName)
    {
        registeredScenes.Remove(sceneName);

        if (SceneManager.GetActiveScene().name == sceneName && !_pendingDestroy)
        {
            _pendingDestroy = true;
            StartCoroutine(FadeOutAndDestroy());
        }
    }

    private void TransitionToClip(AudioClip clip, float targetVolume)
    {
        if (_pendingDestroy) return;

        // Already playing this exact clip — nothing to do
        if (_currentClip == clip && _audioSource.isPlaying) return;

        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = StartCoroutine(CrossfadeToClip(clip, targetVolume));
    }

    private IEnumerator CrossfadeToClip(AudioClip newClip, float targetVolume)
    {
        // --- FADE OUT ---
        if (_audioSource.isPlaying && fadeOutDuration > 0f)
        {
            float startVol = _audioSource.volume;
            float elapsed = 0f;

            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                _audioSource.volume = Mathf.Lerp(startVol, 0f, elapsed / fadeOutDuration);
                yield return null;
            }
        }

        _audioSource.Stop();
        _audioSource.volume = 0f;

        // --- SWAP CLIP ---
        _currentClip = newClip;
        _audioSource.clip = newClip;
        _audioSource.loop = true;
        _audioSource.Play();

        // --- FADE IN ---
        if (fadeInDuration > 0f)
        {
            float elapsed = 0f;

            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                _audioSource.volume = Mathf.Lerp(0f, targetVolume, elapsed / fadeInDuration);
                yield return null;
            }
        }

        _audioSource.volume = targetVolume;
        _fadeCoroutine = null;
    }

    private IEnumerator FadeOutAndStop(float duration)
    {
        if (_audioSource.isPlaying && duration > 0f)
        {
            float startVol = _audioSource.volume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _audioSource.volume = Mathf.Lerp(startVol, 0f, elapsed / duration);
                yield return null;
            }
        }

        _audioSource.Stop();
        _audioSource.volume = 0f;
        _currentClip = null;
        _fadeCoroutine = null;
    }

    /// <summary>
    /// Fades out then destroys this entire GameObject.
    /// Used when leaving all registered scenes.
    /// </summary>
    private IEnumerator FadeOutAndDestroy()
    {
        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = null;
        }

        if (_audioSource.isPlaying && fadeOutDuration > 0f)
        {
            float startVol = _audioSource.volume;
            float elapsed = 0f;

            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                _audioSource.volume = Mathf.Lerp(startVol, 0f, elapsed / fadeOutDuration);
                yield return null;
            }
        }

        _audioSource.Stop();
        Destroy(gameObject);
    }

    private void ConfigureAudioSource(AudioSource source)
    {
        source.playOnAwake = false;
        source.loop = true;
        source.volume = 0f;
        source.spatialBlend = 0f; // 2D audio — heard throughout the map
    }
}