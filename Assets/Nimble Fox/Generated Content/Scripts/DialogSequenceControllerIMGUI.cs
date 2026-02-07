using System.Collections.Generic;
using UnityEngine;

public class DialogSequenceControllerIMGUI : MonoBehaviour
{
    [System.Serializable]
    public class DialogLine
    {
        public string speaker;
        [TextArea]
        public string text;
    }

    [SerializeField] private List<DialogLine> sequence = new List<DialogLine>();
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip clickSfx;
    [SerializeField] private float clickDebounceSeconds = 0.1f;

    // Normalized rect: x, y, w, h are 0–1 in screen space.
    [SerializeField] private Rect dialogPanelRectNormalized = new Rect(0.05f, 0.65f, 0.9f, 0.3f);
    [SerializeField] private float panelPadding = 12f;
    [SerializeField] private Color panelColor = new Color(0f, 0f, 0f, 0.75f);
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private Color speakerColor = Color.yellow;
    [SerializeField] private int fontSize = 18;
    [SerializeField] private int speakerFontSize = 20;
    [SerializeField] private bool showSpeaker = true;
    [SerializeField] private bool enablePopEffect = true;
    [SerializeField] private float popDurationSeconds = 0.12f;

    private int _index = -1;
    private bool _isActive = false;
    private float _lastClickTime = -999f;
    private float _popEndTime = -999f;

    private GUIStyle _panelStyle;
    private GUIStyle _speakerStyle;
    private GUIStyle _textStyle;

    [SerializeField] private bool autoStartOnPlay = true;

    private void Awake()
    {
        // Provide a reasonable default if the rect is invalid.
        if (dialogPanelRectNormalized.width <= 0f || dialogPanelRectNormalized.height <= 0f)
        {
            dialogPanelRectNormalized = new Rect(0.05f, 0.65f, 0.9f, 0.3f);
        }
    }

    private void Start()
    {
        if (autoStartOnPlay)
        {
            Begin();
        }
    }

    private void OnEnable()
    {
        // Styles are (re)created lazily in OnGUI.
    }

    private void OnDisable()
    {
        _isActive = false;
    }

    private void OnGUI()
    {
        if (!_isActive || sequence == null || sequence.Count == 0)
            return;

        if (_index < 0 || _index >= sequence.Count)
            return;

        EnsureStyles();

        // Compute panel rect from normalized coordinates.
        Rect panelRect = new Rect(
            dialogPanelRectNormalized.x * Screen.width,
            dialogPanelRectNormalized.y * Screen.height,
            dialogPanelRectNormalized.width * Screen.width,
            dialogPanelRectNormalized.height * Screen.height
        );

        // Click handling: any mouse button up inside the panel.
        Event e = Event.current;
        if (e.type == EventType.MouseUp && e.button == 0)
        {
            if (panelRect.Contains(e.mousePosition) && CanClick())
            {
                OnDialogClicked();
                // Consume the event so it doesn't propagate.
                e.Use();
            }
        }

        DialogLine current = sequence[_index];

        // Pop effect factor.
        float popScale = 1f;
        float alphaScale = 1f;
        if (enablePopEffect && Time.unscaledTime < _popEndTime)
        {
            float t = 1f - Mathf.Clamp01((_popEndTime - Time.unscaledTime) / popDurationSeconds);
            // Simple ease-out: small pulse.
            popScale = Mathf.Lerp(1f, 1.1f, 1f); // constant 1.1 during pop window
            alphaScale = Mathf.Lerp(1f, 1.2f, t); // slightly brighter
        }

        // Draw panel.
        Color oldGuiColor = GUI.color;
        GUI.color = panelColor;
        GUI.Box(panelRect, GUIContent.none, _panelStyle);
        GUI.color = oldGuiColor;

        // Begin content group inside the panel with padding.
        Rect contentRect = new Rect(
            panelRect.x + panelPadding,
            panelRect.y + panelPadding,
            panelRect.width - 2f * panelPadding,
            panelRect.height - 2f * panelPadding
        );

        GUI.BeginGroup(contentRect);
        {
            float y = 0f;

            if (showSpeaker && !string.IsNullOrEmpty(current.speaker))
            {
                GUIStyle speakerStyle = _speakerStyle;

                // Apply pop scaling to speaker text.
                int spBaseSize = Mathf.Max(1, speakerFontSize);
                speakerStyle.fontSize = Mathf.RoundToInt(spBaseSize * popScale);

                Color sc = speakerColor;
                sc.a *= alphaScale;
                speakerStyle.normal.textColor = sc;

                // Measure approximate height.
                Rect speakerRect = new Rect(
                    0f,
                    y,
                    contentRect.width,
                    speakerStyle.lineHeight + 4f
                );

                GUI.Label(speakerRect, current.speaker, speakerStyle);
                y += speakerRect.height;
            }

            GUIStyle textStyle = _textStyle;

            int baseSize = Mathf.Max(1, fontSize);
            textStyle.fontSize = Mathf.RoundToInt(baseSize * popScale);

            Color tc = textColor;
            tc.a *= alphaScale;
            textStyle.normal.textColor = tc;

            Rect textRect = new Rect(
                0f,
                y,
                contentRect.width,
                contentRect.height - y
            );

            GUI.Label(textRect, current.text, textStyle);
        }
        GUI.EndGroup();
    }

    private void EnsureStyles()
    {
        if (_panelStyle == null)
        {
            _panelStyle = new GUIStyle(GUI.skin.box);
            _panelStyle.margin = new RectOffset(0, 0, 0, 0);
            _panelStyle.padding = new RectOffset(0, 0, 0, 0);
        }

        if (_speakerStyle == null)
        {
            _speakerStyle = new GUIStyle(GUI.skin.label);
            _speakerStyle.alignment = TextAnchor.UpperLeft;
            _speakerStyle.wordWrap = false;
            _speakerStyle.richText = false;
            _speakerStyle.fontSize = speakerFontSize;
            _speakerStyle.normal.textColor = speakerColor;
        }

        if (_textStyle == null)
        {
            _textStyle = new GUIStyle(GUI.skin.label);
            _textStyle.alignment = TextAnchor.UpperLeft;
            _textStyle.wordWrap = true;
            _textStyle.richText = false;
            _textStyle.fontSize = fontSize;
            _textStyle.normal.textColor = textColor;
        }
    }

    public void Begin()
    {
        if (sequence == null || sequence.Count == 0)
        {
            _isActive = false;
            _index = -1;
            return;
        }

        _index = 0;
        _isActive = true;
        _lastClickTime = Time.unscaledTime;
        if (enablePopEffect && popDurationSeconds > 0f)
        {
            _popEndTime = Time.unscaledTime + popDurationSeconds;
        }
    }

    public void Begin(List<DialogLine> newSequence, bool startAtBeginning = true)
    {
        sequence = newSequence ?? new List<DialogLine>();

        if (sequence.Count == 0)
        {
            _isActive = false;
            _index = -1;
            return;
        }

        if (startAtBeginning || _index < 0 || _index >= sequence.Count)
        {
            _index = 0;
        }

        _isActive = true;
        _lastClickTime = Time.unscaledTime;
        if (enablePopEffect && popDurationSeconds > 0f)
        {
            _popEndTime = Time.unscaledTime + popDurationSeconds;
        }
    }

    public void Advance()
    {
        if (!_isActive || sequence == null || sequence.Count == 0)
            return;

        _index++;

        if (_index >= sequence.Count)
        {
            End();
            return;
        }

        _lastClickTime = Time.unscaledTime;
        if (enablePopEffect && popDurationSeconds > 0f)
        {
            _popEndTime = Time.unscaledTime + popDurationSeconds;
        }

        PlayClickSfx();
    }

    public void End()
    {
        _isActive = false;
        _index = -1;
    }

    private void OnDialogClicked()
    {
        Advance();
    }

    private void PlayClickSfx()
    {
        if (audioSource == null || clickSfx == null)
            return;

        audioSource.PlayOneShot(clickSfx);
    }

    private bool CanClick()
    {
        return Time.unscaledTime - _lastClickTime >= clickDebounceSeconds;
    }
}