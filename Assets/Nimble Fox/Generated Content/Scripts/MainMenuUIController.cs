using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class MainMenuUIController : MonoBehaviour
{
    [Header("Start Button")]
    [SerializeField] private Button startButton;

    [Header("Scene Loading")]
    [SerializeField] private string sceneToLoad;
    [SerializeField] private bool loadSceneOnStart = true;

    [Header("Audio")]
    [SerializeField] private AudioSource uiAudioSource;
    [SerializeField] private AudioClip clickClip;

    [Header("Input / Selection")]
    [SerializeField] private bool autoSelectStartButton = true;

    private void OnEnable()
    {
        if (startButton != null)
        {
            startButton.onClick.AddListener(HandleStartInvoked);
        }
        else
        {
            Debug.LogWarning($"[{nameof(MainMenuUIController)}] Start Button is not assigned.", this);
        }

        if (autoSelectStartButton)
        {
            SelectStartButton();
        }
    }

    private void OnDisable()
    {
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(HandleStartInvoked);
        }
    }

    /// <summary>
    /// Programmatically selects the Start button for keyboard/controller navigation.
    /// </summary>
    private void SelectStartButton()
    {
        if (startButton == null)
            return;

        var es = EventSystem.current;
        if (es == null)
        {
            Debug.LogWarning($"[{nameof(MainMenuUIController)}] No EventSystem found in scene; cannot auto-select Start button.", this);
            return;
        }

        // Clear any previous selection to ensure proper navigation.
        if (es.currentSelectedGameObject == startButton.gameObject)
            return;

        es.SetSelectedGameObject(startButton.gameObject);
    }

    /// <summary>
    /// Called when the Start button is clicked or submitted.
    /// </summary>
    private void HandleStartInvoked()
    {
        PlayClick();

        if (loadSceneOnStart)
        {
            if (string.IsNullOrWhiteSpace(sceneToLoad))
            {
                Debug.LogWarning($"[{nameof(MainMenuUIController)}] loadSceneOnStart is enabled, but sceneToLoad is empty.", this);
                return;
            }

            TryLoadScene(sceneToLoad);
        }
        else
        {
            // Placeholder for integration: user can hook additional logic
            Debug.Log($"[{nameof(MainMenuUIController)}] Start invoked. Scene loading disabled (loadSceneOnStart = false).", this);
        }
    }

    /// <summary>
    /// Attempts to load a scene by name, logging if the scene is not available.
    /// </summary>
    private void TryLoadScene(string sceneName)
    {
        // Application.CanStreamedLevelBeLoaded works both for build indices and scene names.
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogWarning(
                $"[{nameof(MainMenuUIController)}] Cannot load scene \"{sceneName}\". " +
                "Make sure it is added to Build Settings.",
                this);
            return;
        }

        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// Plays the configured click sound if available.
    /// </summary>
    private void PlayClick()
    {
        if (uiAudioSource == null || clickClip == null)
            return;

        uiAudioSource.PlayOneShot(clickClip);
    }
}