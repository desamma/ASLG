using UnityEngine;
using System;
using System.Collections.Generic;

public class DailyQuestManager : MonoBehaviour
{
    public static DailyQuestManager Instance;

    [Header("Cấu hình")]
    [SerializeField] private List<QuestSO> allDailyQuestPool; // Danh sách tất cả Daily Quests hiện có
    [SerializeField] private int dailyQuestLimit = 3;        // Mỗi ngày cho tối đa 3 cái

    private const string LAST_RESET_KEY = "LastDailyResetTick";

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        CheckAndResetDailyQuests();
    }

    public void CheckAndResetDailyQuests()
    {
        string lastResetStr = PlayerPrefs.GetString(LAST_RESET_KEY, string.Empty);
        DateTime now = DateTime.Now;

        if (string.IsNullOrEmpty(lastResetStr))
        {
            // Lần đầu chơi game
            ResetQuests(now);
            return;
        }

        DateTime lastReset = DateTime.Parse(lastResetStr);

        // Kiểm tra xem đã sang ngày mới chưa (so sánh phần Date)
        if (now.Date > lastReset.Date)
        {
            ResetQuests(now);
        }
    }

    private void ResetQuests(DateTime resetTime)
    {
        Debug.Log("<color=yellow>[Daily] Đã sang ngày mới! Đang reset nhiệm vụ hằng ngày...</color>");

        if (QuestManager.instance == null) return;

        // 1. Xóa các Daily Quest đã hoàn thành trong QuestManager
        QuestManager.instance.completedQuests.RemoveAll(q => q.questType == QuestType.Daily);

        // 2. Xóa các Daily Quest đang làm dở (tùy bạn muốn giữ hay xóa)
        QuestManager.instance.activeQuests.RemoveAll(q => q.questType == QuestType.Daily);

        // 3. Chọn ngẫu nhiên nhiệm vụ mới từ Pool
        List<QuestSO> newDailies = PickRandomDailies(dailyQuestLimit);
        foreach (var q in newDailies)
        {
            QuestManager.instance.AcceptQuest(q);
        }

        // 4. Lưu lại thời gian reset
        PlayerPrefs.SetString(LAST_RESET_KEY, resetTime.ToString());
        PlayerPrefs.Save();

        // Báo UI vẽ lại
        QuestManager.instance.NotifyUIUpdate();
    }

    private List<QuestSO> PickRandomDailies(int count)
    {
        List<QuestSO> pool = new List<QuestSO>(allDailyQuestPool);
        List<QuestSO> result = new List<QuestSO>();

        for (int i = 0; i < count && pool.Count > 0; i++)
        {
            int index = UnityEngine.Random.Range(0, pool.Count);
            result.Add(pool[index]);
            pool.RemoveAt(index);
        }
        return result;
    }
}