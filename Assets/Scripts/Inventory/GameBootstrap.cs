using UnityEngine;

public class GameBootstrap : MonoBehaviour
{
    private void Awake()
    {
        RecipeDatabase.Initialize();

        Debug.Log("[Bootstrap] ItemDatabase + RecipeDatabase đã sẵn sàng.");
    }
}
