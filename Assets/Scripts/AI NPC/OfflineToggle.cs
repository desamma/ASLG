using TMPro;
using UnityEngine;

public class OfflineToggle : MonoBehaviour
{
    public TMP_Text buttonText;
    public Color offlineColor = Color.green;
    public Color onlineColor = Color.white;

    private void OnEnable()
    {
        RefreshVisualState();
    }

    public void ToggleOfflineMode()
    {
        GameSettings.SetOfflineMode(!GameSettings.IsOfflineMode);
        RefreshVisualState();
    }

    private void RefreshVisualState()
    {
        if (buttonText == null)
        {
            return;
        }

        buttonText.text = GameSettings.IsOfflineMode ? "MODE: OFFLINE" : "MODE: ONLINE";
        buttonText.color = GameSettings.IsOfflineMode ? offlineColor : onlineColor;
    }
}
