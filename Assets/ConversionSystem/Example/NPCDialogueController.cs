using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ConversionSystem.Config;
using ConversionSystem.Core;
using ConversionSystem.Data;
using ConversionSystem.Services;

namespace ConversionSystem.Example
{
    public class NPCDialogueController : MonoBehaviour
    {
        [Header("AI Provider")]
        public AIProvider SelectedProvider = AIProvider.Gemini;
        public AIServiceConfig GeminiConfig;
        public AIServiceConfig ChatGPTConfig;
        public AIServiceConfig MistralConfig;

        [Header("Personality")]
        public PersonalityConfig Personality;

        [Header("Player")]
        public PlayerType CurrentPlayerType = PlayerType.Default;

        [Header("Game Settings")]
        public int MaxTurns = 3;

        [Header("UI - NPC Panel")]
        public GameObject NPCPanel;
        public TMP_Text DialogueText;
        public Button ContinueButton;

        [Header("UI - Player Panel")]
        public GameObject PlayerPanel;
        public TMP_InputField PlayerInputField;
        public Button SubmitButton;

        private AIService _aiService;
        private List<DialogueEntry> _history;
        private int _currentTurn;
        private bool _roundEnded;

        private void Start()
        {
            var config = GetSelectedConfig();
            _aiService = new AIService(config);
        }

        private AIServiceConfig GetSelectedConfig()
        {
            return SelectedProvider switch
            {
                AIProvider.Gemini => GeminiConfig,
                AIProvider.ChatGPT => ChatGPTConfig,
                AIProvider.Mistral => MistralConfig,
                _ => GeminiConfig
            };
        }

        private void OnEnable()
        {
            SubmitButton.onClick.AddListener(OnSubmitClicked);
            ContinueButton.onClick.AddListener(OnContinueClicked);
        }

        private void OnDisable()
        {
            SubmitButton.onClick.RemoveListener(OnSubmitClicked);
            ContinueButton.onClick.RemoveListener(OnContinueClicked);
        }

        private void ShowNPCPanel()
        {
            NPCPanel.SetActive(true);
            PlayerPanel.SetActive(false);
        }

        private void ShowPlayerPanel()
        {
            NPCPanel.SetActive(false);
            PlayerPanel.SetActive(true);
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

        public void StartNewRound()
        {
            _history = new List<DialogueEntry>();
            _currentTurn = 1;
            _roundEnded = false;

            DisplayDialogue(Personality.OpeningDialogue);
        }

        private void DisplayDialogue(string text)
        {
            DialogueText.text = text;
            ShowNPCPanel();
        }

        private string GetSpecificBehavior(PlayerType playerType)
        {
            return playerType switch
            {
                PlayerType.HotGirl => Personality.HotGirlBehavior,
                PlayerType.GrandMa => Personality.GrandMaBehavior,
                _ => Personality.DefaultBehavior
            };
        }

        public async void OnPlayerInput(string playerInput)
        {
            if (_roundEnded) return;

            var request = new AIRequestData
            {
                PersonalityDescription = Personality.PersonalityPrompt,
                SpecificBehavior = GetSpecificBehavior(CurrentPlayerType),
                PlayerCharacter = CurrentPlayerType.ToString(),
                RaiseSuspicionTriggers = Personality.RaiseSuspicionTriggers,
                LowerSuspicionTriggers = Personality.LowerSuspicionTriggers,
                Catchphrases = string.Join("\n- ", Personality.Catchphrases ?? new string[0]),
                CurrentTurn = _currentTurn,
                MaxTurns = MaxTurns,
                PlayerInput = playerInput,
                History = _history
            };

            var response = await _aiService.SendRequestAsync(request);

            if (response == null)
            {
                Debug.LogError("Failed to get AI response");
                return;
            }

            // Update history
            _history.Add(new DialogueEntry("Player", playerInput));
            _history.Add(new DialogueEntry("Cop", response.Dialogue));
            Debug.Log($"(Debug - Score: {response.LeniencyScore} | Decision: {response.Decision})");

            // Check round end
            if (_currentTurn >= MaxTurns || response.IsFinalDecision)
            {
                _roundEnded = true;
                DisplayDialogue(response.Dialogue);
                GameManager.Instance.OnRoundResult(response.Decision);
                return;
            }

            // Display response
            DisplayDialogue(response.Dialogue);

            _currentTurn++;
        }
    }
}
