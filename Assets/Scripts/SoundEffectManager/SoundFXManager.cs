using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A singleton manager that handles the playback of sound effects (SFX) in the game.
/// </summary>
public class SoundFXManager : MonoBehaviour
{
    public static SoundFXManager Instance;

    [SerializeField] private AudioSource soundFXObject;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Play a given AudioClip at the specified Transform's position with the specified volume.
    /// </summary>
    /// <param name="audioClip">selected audio clip</param>
    /// <param name="spawnTransform">spawn position</param>
    /// <param name="volume">volume level</param>
    public void PlaySoundFXClip(AudioClip audioClip, Transform spawnTransform, float volume)
    {
        var audioSource = Instantiate(soundFXObject, spawnTransform.position, Quaternion.identity);

        audioSource.clip = audioClip;
        audioSource.volume = volume;
        float clipLength = audioSource.clip.length;

        audioSource.Play();
        Destroy(audioSource.gameObject, clipLength);
    }

    /// <summary>
    /// Play a given AudioClip at the specified position with the specified volume.
    /// </summary>
    /// <param name="audioClip">selected audio clip</param>
    /// <param name="spawnPosition">spawn position</param>
    /// <param name="volume">volume level</param>
    public void PlaySoundFXClip(AudioClip audioClip, Vector3 spawnPosition, float volume)
    {
        var audioSource = Instantiate(soundFXObject, spawnPosition, Quaternion.identity);

        audioSource.clip = audioClip;
        audioSource.volume = volume;
        float clipLength = audioSource.clip.length;

        audioSource.Play();
        Destroy(audioSource.gameObject, clipLength);
    }

    /// <summary>
    ///  Play a random AudioClip from the provided array at the specified Transform's position with the specified volume.
    /// </summary>
    /// <param name="audioClips">Audio Arrays</param>
    /// <param name="spawnTransform">spawn position</param>
    /// <param name="volume">volume level</param>
    public void PlayRandomSoundFXClips(AudioClip[] audioClips, Transform spawnTransform, float volume)
    {
        int rand = Random.Range(0, audioClips.Length);
        var audioSource = Instantiate(soundFXObject, spawnTransform.position, Quaternion.identity);

        audioSource.clip = audioClips[rand];
        audioSource.volume = volume;
        float clipLength = audioSource.clip.length;

        audioSource.Play();
        Destroy(audioSource.gameObject, clipLength);
    }

    /// <summary>
    ///  Play a random AudioClip from the provided array at the specified Transform's position with the specified volume.
    /// </summary>
    /// <param name="audioClips">Audio List</param>
    /// <param name="spawnTransform">spawn position</param>
    /// <param name="volume">volume level</param>
    public void PlayRandomSoundFXClips(List<AudioClip> audioClips, Transform spawnTransform, float volume)
    {
        int rand = Random.Range(0, audioClips.Count);
        var audioSource = Instantiate(soundFXObject, spawnTransform.position, Quaternion.identity);

        audioSource.clip = audioClips[rand];
        audioSource.volume = volume;
        float clipLength = audioSource.clip.length;

        audioSource.Play();
        Destroy(audioSource.gameObject, clipLength);
    }

    /// <summary>
    ///  Play a looping AudioClip at the specified Transform's position with the specified volume.
    /// </summary>
    /// <param name="audioClip">selected audio clip</param>
    /// <param name="spawnTransform">spawn position</param>
    /// <param name="volume">volume level</param>
    /// <returns>Returns the AudioSource playing the clip, so it can be stopped later if needed.</returns>
    public AudioSource PlayLoopingSoundFXClip(AudioClip audioClip, Transform spawnTransform, float volume)
    {
        var audioSource = Instantiate(soundFXObject, spawnTransform.position, Quaternion.identity);
        audioSource.clip = audioClip;
        audioSource.volume = volume;
        audioSource.loop = true;
        audioSource.Play();
        return audioSource;
    }

    /// <summary>
    ///  Play a looping AudioClip at the specified Transform's position with the specified volume and 3D spatial settings.
    /// </summary>
    /// <param name="audioClip">selected audio clip</param>
    /// <param name="spawnTransform">spawn position</param>
    /// <param name="volume">volume level</param>
    /// <param name="minDistance">minimum distance at which sound starts to attenuate</param>
    /// <param name="maxDistance">maximum distance at which sound is inaudible</param>
    /// <returns>Returns the AudioSource playing the clip, so it can be stopped later if needed.</returns>
    public AudioSource PlayLoopingSoundFXClip(AudioClip audioClip, Transform spawnTransform, float volume, float? minDistance, float? maxDistance)
    {
        var audioSource = Instantiate(soundFXObject, spawnTransform.position, Quaternion.identity);
        audioSource.clip = audioClip;
        audioSource.volume = volume;
        audioSource.loop = true;
        audioSource.spatialBlend = 1f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.minDistance = minDistance ?? 1f;
        audioSource.maxDistance = maxDistance ?? 500f;
        audioSource.Play();
        return audioSource;
    }

    /// <summary>
    /// Stops and destroys the given AudioSource.
    /// </summary>
    /// <param name="audioSource">The AudioSource to stop and destroy</param>
    public void StopAndDestroyAudioSource(AudioSource audioSource)
    {
        if (audioSource != null)
        {
            audioSource.Stop();
            Destroy(audioSource.gameObject);
        }
    }
}
