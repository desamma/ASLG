using UnityEngine;


public class InventoryToggle : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
            InventoryUI.instance?.Toggle();
    }
}