using UnityEngine;

/// <summary>
/// GameBootstrap – khởi tạo tất cả database khi game start.
/// Attach vào GameObject đầu tiên load (Bootstrap scene hoặc GameManager).
/// Đảm bảo chạy trước InventoryManager và CraftingManager.
/// </summary>
public class GameBootstrap : MonoBehaviour
{
    private void Awake()
    {
        ItemDatabase.Initialize();
        RecipeDatabase.Initialize();

        Debug.Log("[Bootstrap] ItemDatabase + RecipeDatabase đã sẵn sàng.");
    }
}
