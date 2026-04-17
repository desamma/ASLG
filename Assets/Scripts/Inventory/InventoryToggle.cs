using UnityEngine;


public class InventoryToggle : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
            InventoryUI.instance?.Toggle();
    }
}