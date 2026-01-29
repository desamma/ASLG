using UnityEngine;

public class Enemy_Pax_Effect : MonoBehaviour
{
    [SerializeField] private float destroyTime = 0.3f;

    private void Start()
    {
        Destroy(gameObject, destroyTime);
    }
}

