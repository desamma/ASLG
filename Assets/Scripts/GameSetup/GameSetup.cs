using UnityEngine;
using TMPro;

public class GameSetup : MonoBehaviour
{
    [Header("Player Settings")]
    public Transform playerTransform;
    public TMP_Text playerNameText;

    void Start()
    {
        if (playerNameText != null)
        {
            playerNameText.text = GameSession.PlayerName;
        }

        if (StatsManager.instance == null) return;

        if (GameSession.PlayerClass == 1)
        {
            StatsManager.instance.maxHealth *= 2;
            StatsManager.instance.currentHealth = StatsManager.instance.maxHealth;
            StatsManager.instance.damage *= 2;
            Debug.Log("<color=red>CLASS ĐẤU SĨ: Đã nhân đôi chỉ số!</color>");
        }
    }
}