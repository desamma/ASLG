using System.Collections;
using UnityEngine;

public class KX_SummonCompanion : MonoBehaviour
{
    [SerializeField] private GameObject companionPrefab;
    [SerializeField] private float summonTime = 0.6f;
    [SerializeField] private float destroyTime = 3f;
    private Enemy_KaleosXaan_Movement bossMovement;

    private void Start()
    {
        bossMovement = FindObjectOfType<Enemy_KaleosXaan_Movement>();
        StartCoroutine(SummonCompanion());
        Destroy(gameObject, destroyTime);
    }

    private IEnumerator SummonCompanion()
    {
        yield return new WaitForSeconds(summonTime);
        var position = transform.position + new Vector3(0f, 1f, 0f);
        GameObject companion = Instantiate(companionPrefab, position, Quaternion.identity);

        if (bossMovement != null)
        {
            bossMovement.RegisterCompanion(companion);
        }
    }
}