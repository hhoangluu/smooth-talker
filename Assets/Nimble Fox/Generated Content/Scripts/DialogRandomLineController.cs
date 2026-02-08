using System.Collections.Generic;
using UnityEngine;

public class DialogRandomLineController : MonoBehaviour
{
    [SerializeField] private List<string> lines = new List<string>();
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip clickSfx;
    [SerializeField] private float popDurationSeconds = 0.12f;
    [SerializeField] private float clickDebounceSeconds = 0.1f;
    [SerializeField] private Rect dialogPanelRectNormalized = new Rect(0.05f, 0.75f, 0.9f, 0.2f);
    [SerializeField] private float panelPadding = 12f;
    [SerializeField] private Color panelColor = new Color(0f, 0f, 0f, 0.7f);
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private int fontSize = 18;
    [SerializeField] private bool enablePopEffect = true;

    private int _currentLineIndex = -1;
    private float _lastClickTime;
    private float _popEndTime;
    private string _currentText = string.Empty;
    private GUIStyle _panelStyle;
    private GUIStyle _textStyle;

    private Texture2D _panelTexture;

    private void Awake()
    {
        EnsureStyles();
        ShowInitialLine();
    }

    private void OnEnable()
    {
        // Reset timing/pop on enable, but keep current text if already chosen.
        _lastClickTime = 0f;
        _popEndTime = 0f;

        if (string.IsNullOrEmpty(_currentText))
        {
            ShowInitialLine();
        }
    }

    private void OnDisable()
    {
        // Nothing persistent to clean up; IMGUI is stateless per frame.
    }

    private void OnGUI()
    {
        if (lines == null || lines.Count == 0)
            return;

        EnsureStyles();

        // Determine actual on-screen rect from normalized rect.
        Rect panelRect = GetPanelScreenRect();

        // Draw clickable panel as a button with custom style.
        bool clicked = GUI.Button(panelRect, GUIContent.none, _panelStyle);
        if (clicked)
        {
            OnDialogClicked();
        }

        // Prepare text style (including pop effect for this frame).
        Color originalColor = _textStyle.normal.textColor;
        int originalFontSize = _textStyle.fontSize;

        ApplyPopEffectToStyle();

        // Inner rect with padding.
        Rect textRect = new Rect(
            panelRect.x + panelPadding,
            panelRect.y + panelPadding,
            panelRect.width - 2f * panelPadding,
            panelRect.height - 2f * panelPadding
        );

        GUI.Label(textRect, _currentText ?? string.Empty, _textStyle);

        // Restore style so we don't leak modifications between frames.
        _textStyle.normal.textColor = originalColor;
        _textStyle.fontSize = originalFontSize;
    }

    private void EnsureStyles()
    {
        // Panel style
        if (_panelStyle == null)
        {
            _panelStyle = new GUIStyle(GUI.skin.box);
            _panelStyle.margin = new RectOffset(0, 0, 0, 0);
            _panelStyle.padding = new RectOffset(0, 0, 0, 0);
        }

        if (_panelTexture == null)
        {
            _panelTexture = new Texture2D(1, 1);
            _panelTexture.hideFlags = HideFlags.HideAndDontSave;
        }

        // Update panel color if needed.
        if (_panelTexture != null)
        {
            Color current = _panelTexture.GetPixel(0, 0);
            if (current != panelColor)
            {
                _panelTexture.SetPixel(0, 0, panelColor);
                _panelTexture.Apply();
            }

            _panelStyle.normal.background = _panelTexture;
        }

        // Text style
        if (_textStyle == null)
        {
            _textStyle = new GUIStyle(GUI.skin.label);
            _textStyle.wordWrap = true;
            _textStyle.alignment = TextAnchor.MiddleLeft;
        }

        _textStyle.normal.textColor = textColor;
        _textStyle.fontSize = fontSize > 0 ? fontSize : GUI.skin.label.fontSize;
    }

    private void OnDialogClicked()
    {
        if (!CanClick())
            return;

        _lastClickTime = Time.unscaledTime;

        AdvanceLine();
    }

    /// <summary>
    /// Shows an initial random line without playing sound or pop animation.
    /// </summary>
    private void ShowInitialLine()
    {
        if (lines == null || lines.Count == 0)
            return;

        int nextIndex = PickNextLineIndex();
        if (nextIndex < 0 || nextIndex >= lines.Count)
            return;

        _currentLineIndex = nextIndex;
        SetLineText(lines[nextIndex]);
    }

    private void AdvanceLine()
    {
        if (lines == null || lines.Count == 0)
            return;

        int nextIndex = PickNextLineIndex();
        if (nextIndex < 0 || nextIndex >= lines.Count)
            return;

        _currentLineIndex = nextIndex;
        SetLineText(lines[nextIndex]);
        PlayClickSfx();

        if (enablePopEffect && popDurationSeconds > 0f)
        {
            _popEndTime = Time.unscaledTime + popDurationSeconds;
        }
    }

    private int PickNextLineIndex()
    {
        if (lines == null || lines.Count == 0)
            return -1;

        int count = lines.Count;

        if (count == 1)
            return 0;

        int newIndex;
        do
        {
            newIndex = Random.Range(0, count);
        } while (newIndex == _currentLineIndex);

        return newIndex;
    }

    private void SetLineText(string text)
    {
        _currentText = text ?? string.Empty;
    }

    private void PlayClickSfx()
    {
        if (audioSource == null || clickSfx == null)
            return;

        audioSource.PlayOneShot(clickSfx);
    }

    private bool CanClick()
    {
        if (clickDebounceSeconds <= 0f)
            return true;

        return Time.unscaledTime - _lastClickTime >= clickDebounceSeconds;
    }

    private Rect GetPanelScreenRect()
    {
        // Fallback default if rect has no size.
        Rect norm = dialogPanelRectNormalized;
        if (norm.width <= 0f || norm.height <= 0f)
        {
            norm = new Rect(0.05f, 0.75f, 0.9f, 0.2f);
        }

        float x = norm.x * Screen.width;
        float y = norm.y * Screen.height;
        float w = norm.width * Screen.width;
        float h = norm.height * Screen.height;
        return new Rect(x, y, w, h);
    }

    private void ApplyPopEffectToStyle()
    {
        if (!enablePopEffect || popDurationSeconds <= 0f)
            return;

        float now = Time.unscaledTime;
        if (now >= _popEndTime)
            return;

        float remaining = _popEndTime - now;
        float t = Mathf.Clamp01(remaining / popDurationSeconds); // 1 at start -> 0 at end

        // Simple ease-out pop: scale from 1.15 down to 1.0
        float scale = 1f + 0.15f * t;

        int baseFontSize = fontSize > 0 ? fontSize : GUI.skin.label.fontSize;
        _textStyle.fontSize = Mathf.RoundToInt(baseFontSize * scale);

        // Optional alpha pulse (slightly brighter at start).
        Color c = textColor;
        c.a = Mathf.Lerp(textColor.a, 1f, t * 0.5f);
        _textStyle.normal.textColor = c;
    }
}