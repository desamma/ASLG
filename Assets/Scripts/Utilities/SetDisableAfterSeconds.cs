using System.Collections;
using UnityEngine;

public class SetDisableAfterSeconds : MonoBehaviour
{
    [SerializeField] private float seconds = 1f;
    private Coroutine disableCoroutine;

    private void OnEnable()
    {
        if (disableCoroutine != null)
        {
            StopCoroutine(disableCoroutine);
        }

        disableCoroutine = StartCoroutine(StartDisableAfterSeconds());
    }

    private void OnDisable()
    {
        if (disableCoroutine != null)
        {
            StopCoroutine(disableCoroutine);
            disableCoroutine = null;
        }
    }
    private IEnumerator StartDisableAfterSeconds()
    {
        yield return new WaitForSeconds(seconds);
        if (gameObject.activeInHierarchy)
        {
            gameObject.SetActive(false);
        }
    }
}

