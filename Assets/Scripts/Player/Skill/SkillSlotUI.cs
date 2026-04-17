using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillSlotUI : MonoBehaviour
{
    [Header("Backend Reference")]
    [SerializeField] private PlayerSkill playerSkill;

    [Header("UI Elements")]
    [SerializeField] private Sprite skillBorderTier0;
    [SerializeField] private Sprite skillBorderTier1;
    [SerializeField] private Sprite skillBorderTier2;
    [SerializeField] private Sprite skillBorderTier3;
    [SerializeField] private Image skillBorder;
    [SerializeField] private Image skillIcon;
    [SerializeField] private Image cooldownOverlay;
    [SerializeField] private TextMeshProUGUI cooldownText;

    private void Start()
    {
        RefreshSkillUI();
    }

    private void Update()
    {
        if (playerSkill == null) return;

        cooldownOverlay.fillAmount = playerSkill.CooldownFraction;
        cooldownOverlay.enabled = playerSkill.CooldownFraction > 0f;

        if (cooldownText != null)
        {
            cooldownText.text = Mathf.Ceil(playerSkill.CooldownTimer).ToString();
            cooldownText.enabled = playerSkill.CooldownFraction > 0f;
        }
    }

    public void RefreshSkillUI()
    {
        if (playerSkill == null) return;

        if (playerSkill.SkillIcon != null)
        {
            skillIcon.sprite = playerSkill.SkillIcon;
            cooldownOverlay.sprite = playerSkill.SkillIcon;
        }

        switch (playerSkill.CurrentTier)
        {
            case 0:
                skillBorder.sprite = skillBorderTier0;
                break;
            case 1:
                skillBorder.sprite = skillBorderTier1;
                break;
            case 2:
                skillBorder.sprite = skillBorderTier2;
                break;
            case 3:
                skillBorder.sprite = skillBorderTier3;
                break;
        }
    }
}
