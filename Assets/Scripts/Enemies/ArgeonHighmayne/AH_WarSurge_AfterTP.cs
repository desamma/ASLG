using System;
using UnityEngine;

public class AH_WarSurge_AfterTP : MonoBehaviour
{
    [SerializeField] private float destroyTime = 2f;

    private void Start()
    {
        Destroy(gameObject, destroyTime);
    }
}
