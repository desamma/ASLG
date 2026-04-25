using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Respawn : MonoBehaviour
{
    [SerializeField] private CanvasGroup respawnCanvasGroup;
    [SerializeField] private Button respawnButton;

    [SerializeField] private float fadeDuration = 2f;

    private void Awake()
    {
        if (FindObjectsOfType<Respawn>().Length > 1)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(this.gameObject);

    }

    private void Start()
    {
        if (StatsManager.instance != null && respawnCanvasGroup.alpha == 0f)
        {
            StatsManager.instance.OnPlayerDeathEvent += ShowRespawnUI;
        }
        respawnButton.onClick.AddListener(OnRespawnButtonClicked);
        HideRespawnUI();
    }

    private void OnDestroy()
    {
        if (StatsManager.instance != null)
        {
            StatsManager.instance.OnPlayerDeathEvent -= ShowRespawnUI;
        }
    }

    public void ShowRespawnUI()
    {
        StartCoroutine(FadeIn());
    }

    private IEnumerator FadeIn()
    {
        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            respawnCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        respawnCanvasGroup.alpha = 1f;
        respawnCanvasGroup.interactable = true;
        respawnCanvasGroup.blocksRaycasts = true;
    }

    private void HideRespawnUI()
    {
        respawnCanvasGroup.alpha = 0f;
        respawnCanvasGroup.interactable = false;
        respawnCanvasGroup.blocksRaycasts = false;
    }

    private void OnRespawnButtonClicked()
    {
        Debug.Log("Respawning...");
        HideRespawnUI();
        SaveManager.Instance.LoadGame();
    }
}
