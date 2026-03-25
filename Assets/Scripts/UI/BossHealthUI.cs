using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BossHealthUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text bossName;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Image sliderFill;
    [Tooltip("Optional ghost slider for delayed health loss effect")]
    [SerializeField] private Slider ghostSlider;
    [Tooltip("Optional ghost fill image for color fading effect")]
    [SerializeField] private Image ghostFill;    

    [Header("Health Colors")]
    [SerializeField] private Color highHealthColor = new(0.2f, 0.85f, 0.3f);   // green
    [SerializeField] private Color midHealthColor = new(1.0f, 0.75f, 0.1f);   // yellow
    [SerializeField] private Color lowHealthColor = new(0.9f, 0.2f, 0.1f);    // red
    [SerializeField] private Color hitFlashColor = Color.white;

    [Header("Slider Settings")]
    [SerializeField] private float smoothSpeed = 6f;    // lerp speed for smooth bar movement
    [SerializeField] private float ghostDelay = 0.6f;  // seconds before ghost bar starts catching up
    [SerializeField] private float ghostSpeed = 2.5f;  // lerp speed of ghost bar

    [Header("Text Effects")]
    [SerializeField] private bool enableNamePulse = true;
    [SerializeField] private float pulseSpeed = 2.5f;
    [SerializeField] private float pulseMinAlpha = 0.55f;

    [SerializeField] private bool enableShakeOnHit = true;
    [SerializeField] private float shakeDuration = 0.25f;
    [SerializeField] private float shakeMagnitude = 4f;

    [SerializeField] private bool enableVertexGrad = false;
    [SerializeField] private Color gradTopColor = Color.white;
    [SerializeField] private Color gradBottomColor = new(0.8f, 0.8f, 0.8f);

    private float _maxHealth;
    private float _targetValue;
    private float _ghostTargetValue;
    private bool _smoothing;
    private bool _ghostPending;
    private Color _originalNameColor;
    private Vector2 _nameOriginalPos;
    private Coroutine _flashCoroutine;
    private Coroutine _ghostCoroutine;
    private Coroutine _shakeCoroutine;

    #region Overloads initialize
    public void Initialize(string name, float maxHealth)
    {
        Setup(name, maxHealth);
    }

    public void Initialize(string name, float maxHealth, Color textColor)
    {
        Setup(name, maxHealth);
        bossName.color = textColor;
        _originalNameColor = textColor;
    }

    public void Initialize(string name, float maxHealth, Color textColor, Color gradTopColor, Color gradBottomColor)
    {
        Setup(name, maxHealth);
        bossName.color = textColor;
        _originalNameColor = textColor;
        this.gradTopColor = gradTopColor;
        this.gradBottomColor = gradBottomColor;
        ApplyVertexGradient();
    }

    public void Initialize(string name, float maxHealth, Color textColor, Color topLeft, Color topRight, Color bottomLeft, Color bottomRight)
    {
        Setup(name, maxHealth);
        bossName.color = textColor;
        _originalNameColor = textColor;
        enableVertexGrad = true;
        ApplyVertexGradient(topLeft, topRight, bottomLeft, bottomRight);
    }

    #endregion
    private void Setup(string name, float maxHealth)
    {
        bossName.text = name;
        _maxHealth = maxHealth;
        _originalNameColor = bossName.color;
        _nameOriginalPos = bossName.rectTransform.anchoredPosition;

        healthSlider.maxValue = maxHealth;
        healthSlider.value = maxHealth;
        _targetValue = maxHealth;

        if (ghostSlider != null)
        {
            ghostSlider.maxValue = maxHealth;
            ghostSlider.value = maxHealth;
            _ghostTargetValue = maxHealth;
        }

        ApplyVertexGradient();
        SetFillColor(1f);
    }

    /// <summary>
    /// Update health — triggers smooth bar movement, ghost trail, color shift, and optional text shake.
    /// </summary>
    public void UpdateHealth(float current)
    {
        current = Mathf.Clamp(current, 0f, _maxHealth);
        _targetValue = current;

        // Flash the fill bar white on hit
        if (_flashCoroutine != null) StopCoroutine(_flashCoroutine);
        _flashCoroutine = StartCoroutine(FlashFill());

        // Shake the boss name label
        if (enableShakeOnHit)
        {
            if (_shakeCoroutine != null) StopCoroutine(_shakeCoroutine);
            _shakeCoroutine = StartCoroutine(ShakeName());
        }

        // Delayed ghost bar
        if (ghostSlider != null)
        {
            if (_ghostCoroutine != null) StopCoroutine(_ghostCoroutine);
            _ghostCoroutine = StartCoroutine(DelayedGhost(current));
        }
    }

    public void Show() => gameObject.SetActive(true);
    public void Hide() => gameObject.SetActive(false);

    private void Update()
    {
        if (!Mathf.Approximately(healthSlider.value, _targetValue))
        {
            healthSlider.value = Mathf.Lerp(healthSlider.value, _targetValue, Time.deltaTime * smoothSpeed);
            SetFillColor(healthSlider.value / _maxHealth);
        }

        if (enableNamePulse)
        {
            float alpha = Mathf.Lerp(pulseMinAlpha, 1f,
                (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f);
            Color c = bossName.color;
            c.a = alpha;
            bossName.color = c;
        }
    }

    /// <summary>
    /// Flash fill white for one frame then interpolate back.
    /// </summary>
    private IEnumerator FlashFill()
    {
        if (sliderFill == null) yield break;
        sliderFill.color = hitFlashColor;
        float elapsed = 0f;
        float duration = 0.18f;
        float ratio = _targetValue / _maxHealth;
        Color targetColor = GetHealthColor(ratio);
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            sliderFill.color = Color.Lerp(hitFlashColor, targetColor, elapsed / duration);
            yield return null;
        }
        sliderFill.color = targetColor;
    }

    /// <summary>
    /// Ghost bar stays put briefly, then slowly catches up to real health.
    /// </summary>
    private IEnumerator DelayedGhost(float newTarget)
    {
        yield return new WaitForSeconds(ghostDelay);
        while (!Mathf.Approximately(ghostSlider.value, newTarget))
        {
            ghostSlider.value = Mathf.Lerp(ghostSlider.value, newTarget, Time.deltaTime * ghostSpeed);
            if (ghostFill != null)
                ghostFill.color = Color.Lerp(ghostFill.color,
                    new Color(1f, 0.55f, 0.1f, 0.7f), Time.deltaTime * ghostSpeed);
            yield return null;
        }
        ghostSlider.value = newTarget;
    }

    /// <summary>
    /// Shake the boss name label on hit.
    /// </summary>
    private IEnumerator ShakeName()
    {
        float elapsed = 0f;
        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            Vector2 offset = (1f - elapsed / shakeDuration) * shakeMagnitude * Random.insideUnitCircle;       // dampens over time
            bossName.rectTransform.anchoredPosition = _nameOriginalPos + offset;
            yield return null;
        }
        bossName.rectTransform.anchoredPosition = _nameOriginalPos;
    }

    private void SetFillColor(float ratio)
    {
        if (sliderFill == null) return;
        sliderFill.color = GetHealthColor(ratio);
    }

    private Color GetHealthColor(float ratio)
    {
        if (ratio > 0.5f)
            return Color.Lerp(midHealthColor, highHealthColor, (ratio - 0.5f) * 2f);
        else
            return Color.Lerp(lowHealthColor, midHealthColor, ratio * 2f);
    }

    private void ApplyVertexGradient()
    {
        if (!enableVertexGrad) return;
        bossName.enableVertexGradient = true;
        bossName.colorGradient = new VertexGradient(
            gradTopColor, gradTopColor,
            gradBottomColor, gradBottomColor);
    }
    private void ApplyVertexGradient(Color topLeft, Color topRight, Color bottomLeft, Color bottomRight)
    {
        if (!enableVertexGrad) return;
        bossName.enableVertexGradient = true;
        bossName.colorGradient = new VertexGradient(topLeft, topRight, bottomLeft, bottomRight);
    }
}