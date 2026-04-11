using System.Collections.Generic;
using UnityEngine;

public class ArrowPool : MonoBehaviour
{
    public static ArrowPool Instance { get; private set; }

    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private int initialSize = 64;

    private readonly Queue<ArrowProjectile> pool = new();

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); return; }

        Grow(initialSize);
    }

    private void Grow(int count)
    {
        for (int i = 0; i < count; i++)
        {
            var go = Instantiate(arrowPrefab, transform);
            var arrow = go.GetComponent<ArrowProjectile>();
            go.SetActive(false);
            pool.Enqueue(arrow);
        }
    }

    public ArrowProjectile Get()
    {
        // If pool is empty, grow on demand rather than dropping arrows silently
        if (pool.Count == 0)
        {
            Debug.LogWarning("[ArrowPool] Pool exhausted — growing by 16.");
            Grow(16);
        }

        var arrow = pool.Dequeue();
        arrow.gameObject.SetActive(true);
        return arrow;
    }

    public void Return(ArrowProjectile arrow)
    {
        arrow.gameObject.SetActive(false);
        arrow.transform.SetParent(transform);
        pool.Enqueue(arrow);
    }
}