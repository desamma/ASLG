using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

/// <summary>
/// Represents one icon slot in the HUD.
/// The icon is a live GameObject prefab (Animator, particles, sprites — anything)
/// instantiated inside the IconRoot RectTransform at runtime.
///
/// Expected prefab hierarchy:
///   StatusEffectSlot  (this script + CanvasGroup)
///   ├── Background    (Image  — border / background tint)
///   ├── IconRoot      (RectTransform — icon prefab spawns here)
///   ├── TimerRing     (Image  — Filled / Radial 360, clockwise)
///   ├── StackBadge    (GameObject)
///   │   └── StackText   (TextMeshProUGUI)
///   └── DurationText  (TextMeshProUGUI — optional countdown label)
///
/// Icon prefab can be:
///   • A UI Image with an Animator driving sprite-sheet / frame animation
///   • A SpriteRenderer + Animator (on its own sorting layer above the UI)
///   • A Particle System (World Space, offset on Z)
///   • Any combination of the above
/// </summary>
public class StatusEffectSlot : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────
    [Header("UI References")]
    [SerializeField] private Image background;
    [SerializeField] private RectTransform iconRoot;       // icon prefab lands here
    [SerializeField] private Image timerRing;
    [SerializeField] private GameObject stackBadge;
    [SerializeField] private TextMeshProUGUI stackText;
    [SerializeField] private TextMeshProUGUI durationText;   // optional "4.2s" label

    [Header("Slot Animation")]
    [SerializeField] private float addAnimDuration = 0.25f;
    [SerializeField] private float removeAnimDuration = 0.20f;
    [SerializeField] private float refreshPunchScale = 1.30f;

    // ── State ─────────────────────────────────────────────────────────────
    private ActiveStatusEffect _active;
    private CanvasGroup _canvasGroup;
    private GameObject _iconInstance;  // live prefab inside iconRoot

    public StatusEffectType EffectType => _active?.Definition.effectType ?? StatusEffectType.Neutral;

    // ── Lifecycle ─────────────────────────────────────────────────────────
    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void Update()
    {
        if (_active == null) return;

        // Timer ring fill
        if (timerRing != null)
            timerRing.fillAmount = _active.NormalizedTimeLeft;

        // Duration countdown text
        if (durationText != null)
        {
            if (_active.Definition.isPermanent)
                durationText.text = "∞";
            else
                durationText.text = _active.RemainingDuration > 9.5f
                    ? Mathf.CeilToInt(_active.RemainingDuration).ToString()
                    : _active.RemainingDuration.ToString("F1");
        }
    }

    /// <summary>
    /// Called by StatusEffectHUD when the effect is first applied.
    /// </summary>
    public void Initialise(ActiveStatusEffect active)
    {
        _active = active;
        var def = active.Definition;

        // Border colour
        if (background != null)
            background.color = def.borderColor;

        // Spawn the animated icon prefab
        SpawnIconPrefab(def);

        // Timer ring
        if (timerRing != null)
        {
            timerRing.fillAmount = 1f;
            timerRing.gameObject.SetActive(!def.isPermanent);
        }

        // Stacks
        UpdateStackBadge();
        active.OnStackChanged += _ => UpdateStackBadge();

        // Slot appear animation
        _canvasGroup.alpha = 0f;
        transform.localScale = Vector3.one * 0.5f;
        StartCoroutine(AnimateAppear());
    }

    /// <summary>
    /// Called by StatusEffectHUD when the effect is reapplied / refreshed.
    /// </summary>
    public void OnRefresh()
    {
        StopAllCoroutines();
        StartCoroutine(AnimatePunch());
    }

    /// <summary>
    /// Called by StatusEffectHUD when the effect expires or is removed.
    /// </summary>
    public void PlayRemoveAnimation(Action onComplete)
    {
        StopAllCoroutines();
        StartCoroutine(AnimateDisappear(onComplete));
    }

    // ── Icon Prefab ───────────────────────────────────────────────────────

    private void SpawnIconPrefab(StatusEffect def)
    {
        // Clean up any previous icon
        if (_iconInstance != null)
            Destroy(_iconInstance);

        if (def.iconPrefab == null)
        {
            Debug.LogWarning($"[StatusEffectSlot] '{def.effectId}' has no iconPrefab assigned.");
            return;
        }

        // Parent: use iconRoot if wired up, otherwise fall back to this slot
        Transform parent = iconRoot != null ? (Transform)iconRoot : transform;

        _iconInstance = Instantiate(def.iconPrefab, parent);
        _iconInstance.transform.SetLocalPositionAndRotation(def.iconLocalPosition, Quaternion.identity);
        _iconInstance.transform.localScale = def.iconLocalScale;

        // If the prefab root is a UI RectTransform, stretch it to fill iconRoot
        if (_iconInstance.TryGetComponent<RectTransform>(out var rt) && iconRoot != null)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = def.iconLocalScale;
            rt.localPosition = def.iconLocalPosition;
        }
    }

    private void UpdateStackBadge()
    {
        if (stackBadge == null) return;
        bool show = _active != null && _active.StackCount > 1;
        stackBadge.SetActive(show);
        if (stackText != null && show)
            stackText.text = _active.StackCount.ToString();
    }

    // ── Slot Coroutine Animations ─────────────────────────────────────────

    private IEnumerator AnimateAppear()
    {
        float t = 0f;
        while (t < addAnimDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.SmoothStep(0f, 1f, t / addAnimDuration);
            _canvasGroup.alpha = p;
            transform.localScale = Vector3.Lerp(Vector3.one * 0.5f, Vector3.one, p);
            yield return null;
        }
        _canvasGroup.alpha = 1f;
        transform.localScale = Vector3.one;
    }

    private IEnumerator AnimateDisappear(Action onComplete)
    {
        float t = 0f;
        Vector3 startScale = transform.localScale;
        while (t < removeAnimDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.SmoothStep(0f, 1f, t / removeAnimDuration);
            _canvasGroup.alpha = 1f - p;
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, p);
            yield return null;
        }
        onComplete?.Invoke();
    }

    private IEnumerator AnimatePunch()
    {
        float half = addAnimDuration * 0.5f;
        float t = 0f;

        while (t < half)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * refreshPunchScale, t / half);
            yield return null;
        }
        t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(Vector3.one * refreshPunchScale, Vector3.one, t / half);
            yield return null;
        }
        transform.localScale = Vector3.one;
    }
}
