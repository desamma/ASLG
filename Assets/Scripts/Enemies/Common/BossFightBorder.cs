using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossFightBorder : MonoBehaviour
{
    [Header("Enemies")]
    [SerializeField] private List<GameObject> enemies = new List<GameObject>();

    [Header("Border")]
    [SerializeField] private List<GameObject> borderObjects = new List<GameObject>();
    [SerializeField] private float checkInterval = 1f;

    private Coroutine checkRoutine;

    private void OnEnable()
    {
        ApplyBorderState(IsAnyEnemyAlive());
        checkRoutine = StartCoroutine(CheckEnemiesRoutine());
    }

    private void OnDisable()
    {
        if (checkRoutine != null)
        {
            StopCoroutine(checkRoutine);
            checkRoutine = null;
        }
    }

    public void SetEnemies(List<GameObject> newEnemies)
    {
        enemies = newEnemies ?? new List<GameObject>();
    }

    public void AddEnemy(GameObject enemy)
    {
        if (enemy == null)
            return;

        enemies ??= new List<GameObject>();

        if (!enemies.Contains(enemy))
            enemies.Add(enemy);
    }

    private IEnumerator CheckEnemiesRoutine()
    {
        var wait = new WaitForSeconds(checkInterval);

        while (true)
        {
            ApplyBorderState(IsAnyEnemyAlive());
            yield return wait;
        }
    }

    private bool IsAnyEnemyAlive()
    {
        if (enemies == null || enemies.Count == 0)
            return false;

        for (int i = 0; i < enemies.Count; i++)
        {
            var enemy = enemies[i];
            if (enemy != null && enemy.activeInHierarchy)
                return true;
        }

        return false;
    }

    private void ApplyBorderState(bool anyEnemyAlive)
    {
        bool closeBorder = anyEnemyAlive;

        for (int i = 0; i < borderObjects.Count; i++)
        {
            var borderObject = borderObjects[i];
            if (borderObject != null)
                borderObject.SetActive(closeBorder);
        }
    }
}
