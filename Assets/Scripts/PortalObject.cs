using UnityEngine;

public class PortalObject : MonoBehaviour
{
    [Header("Cài đặt Tế Đàn")]
    public float interactDistance = 3f;
    public GameObject interactHint; 

    private Transform playerTransform;

    void Update()
    {
        if (playerTransform == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
            return;
        }

        float dist = Vector2.Distance(transform.position, playerTransform.position);

        if (interactHint != null)
            interactHint.SetActive(dist <= interactDistance);

        if (dist <= interactDistance && Input.GetKeyDown(KeyCode.F))
        {
            // Bắt buộc lưu dữ liệu các chỉ số trước khi mở map để chuẩn bị chuyển scene
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.ForceSaveNow();
            }

            if (WorldMapManager.Instance != null)
            {
                WorldMapManager.Instance.OpenMapForTeleport();
            }

            if (QuestManager.instance != null)
            {
                QuestManager.instance.OnPlayerTeleported();
            }
        }
    }
}