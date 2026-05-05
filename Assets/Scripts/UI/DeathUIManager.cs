using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DeathUIManager : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Kéo Canvas Group của màn hình báo tử vào đây")]
    public CanvasGroup deathCanvasGroup;

    void Start()
    {
        if (deathCanvasGroup != null)
        {
            deathCanvasGroup.alpha = 0f;
            deathCanvasGroup.blocksRaycasts = false; 
        }

        if (StatsManager.instance != null)
        {
            StatsManager.instance.OnPlayerDeathEvent += TriggerDeathScene;
        }
    }

    void OnDestroy()
    {
        if (StatsManager.instance != null)
        {
            StatsManager.instance.OnPlayerDeathEvent -= TriggerDeathScene;
        }
    }

    private void TriggerDeathScene()
    {
        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        Time.timeScale = 0f;

        if (deathCanvasGroup != null) deathCanvasGroup.blocksRaycasts = true;

        float fadeDuration = 2f;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            if (deathCanvasGroup != null)
            {
                deathCanvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
            }
            yield return null;
        }

        if (deathCanvasGroup != null) deathCanvasGroup.alpha = 1f;

        yield return new WaitForSecondsRealtime(5f);

        if (StatsManager.instance != null)
        {
            Destroy(StatsManager.instance.gameObject);
        }

        Time.timeScale = 1f;

        SceneManager.LoadScene("Start");
    }
}