using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class CharacterCreation : MonoBehaviour
{
    public TMP_InputField nameInput;

    public void SelectKnight()
    {
        if (string.IsNullOrWhiteSpace(nameInput.text)) return;

        GameSession.PlayerName = nameInput.text;
        ClassManager.Instance.SelectClass(PlayerClass.Knight);
        
        SceneManager.LoadScene(2); // ID của Scene Game
    }

    public void SelectArcher()
    {
        if (string.IsNullOrWhiteSpace(nameInput.text)) return;

        GameSession.PlayerName = nameInput.text;
        ClassManager.Instance.SelectClass(PlayerClass.Archer);
        
        SceneManager.LoadScene(2); // ID của Scene Game
    }

    public void SelectRogue()
    {
        if (string.IsNullOrWhiteSpace(nameInput.text)) return;

        GameSession.PlayerName = nameInput.text;
        ClassManager.Instance.SelectClass(PlayerClass.Rogue);
        
        SceneManager.LoadScene(2); // ID của Scene Game
    }

    public void SelectSummoner()
    {
        if (string.IsNullOrWhiteSpace(nameInput.text)) return;

        GameSession.PlayerName = nameInput.text;
        ClassManager.Instance.SelectClass(PlayerClass.Summoner);
        
        SceneManager.LoadScene(2); // ID của Scene Game
    }
}