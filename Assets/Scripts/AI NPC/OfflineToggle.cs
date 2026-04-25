using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OfflineToggle : MonoBehaviour
{
    public TMP_Text buttonText;
    public Color offlineColor = Color.green;
    public Color onlineColor = Color.white;

    public void ToggleOfflineMode()
    {
        GameSettings.IsOfflineMode = !GameSettings.IsOfflineMode;
        
        if (buttonText != null)
        {
            buttonText.text = GameSettings.IsOfflineMode ? "MODE: OFFLINE" : "MODE: ONLINE";
            buttonText.color = GameSettings.IsOfflineMode ? offlineColor : onlineColor;
        }

        Debug.Log($"<color=yellow>[System]</color> Chế độ Offline: {GameSettings.IsOfflineMode}");
    }
}