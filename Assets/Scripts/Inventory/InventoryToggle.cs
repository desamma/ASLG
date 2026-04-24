using UnityEngine;


public class InventoryToggle : MonoBehaviour
{
    private void Start()
    {
        DontDestroyOnLoad(this.gameObject);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
            InventoryUI.instance?.Toggle();
    }
}