using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class StartButtonLowPolyLabel : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler,
    ISelectHandler,
    IDeselectHandler
{
    [SerializeField] private Button targetButton;
    [SerializeField] private Image labelImage;

    [Header("Sprites")]
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite hoverSprite;
    [SerializeField] private Sprite pressedSprite;

    [Header("Tint / Disabled")]
    [SerializeField] private Color tint = Color.white;
    [SerializeField] private float disabledAlpha = 0.4f;

    // Internal state
    private bool _isHovered;
    private bool _isPressed;
    private bool _isSelected;

    // Cache of the initial tint/alpha so we can fall back if needed
    private Color _initialColor;
    private bool _hasInitialColor;

    private void Reset()
    {
        // Try to auto-wire references when the component is first added
        if (targetButton == null)
            targetButton = GetComponentInParent<Button>();

        if (labelImage == null)
            labelImage = GetComponent<Image>();

        if (labelImage != null)
        {
            _initialColor = labelImage.color;
            _hasInitialColor = true;

            // Default tint to the existing color (keeping alpha)
            if (tint == default(Color))
                tint = new Color(_initialColor.r, _initialColor.g, _initialColor.b, 1f);
        }

        if (disabledAlpha <= 0f || disabledAlpha > 1f)
            disabledAlpha = 0.4f;
    }

    private void Awake()
    {
        if (targetButton == null)
            targetButton = GetComponentInParent<Button>();

        if (labelImage == null)
            labelImage = GetComponent<Image>();

        if (labelImage != null && !_hasInitialColor)
        {
            _initialColor = labelImage.color;
            _hasInitialColor = true;

            if (tint == default(Color))
                tint = new Color(_initialColor.r, _initialColor.g, _initialColor.b, 1f);
        }
    }

    private void OnEnable()
    {
        // Ensure flags are reset when enabled
        _isHovered = false;
        _isPressed = false;
        _isSelected = false;

        // Sync visual state immediately
        ApplyState(true);
    }

    private void OnDisable()
    {
        // Clear transient interaction state
        _isHovered = false;
        _isPressed = false;
        _isSelected = false;
    }

    private void Update()
    {
        ApplyState(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovered = true;
        // Visual update will be picked up in Update()
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
    }

    public void OnDeselect(BaseEventData eventData)
    {
        _isSelected = false;
        _isHovered = false;
        _isPressed = false;
    }

    /// <summary>
    /// Applies the correct sprite and tint based on hover/press/selection and button interactivity.
    /// </summary>
    private void ApplyState(bool immediate)
    {
        if (labelImage == null)
            return;

        bool isInteractable = targetButton == null || targetButton.interactable;

        // Disabled state: show normal sprite but with reduced alpha.
        if (!isInteractable)
        {
            SetSprite(normalSprite);
            ApplyTintAndAlpha(disabledAlpha);
            return;
        }

        bool hoverOrSelected = _isHovered || _isSelected;
        Sprite targetSprite = normalSprite;
        float targetAlpha = 1f;

        // Priority: pressed > hover/selected > normal
        if (_isPressed && hoverOrSelected)
        {
            if (pressedSprite != null)
                targetSprite = pressedSprite;
            else if (hoverSprite != null)
                targetSprite = hoverSprite;
        }
        else if (hoverOrSelected)
        {
            if (hoverSprite != null)
                targetSprite = hoverSprite;
        }

        SetSprite(targetSprite);
        ApplyTintAndAlpha(targetAlpha);
    }

    /// <summary>
    /// Assigns the given sprite to the label image.
    /// </summary>
    private void SetSprite(Sprite sprite)
    {
        if (labelImage == null)
            return;

        // Never assign null if we already have a sprite and the fallback exists.
        if (sprite == null && normalSprite != null)
            sprite = normalSprite;

        labelImage.sprite = sprite;
        labelImage.enabled = (sprite != null);
    }

    /// <summary>
    /// Applies the configured tint color and a dynamic alpha.
    /// </summary>
    private void ApplyTintAndAlpha(float alpha)
    {
        if (labelImage == null)
            return;

        alpha = Mathf.Clamp01(alpha);

        Color baseTint = tint;
        if (baseTint == default(Color) && _hasInitialColor)
        {
            baseTint = new Color(_initialColor.r, _initialColor.g, _initialColor.b, 1f);
        }

        labelImage.color = new Color(baseTint.r, baseTint.g, baseTint.b, alpha);
    }
}