using UnityEngine;
using System.IO;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

[System.Serializable]
public class GameSaveData
{
    public string playerName;
    public string playerClass;
    public Vector3 playerPosition;
    public string sceneName;

    // Alicia
    public bool isAliciaActive;
    public Vector3 aliciaPosition;
    public int relationshipScore;
}

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance;
    public GameSaveData currentSaveData = new GameSaveData();
    private string saveFilePath;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoCreateSaveManager()
    {
        GameObject go = new GameObject("SaveManager_System");
        Instance = go.AddComponent<SaveManager>();
        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        saveFilePath = Application.persistentDataPath + "/savegame.json";
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this) SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public static bool HasSaveFile()
    {
        return File.Exists(Application.persistentDataPath + "/savegame.json");
    }

    public void SaveGame()
    {
        currentSaveData.sceneName = SceneManager.GetActiveScene().name;

        // Lưu Player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) currentSaveData.playerPosition = player.transform.position;
        
        if (StatsManager.instance != null) currentSaveData.playerName = StatsManager.instance.playerName;

        // Lưu Alicia & AI Quest
        LLMChatManager chatManager = FindObjectOfType<LLMChatManager>();
        if (chatManager != null)
        {
            if (chatManager.aliciaScript != null && chatManager.aliciaScript.gameObject.activeInHierarchy)
            {
                currentSaveData.isAliciaActive = true;
                currentSaveData.aliciaPosition = chatManager.aliciaScript.transform.position;
                currentSaveData.relationshipScore = chatManager.aliciaScript.relationshipScore;
            }
            else currentSaveData.isAliciaActive = false;
        }

        string json = JsonUtility.ToJson(currentSaveData, true);
        File.WriteAllText(saveFilePath, json);
        Debug.Log($"<color=cyan>[SaveManager] Auto-Saved to: {saveFilePath}</color>");
    }

    public void LoadGame()
    {
        if (HasSaveFile())
        {
            string json = File.ReadAllText(saveFilePath);
            currentSaveData = JsonUtility.FromJson<GameSaveData>(json);
            Debug.Log("<color=cyan>[SaveManager] Loaded file, transitioning scene...</color>");
            
            if (StatsManager.instance != null) StatsManager.instance.playerName = currentSaveData.playerName;
            SceneManager.LoadScene(currentSaveData.sceneName);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Bật AutoSave nếu ở Game
        if (scene.name == "Game" || scene.buildIndex > 0)
        {
            StopCoroutine(nameof(AutoSaveRoutine));
            StartCoroutine(nameof(AutoSaveRoutine));
        }
        StartCoroutine(ApplySaveDataRoutine());
    }

    private IEnumerator ApplySaveDataRoutine()
    {
        yield return new WaitForEndOfFrame(); // Đợi 1 frame cho PlayerSpawner kịp sinh ra nhân vật

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && currentSaveData.playerPosition != Vector3.zero) player.transform.position = currentSaveData.playerPosition;

        LLMChatManager chatManager = FindObjectOfType<LLMChatManager>();
        if (chatManager != null)
        {
            if (chatManager.aliciaScript != null && currentSaveData.isAliciaActive)
            {
                chatManager.aliciaScript.transform.position = currentSaveData.aliciaPosition;
                chatManager.aliciaScript.relationshipScore = currentSaveData.relationshipScore;
            }
        }
    }

    private IEnumerator AutoSaveRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(10f); // Lưu mỗi 10 giây
            SaveGame();
        }
    }
}