using System.Collections;
using UnityEngine;

public class SetDisableAfterSeconds : MonoBehaviour
{
    [SerializeField] private float seconds = 1f;
    private void Start()
    {
        StartCoroutine(StartDisableAfterSeconds());
    }
    private IEnumerator StartDisableAfterSeconds()
    {
        yield return new WaitForSeconds(seconds);
        gameObject.SetActive(false);
    }
}

