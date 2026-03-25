using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;
using System.Collections;

/// <summary>
/// Singleton tooltip panel shown when hovering a StatusEffectSlot.
/// Reads per-instance overrides from ActiveStatusEffect first,
/// falling back to the SO definition — the SO is never modified.
/// </summary>
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
    [SerializeField] private TextMeshProUGUI metaText;

    [Header("Layout")]
    [SerializeField] private Vector2 cursorOffset = new(12f, -12f);

    [Header("Fade")]
    [SerializeField] private float fadeInDuration = 0.12f;
    [SerializeField] private float fadeOutDuration = 0.08f;

    private CanvasGroup _canvasGroup;
    private RectTransform _rectTransform;
    private Canvas _rootCanvas;
    private ActiveStatusEffect _currentEffect;
    private Coroutine _fadeCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        _canvasGroup = GetComponent<CanvasGroup>();
        _rectTransform = GetComponent<RectTransform>();
        _rootCanvas = GetComponentInParent<Canvas>();

        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();

        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (_currentEffect == null) return;
        FollowCursor();
        Show();
        RefreshMetaLine();
    }

    public void BeginHover(ActiveStatusEffect effect)
    {
        _currentEffect = effect;
        gameObject.SetActive(true);
        PopulateContent(effect);
        FollowCursor();
    }

    public void EndHover()
    {
        _currentEffect = null;
        Hide();
    }

    private void PopulateContent(ActiveStatusEffect active)
    {
        var def = active.Definition;

        if (headerPanel != null) headerPanel.color = def.borderColor;
        if (titleText != null) titleText.text = def.displayName;

        if (tooltipIconImage != null)
        {
            tooltipIconImage.sprite = def.tooltipIcon;
            tooltipIconImage.enabled = def.tooltipIcon != null;
        }

        if (descriptionText != null)
        {
            descriptionText.text = active.ResolvedDescription;
            descriptionText.enabled = !string.IsNullOrEmpty(active.ResolvedDescription);
        }

        if (statLinesText != null)
        {
            var lines = active.ResolvedStatLines;
            if (lines != null && lines.Length > 0)
            {
                var sb = new StringBuilder();
                foreach (var line in lines)
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

        if (flavourText != null)
        {
            var fv = active.ResolvedFlavour;
            flavourText.text = !string.IsNullOrEmpty(fv) ? $"<i>{fv}</i>" : string.Empty;
            flavourText.enabled = !string.IsNullOrEmpty(fv);
        }

        RefreshMetaLine();
    }

    private void RefreshMetaLine()
    {
        if (metaText == null || _currentEffect == null) return;

        var def = _currentEffect.Definition;
        var sb = new StringBuilder();

        if (def.showDurationInTooltip)
        {
            if (def.isPermanent)
                sb.AppendLine("Duration: <b>Permanent</b>");
            else
                sb.AppendLine($"Duration: <b>{_currentEffect.RemainingDuration:F1}s</b>");
        }

        if (def.showStacksInTooltip && _currentEffect.StackCount > 1)
            sb.AppendLine($"Stacks: <b>{_currentEffect.StackCount} / {def.maxStacks}</b>");

        metaText.text = sb.ToString().TrimEnd();
        metaText.enabled = sb.Length > 0;
    }

    private void FollowCursor()
    {
        if (_rootCanvas == null) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)_rootCanvas.transform, Input.mousePosition,
            _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _rootCanvas.worldCamera,
            out Vector2 localPoint);
        _rectTransform.anchoredPosition = localPoint + cursorOffset;
    }

    private void Show()
    {
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = StartCoroutine(FadeTo(1f, fadeInDuration));
    }

    private void Hide()
    {
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        if (!gameObject.activeInHierarchy) { _canvasGroup.alpha = 0f; return; }
        _fadeCoroutine = StartCoroutine(FadeTo(0f, fadeOutDuration));
    }

    private IEnumerator FadeTo(float target, float duration)
    {
        float start = _canvasGroup.alpha;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            _canvasGroup.alpha = Mathf.Lerp(start, target, t / duration);
            yield return null;
        }
        _canvasGroup.alpha = target;
        if (target == 0f) gameObject.SetActive(false);
    }
}