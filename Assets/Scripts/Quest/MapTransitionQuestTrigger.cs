using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class MapTransitionQuestTrigger : MonoBehaviour
{
    [Header("Cấu hình Nhiệm vụ")]
    [Tooltip("Kéo file Main Quest 1 (QuestSO) vào đây")]
    public QuestSO questToAdvance;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (QuestManager.instance != null && questToAdvance != null)
            {
                QuestManager.instance.AdvanceQuestByReference(questToAdvance);
            }
        }
    }
}