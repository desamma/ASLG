using UnityEngine;
using UnityEngine.SceneManagement;


public class InventoryToggle : MonoBehaviour
{
    private void Start()
    {
        DontDestroyOnLoad(this.gameObject);
    }

    

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.B) && SceneManager.GetActiveScene() != SceneManager.GetSceneByName("Start"))
            InventoryUI.instance.Toggle();
    }
}