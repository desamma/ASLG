using UnityEngine;
using UnityEngine.UI;

public class PlayerUIManager : MonoBehaviour
{
    [Header("UI Components")]
    [Tooltip("Kéo Slider thanh máu của Player vào đây")]
    public Slider healthSlider;

    private void Start()
    {
        // Cập nhật máu ngay lúc vừa vào game
        UpdateHealthUI();

        // Lắng nghe sự kiện: Bất cứ khi nào Stats thay đổi, tự động gọi hàm UpdateHealthUI
        if (StatsManager.instance != null)
        {
            StatsManager.instance.OnStatsChangedEvent += UpdateHealthUI;
        }
    }

    private void OnDestroy()
    {
        // Hủy lắng nghe khi chuyển scene hoặc tắt game để tránh lỗi bộ nhớ
        if (StatsManager.instance != null)
        {
            StatsManager.instance.OnStatsChangedEvent -= UpdateHealthUI;
        }
    }

    private void UpdateHealthUI()
    {
        if (healthSlider != null && StatsManager.instance != null)
        {
            // Cập nhật giá trị Max và Current cho Slider
            healthSlider.maxValue = StatsManager.instance.maxHealth;
            healthSlider.value = StatsManager.instance.currentHealth;
        }
    }
}