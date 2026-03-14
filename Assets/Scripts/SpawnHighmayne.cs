using UnityEngine;

public class Spawnhighmayne : MonoBehaviour
{
    public GameObject highmayne;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Instantiate(highmayne, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }
}
