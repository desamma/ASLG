using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;
using System.Collections;
using System;

/// <summary>
/// Singleton tooltip panel that StatusEffectSlot shows/hides on hover.
///</summary>
public class StatusEffectTooltip : MonoBehaviour
{
    public static StatusEffectTooltip Instance { get; private set; }

    [Header("Panel References")]
    [SerializeField] private Image headerPanel;
    [SerializeField] private Image tooltipIconImage;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI statLinesText;
    [SerializeField] private TextMeshProUGUI flavourText;
    [SerializeField] private TextMeshProUGUI metaText;       // duration + stacks

    [Header("Layout")]
    [Tooltip("Pixel offset from the slot so the tooltip doesn't overlap it.")]
    [SerializeField] private Vector2 cursorOffset = new(12f, -12f);
    [Tooltip("Keep the tooltip this many pixels inside the screen edge.")]
    [SerializeField] private float screenPadding = 8f;

    [Header("Fade")]
    [SerializeField] private float fadeInDuration = 0.12f;
    [SerializeField] private float fadeOutDuration = 0.08f;

    //State
    private CanvasGroup _canvasGroup;
    private RectTransform _rectTransform;
    private Canvas _rootCanvas;
    private ActiveStatusEffect _currentEffect;
    private Coroutine _fadeCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        _canvasGroup = GetComponent<CanvasGroup>();
        _rectTransform = GetComponent<RectTransform>();
        _rootCanvas = GetComponentInParent<Canvas>();

        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // hide initially
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (_currentEffect == null)
            return;

        FollowCursor();

        Show();

        // Refresh duration / stacks each frame
        RefreshMetaLine();
    }

    /// <summary>
    /// Call from StatusEffectSlot.OnPointerEnter.
    /// </summary>
    public void BeginHover(ActiveStatusEffect effect)
    {
        _currentEffect = effect;
        gameObject.SetActive(true);
        PopulateContent(effect);
        FollowCursor();
    }

    /// <summary>
    /// Call from StatusEffectSlot.OnPointerExit.
    /// </summary>
    public void EndHover()
    {
        _currentEffect = null;
        Hide();
    }

    private void PopulateContent(ActiveStatusEffect active)
    {
        var definition = active.Definition;
        
        // Header colour
        if (headerPanel != null)
            headerPanel.color = definition.ResolvedHeaderColor;

        // Title
        if (titleText != null)
            titleText.text = definition.displayName;

        // Tooltip icon
        if (tooltipIconImage != null)
        {
            tooltipIconImage.sprite = definition.tooltipIcon;
            tooltipIconImage.enabled = definition.tooltipIcon != null;
        }

        // Description
        if (descriptionText != null)
        {
            descriptionText.text = definition.description;
            descriptionText.enabled = !string.IsNullOrEmpty(definition.description);
        }

        // Stat lines — build bullet list
        if (statLinesText != null)
        {
            if (definition.statLines != null && definition.statLines.Length > 0)
            {
                var sb = new StringBuilder();
                foreach (var line in definition.statLines)
                    if (!string.IsNullOrWhiteSpace(line))
                        sb.AppendLine($"• {line}");
                statLinesText.text = sb.ToString().TrimEnd();
                statLinesText.enabled = true;
            }
            else
            {
                statLinesText.enabled = false;
            }
        }

        // Flavour text
        if (flavourText != null)
        {
            flavourText.text = !string.IsNullOrEmpty(definition.flavourText)
                ? $"<i>{definition.flavourText}</i>"
                : string.Empty;
            flavourText.enabled = !string.IsNullOrEmpty(definition.flavourText);
        }

        RefreshMetaLine();
    }

    private void RefreshMetaLine()
    {
        if (metaText == null || _currentEffect == null) return;

        var definition = _currentEffect.Definition;
        var stringBuilder = new StringBuilder();

        if (definition.showDurationInTooltip)
        {
            if (definition.isPermanent)
                stringBuilder.AppendLine("Duration: <b>Permanent</b>");
            else
                stringBuilder.AppendLine($"Duration: <b>{_currentEffect.RemainingDuration:F1}s</b>");
        }

        if (definition.showStacksInTooltip && _currentEffect.StackCount > 1)
            stringBuilder.AppendLine($"Stacks: <b>{_currentEffect.StackCount} / {definition.maxStacks}</b>");

        metaText.text = stringBuilder.ToString().TrimEnd();
        metaText.enabled = stringBuilder.Length > 0;
    }

    private void FollowCursor()
    {
        if (_rootCanvas == null) return;

        // Convert mouse position to canvas local point
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)_rootCanvas.transform, Input.mousePosition,
        _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _rootCanvas.worldCamera, out Vector2 localPoint);

        Vector2 target = localPoint + cursorOffset;
        _rectTransform.anchoredPosition = target;
    }

    private void Show()
    {
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = StartCoroutine(FadeTo(1f, fadeInDuration));
    }

    private void Hide()
    {
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);

        if (!gameObject.activeInHierarchy)
        {
            _canvasGroup.alpha = 0f;
            return;
        }

        _fadeCoroutine = StartCoroutine(FadeTo(0f, fadeOutDuration));
    }

    private IEnumerator FadeTo(float targetAlpha, float duration)
    {
        float start = _canvasGroup.alpha;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            _canvasGroup.alpha = Mathf.Lerp(start, targetAlpha, t / duration);
            yield return null;
        }
        _canvasGroup.alpha = targetAlpha;
        gameObject.SetActive(false);
    }
}