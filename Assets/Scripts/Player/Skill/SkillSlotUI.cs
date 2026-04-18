using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillSlotUI : MonoBehaviour
{
    [Header("Backend Reference")]
    [SerializeField] private PlayerSkill playerSkill;
    [SerializeField] private GameObject playerGameObject;

    [Header("Upgrade Skill Button")]
    [SerializeField] private Button upgradeSkillButton;
    [SerializeField] private TextMeshProUGUI upgradeSkillText;

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
        playerGameObject = GameObject.FindGameObjectWithTag("Player");

        if (playerGameObject == null)
        {
            Debug.LogError("Error: Could not find any GameObject with the tag 'Player'. Stopping UI setup.");
            return;
        }

        playerSkill = playerGameObject.GetComponent<PlayerSkill>();

        if (playerSkill == null)
        {
            Debug.LogError("Error: Found the 'Player' object, but it does NOT have the PlayerSkill script attached!");
            return;
        }

        if (upgradeSkillButton != null && upgradeSkillText != null)
        {
            upgradeSkillButton.onClick.AddListener(OnSkillClickUpgrade);

            upgradeSkillButton.enabled = true;
            upgradeSkillButton.interactable = true;

            upgradeSkillText.text = "Upgrade skill";
        }


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

    private void OnSkillClickUpgrade()
    {
        if (playerSkill.TryUpgrade())
        {
            if (playerSkill.CurrentTier == 3)
            {
                upgradeSkillButton.enabled = false;
                upgradeSkillButton.interactable = false;
                upgradeSkillText.text = "Your skill level is maxed";
            }
            RefreshSkillUI();
            Debug.Log("Player skill upgrade successfully");
        }
        else
        {
            Debug.Log("Player skill upgrade failed");
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
