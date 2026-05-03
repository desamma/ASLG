using System;
using UnityEngine;

public class EnemyQuestTarget : MonoBehaviour
{
    [Header("Quest Target Settings")]
    [Tooltip("Chọn loại quái vật từ danh sách Enum (VD: Slime).")]
    public EnemyType myType;

    public static event Action<EnemyType> OnEnemyDied;

    private bool isApplicationQuitting = false;

    private void OnApplicationQuit()
    {
        isApplicationQuitting = true;
    }

    /// <summary>
    /// Hàm của Unity: Tự động chạy khi gameObject bị xóa (Destroy)
    /// </summary>
    private void OnDestroy()
    {
        if (!isApplicationQuitting && gameObject.scene.isLoaded)
        {
            NotifyDeath();
        }
    }

    private void NotifyDeath()
    {
        if (myType == EnemyType.None)
        {
            Debug.LogWarning($"[Quest System] Quái '{gameObject.name}' bị tiêu diệt nhưng chưa được gắn EnemyType!");
            return;
        }

        // Phát thông báo cho QuestManager biết
        OnEnemyDied?.Invoke(myType);
        Debug.Log($"[Quest System] Đã tự động báo tử: {myType}");
    }
}