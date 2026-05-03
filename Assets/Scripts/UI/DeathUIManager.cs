using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DeathUIManager : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Kéo Canvas Group của màn hình báo tử vào đây")]
    public CanvasGroup deathCanvasGroup;

    void Start()
    {
        // Đảm bảo lúc bắt đầu game, màn hình báo tử hoàn toàn tàng hình
        if (deathCanvasGroup != null)
        {
            deathCanvasGroup.alpha = 0f;
            deathCanvasGroup.blocksRaycasts = false; // Không chặn thao tác chuột của người chơi
        }

        // Lắng nghe tiếng gọi tử thần từ StatsManager
        if (StatsManager.instance != null)
        {
            StatsManager.instance.OnPlayerDeathEvent += TriggerDeathScene;
        }
    }

    void OnDestroy()
    {
        // Hủy lắng nghe khi object bị xóa để tránh lỗi RAM
        if (StatsManager.instance != null)
        {
            StatsManager.instance.OnPlayerDeathEvent -= TriggerDeathScene;
        }
    }

    private void TriggerDeathScene()
    {
        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        // 1. NGƯNG ĐỌNG THỜI GIAN NGAY LẬP TỨC (Boss, Quái, Đạn... sẽ đứng im)
        Time.timeScale = 0f;

        // Bật lớp chặn chuột
        if (deathCanvasGroup != null) deathCanvasGroup.blocksRaycasts = true;

        // 2. HIỆN DẦN LÊN TRONG 2 GIÂY (Dùng unscaledDeltaTime để vượt qua timeScale = 0)
        float fadeDuration = 2f;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            // Thay vì Time.deltaTime, ta dùng Time.unscaledDeltaTime
            elapsed += Time.unscaledDeltaTime;
            if (deathCanvasGroup != null)
            {
                deathCanvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
            }
            yield return null;
        }

        if (deathCanvasGroup != null) deathCanvasGroup.alpha = 1f;

        // 3. GIỮ NGUYÊN HIỆN TRƯỜNG Ở ĐÓ 5 GIÂY (Dùng WaitForSecondsRealtime)
        yield return new WaitForSecondsRealtime(5f);

        // 4. TIÊU DIỆT CÁI XÁC CŨ TRƯỚC KHI VỀ MENU (Chống bóng ma)
        if (StatsManager.instance != null)
        {
            Destroy(StatsManager.instance.gameObject);
        }

        // 5. CỰC KỲ QUAN TRỌNG: RÃ ĐÔNG THỜI GIAN TRƯỚC KHI CHUYỂN SCENE
        // Nếu quên dòng này, bạn về Menu Start game vẫn sẽ bị đóng băng!
        Time.timeScale = 1f;

        // 6. ĐÁ VỀ SCENE START (ID = 0)
        SceneManager.LoadScene("Start");
    }
}