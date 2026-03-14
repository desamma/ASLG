using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class CharacterCreation : MonoBehaviour
{
    public TMP_InputField nameInput;

    public void SelectFighter()
    {
        if (string.IsNullOrWhiteSpace(nameInput.text)) return;

        GameSession.PlayerName = nameInput.text;
        GameSession.PlayerClass = 1; // Đấu Sĩ
        SceneManager.LoadScene(2); // ID của Scene Game
    }

    public void SelectSummoner()
    {
        if (string.IsNullOrWhiteSpace(nameInput.text)) return;

        GameSession.PlayerName = nameInput.text;
        GameSession.PlayerClass = 2; // Summoner
        SceneManager.LoadScene(2); // ID của Scene Game
    }
}