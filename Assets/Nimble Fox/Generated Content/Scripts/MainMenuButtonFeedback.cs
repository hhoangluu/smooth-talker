using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MainMenuButtonFeedback : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler,
    ISelectHandler,
    IDeselectHandler
{
    [SerializeField] private Button targetButton;
    [SerializeField] private RectTransform targetTransform;
    [SerializeField] private Image targetGraphic;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hoverColor = new Color(1f, 1f, 1f, 0.9f);
    [SerializeField] private Color pressedColor = new Color(0.9f, 0.9f, 0.9f, 0.8f);
    [SerializeField] private float hoverScale = 1.05f;
    [SerializeField] private float pressedScale = 0.97f;
    [SerializeField] private float transitionSpeed = 10f;
    [SerializeField] private AudioSource uiAudioSource;
    [SerializeField] private AudioClip hoverClip;
    [SerializeField] private AudioClip clickClip;

    // Internal state
    private Vector3 _initialScale = Vector3.one;
    private bool _isHovered;
    private bool _isPressed;
    private bool _isSelected;

    private void Reset()
    {
        targetButton = GetComponent<Button>();
        targetTransform = GetComponent<RectTransform>();
        targetGraphic = GetComponent<Image>();

        if (targetGraphic != null)
        {
            normalColor = targetGraphic.color;
        }

        hoverColor = Color.Lerp(normalColor, Color.white, 0.15f);
        pressedColor = Color.Lerp(normalColor, Color.gray, 0.25f);

        hoverScale = 1.05f;
        pressedScale = 0.97f;
        transitionSpeed = 10f;
    }

    private void Awake()
    {
        if (targetButton == null)
            targetButton = GetComponent<Button>();

        if (targetTransform == null)
            targetTransform = GetComponent<RectTransform>();

        if (targetGraphic == null)
            targetGraphic = GetComponent<Image>();

        if (targetTransform != null)
            _initialScale = targetTransform.localScale;

        if (targetGraphic != null && normalColor == default(Color))
            normalColor = targetGraphic.color;
    }

    private void OnEnable()
    {
        if (targetButton != null)
            targetButton.onClick.AddListener(HandleButtonClicked);

        // Ensure visual state is synced when enabled
        ApplyVisualState(true);
    }

    private void OnDisable()
    {
        if (targetButton != null)
            targetButton.onClick.RemoveListener(HandleButtonClicked);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovered = true;
        PlayHover();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovered = false;
        _isPressed = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        _isPressed = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        _isPressed = false;
    }

    public void OnSelect(BaseEventData eventData)
    {
        _isSelected = true;
        PlayHover();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        _isSelected = false;
        _isHovered = false;
        _isPressed = false;
    }

    private void Update()
    {
        ApplyVisualState(false);
    }

    private void ApplyVisualState(bool immediate)
    {
        if (targetTransform == null && targetGraphic == null)
            return;

        // Determine target scale and color based on state priority:
        // pressed > hovered/selected > normal
        float targetScaleFactor = 1f;
        Color targetColor = normalColor;

        bool hoverOrSelected = _isHovered || _isSelected;

        if (_isPressed && hoverOrSelected)
        {
            targetScaleFactor = pressedScale;
            targetColor = pressedColor;
        }
        else if (hoverOrSelected)
        {
            targetScaleFactor = hoverScale;
            targetColor = hoverColor;
        }
        else
        {
            targetScaleFactor = 1f;
            targetColor = normalColor;
        }

        Vector3 desiredScale = _initialScale * targetScaleFactor;

        if (immediate || transitionSpeed <= 0f)
        {
            if (targetTransform != null)
                targetTransform.localScale = desiredScale;

            if (targetGraphic != null)
                targetGraphic.color = targetColor;

            return;
        }

        float t = transitionSpeed * Time.unscaledDeltaTime;
        t = Mathf.Clamp01(t);

        if (targetTransform != null)
        {
            targetTransform.localScale = Vector3.Lerp(
                targetTransform.localScale,
                desiredScale,
                t
            );
        }

        if (targetGraphic != null)
        {
            targetGraphic.color = Color.Lerp(
                targetGraphic.color,
                targetColor,
                t
            );
        }
    }

    private void PlayHover()
    {
        if (uiAudioSource == null || hoverClip == null)
            return;

        uiAudioSource.PlayOneShot(hoverClip);
    }

    private void PlayClick()
    {
        if (uiAudioSource == null || clickClip == null)
            return;

        uiAudioSource.PlayOneShot(clickClip);
    }

    private void HandleButtonClicked()
    {
        PlayClick();
    }
}