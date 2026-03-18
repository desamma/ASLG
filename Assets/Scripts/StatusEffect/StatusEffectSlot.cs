using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;
using System.Collections;

/// <summary>
/// Represents one icon slot in the HUD.
/// Implements IPointerEnterHandler / IPointerExitHandler to show the tooltip.
/// </summary>
public class StatusEffectSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI References")]
    [SerializeField] private Image background;
    [SerializeField] private RectTransform iconRoot;
    [SerializeField] private Image timerRing;
    [SerializeField] private GameObject stackBadge;
    [SerializeField] private TextMeshProUGUI stackText;
    [SerializeField] private TextMeshProUGUI durationText;

    [Header("Slot Animation")]
    [SerializeField] private float addAnimDuration = 0.25f;
    [SerializeField] private float removeAnimDuration = 0.20f;
    [SerializeField] private float refreshPunchScale = 1.30f;

    private ActiveStatusEffect _active;
    private CanvasGroup _canvasGroup;
    private GameObject _iconInstance;

    public StatusEffectType EffectType => _active?.Definition.effectType ?? StatusEffectType.Neutral;

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void Update()
    {
        if (_active == null) return;

        if (timerRing != null)
            timerRing.fillAmount = _active.NormalizedTimeLeft;

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

    private void OnDisable()
    {
        // Hide tooltip if the slot is disabled mid hover
        StatusEffectTooltip.Instance.EndHover();
    }

    public void Initialize(ActiveStatusEffect active)
    {
        _active = active;
        var def = active.Definition;

        if (background != null)
            background.color = def.borderColor;

        SpawnIconPrefab(def);

        if (timerRing != null)
        {
            timerRing.fillAmount = 1f;
            timerRing.gameObject.SetActive(!def.isPermanent);
        }

        UpdateStackBadge();
        active.OnStackChanged += _ => UpdateStackBadge();

        _canvasGroup.alpha = 0f;
        transform.localScale = Vector3.one * 0.5f;
        StartCoroutine(AnimateAppear());
    }

    public void OnRefresh()
    {
        StopAllCoroutines();
        StartCoroutine(AnimatePunch());
    }

    public void PlayRemoveAnimation(Action onComplete)
    {
        StatusEffectTooltip.Instance.EndHover();
        StopAllCoroutines();
        StartCoroutine(AnimateDisappear(onComplete));
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_active == null) return;
        StatusEffectTooltip.Instance.BeginHover(_active);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        StatusEffectTooltip.Instance.EndHover();
    }

    private void SpawnIconPrefab(StatusEffect def)
    {
        if (_iconInstance != null)
            Destroy(_iconInstance);

        if (def.iconPrefab == null)
        {
            Debug.LogWarning($"[StatusEffectSlot] '{def.effectId}' has no iconPrefab assigned.");
            return;
        }

        Transform parent = iconRoot != null ? (Transform)iconRoot : transform;
        _iconInstance = Instantiate(def.iconPrefab, parent);
        _iconInstance.transform.SetLocalPositionAndRotation(def.iconLocalPosition, Quaternion.identity);
        _iconInstance.transform.localScale = def.iconLocalScale;

        //if (_iconInstance.TryGetComponent<RectTransform>(out var rt) && iconRoot != null)
        //{
        //    rt.anchorMin = Vector2.zero;
        //    rt.anchorMax = Vector2.one;
        //    rt.offsetMin = Vector2.zero;
        //    rt.offsetMax = Vector2.zero;
        //    rt.localScale = def.iconLocalScale;
        //    rt.localPosition = def.iconLocalPosition;
        //}
    }

    private void UpdateStackBadge()
    {
        if (stackBadge == null) return;
        bool show = _active != null && _active.StackCount > 1;
        stackBadge.SetActive(show);
        if (stackText != null && show)
            stackText.text = _active.StackCount.ToString();
    }

    private IEnumerator AnimateAppear()
    {
        float t = 0f;
        while (t < addAnimDuration)
        {
            t += Time.deltaTime;
            var p = Mathf.SmoothStep(0f, 1f, t / addAnimDuration);
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
            var p = Mathf.SmoothStep(0f, 1f, t / removeAnimDuration);
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

        //grow bigger 1.0 → 1.3 
        while (t < half)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * refreshPunchScale, t / half);
            yield return null;
        }

        t = 0f;
        //shrink back 1.3 → 1.0
        while (t < half)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(Vector3.one * refreshPunchScale, Vector3.one, t / half);
            yield return null;
        }
        transform.localScale = Vector3.one;
    }
}