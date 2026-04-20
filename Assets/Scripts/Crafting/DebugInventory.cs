using UnityEngine;

public class DebugInventory : MonoBehaviour
{
    public ItemData[] testItems;
    public int testQuantity = 10;

    private void Start()
    {
        var inv = PlayerInventory.Instance;
        if (inv == null) return;
        foreach (var item in testItems)
            inv.AddItem(item, testQuantity);
    }
}