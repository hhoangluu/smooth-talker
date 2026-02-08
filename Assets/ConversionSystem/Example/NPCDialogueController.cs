using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ConversionSystem.Core;
using ConversionSystem.Data;
using ConversionSystem.Services;

namespace ConversionSystem.Example
{
    public class NPCDialogueController : MonoBehaviour
    {
        [Header("UI - NPC Panel")]
        public GameObject NPCPanel;
        public TMP_Text DialogueText;
        public Button ContinueButton;

        [Header("UI - Player Panel")]
        public GameObject PlayerPanel;
        public TMP_InputField PlayerInputField;
        public Button SubmitButton;

        [Header("Settings")]
        public bool EnableTTS = true;

        private List<DialogueEntry> _history = new();
        private int _currentTurn;
        private bool _roundEnded;

        private void OnEnable()
        {
            SubmitButton.onClick.AddListener(OnSubmitClicked);
            ContinueButton.onClick.AddListener(OnContinueClicked);
            StartNewRound();
        }

        private void OnDisable()
        {
            SubmitButton.onClick.RemoveListener(OnSubmitClicked);
            ContinueButton.onClick.RemoveListener(OnContinueClicked);
        }

        private void ShowNPCPanel()
        {
            // NPCPanel.SetActive(true);
            // PlayerPanel.SetActive(false);
        }

        private void ShowPlayerPanel()
        {
            // NPCPanel.SetActive(false);
            // PlayerPanel.SetActive(true);
            PlayerInputField.text = "";
            PlayerInputField.ActivateInputField();
        }

        public void HidePanels()
        {
            NPCPanel.SetActive(false);
            PlayerPanel.SetActive(false);
        }

        private void OnContinueClicked()
        {
            if (!_roundEnded)
                ShowPlayerPanel();
        }

        private void OnSubmitClicked()
        {
            if (string.IsNullOrEmpty(PlayerInputField.text)) return;
            OnPlayerInput(PlayerInputField.text);
            PlayerInputField.text = "";
        }

        public void OnPressRecord()
        {
            VoiceInput.Instance?.StartRecording();
        }

        public async void OnReleaseRecord()
        {
            byte[] wavData = VoiceInput.Instance?.StopRecording();
            if (wavData == null) return;

            string text = await AIService.Instance.TranscribeAudioAsync(wavData);
            if (string.IsNullOrEmpty(text)) return;

            PlayerInputField.text = text;
            OnPlayerInput(text);
        }

        public async void StartNewRound()
        {
            _history = new List<DialogueEntry>();
            _currentTurn = 1;
            _roundEnded = false;

            await DisplayDialogue(AIService.Instance.Personality.OpeningDialogue);
        }

        private async System.Threading.Tasks.Task DisplayDialogue(string text)
        {
            if (EnableTTS && TextToSpeechDeepgram.Instance != null)
                await TextToSpeechDeepgram.Instance.SpeakAsync(text);

            DialogueText.text = text;
            ShowNPCPanel();
            PlayerInputField.text = "";
        }

        private async System.Threading.Tasks.Task WaitForSpeechEnd()
        {
            if (!EnableTTS || TextToSpeechDeepgram.Instance == null) return;
            while (TextToSpeechDeepgram.Instance.audioSource.isPlaying)
                await System.Threading.Tasks.Task.Yield();
        }

        private string GetSpecificBehavior(PlayerType playerType)
        {
            var personality = AIService.Instance.Personality;
            return playerType switch
            {
                PlayerType.HotGirl => personality.HotGirlBehavior,
                PlayerType.GrandMa => personality.GrandMaBehavior,
                _ => personality.DefaultBehavior
            };
        }

        public async void OnPlayerInput(string playerInput)
        {
            if (_roundEnded) return;

            var ai = AIService.Instance;
            var request = new AIRequestData
            {
                PersonalityDescription = ai.Personality.PersonalityPrompt,
                SpecificBehavior = GetSpecificBehavior(ai.CurrentPlayerType),
                PlayerCharacter = ai.CurrentPlayerType.ToString(),
                RaiseSuspicionTriggers = ai.Personality.RaiseSuspicionTriggers,
                LowerSuspicionTriggers = ai.Personality.LowerSuspicionTriggers,
                Catchphrases = string.Join("\n- ", ai.Personality.Catchphrases ?? new string[0]),
                CurrentTurn = _currentTurn,
                MaxTurns = ai.MaxTurns,
                PlayerInput = playerInput,
                History = _history
            };

            var response = await ai.SendRequestAsync(request);

            if (response == null)
            {
                Debug.LogError("Failed to get AI response");
                return;
            }

            _history.Add(new DialogueEntry("Player", playerInput));
            _history.Add(new DialogueEntry("Cop", response.Dialogue));
            Debug.Log($"(Debug - Score: {response.LeniencyScore} | Decision: {response.Decision})");
            if (_currentTurn >= ai.MaxTurns || response.IsFinalDecision)
            {
                _roundEnded = true;
                await DisplayDialogue(response.Dialogue);
                await WaitForSpeechEnd();
                await System.Threading.Tasks.Task.Delay(3000);
                GameManager.Instance.OnRoundResult(response.Decision);
                return;
            }

            await DisplayDialogue(response.Dialogue);

            _currentTurn++;
        }
    }
}
