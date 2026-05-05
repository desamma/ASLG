using UnityEngine;

public class GameBootstrapper : MonoBehaviour
{
    private static bool isInitialized = false;

    private void Awake()
    {
        if (!isInitialized)
        {
            ItemDatabase.Initialize();
            isInitialized = true;
            
            DontDestroyOnLoad(gameObject); 
        }
        else
        {
            Destroy(gameObject);
        }
    }
}