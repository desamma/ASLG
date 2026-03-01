using UnityEngine;

public class DestroyAfterSeconds : MonoBehaviour
{
    [SerializeField] private float seconds;
    private void Start()
    {
        Destroy(gameObject, seconds);
    }
}
