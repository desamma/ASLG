using UnityEngine;
using UnityEngine.UI;

public class StatSlotUI : MonoBehaviour
{
    [SerializeField] private StatType statType;
    [SerializeField] private Button upgradeButton;

    private void Awake()
    {
        if (upgradeButton != null)
            upgradeButton.onClick.AddListener(OnClickUpgrade);
    }

    private void OnClickUpgrade()
    {
        StatsManager.instance.TryUpgradeStat(statType, 1);
    }
}