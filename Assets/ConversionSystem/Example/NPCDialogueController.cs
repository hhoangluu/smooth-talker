using System.Collections;
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
        public TMP_Text DialogueText;
        public Image NPCAvatar;

        [Header("UI - Player Panel")]
        public TMP_InputField PlayerInputField;
        public Button SubmitButton;
        public Image PlayerAvatar;

        [Header("UI - Loading")]
        public GameObject LoadingPanel;

        [Header("Progress Bar")]
        public Image ProgressBarFill;
        public float BarAnimDuration = 0.5f;

        [Header("Settings")]
        public bool EnableTTS = true;
        public TextToSpeechDeepgram TTS;

        private List<DialogueEntry> _history = new();
        private int _currentTurn;
        private bool _roundEnded;

        private void OnEnable()
        {
            SubmitButton.onClick.AddListener(OnSubmitClicked);
            StartNewRound();
        }

        private void OnDisable()
        {
            SubmitButton.onClick.RemoveListener(OnSubmitClicked);
        }

        private void SetLoading(bool loading)
        {
            if (LoadingPanel != null) LoadingPanel.SetActive(loading);
            SubmitButton.interactable = !loading;
            PlayerInputField.interactable = !loading;
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

            SetLoading(true);
            string text = await AIService.Instance.TranscribeAudioAsync(wavData);
            if (string.IsNullOrEmpty(text))
            {
                SetLoading(false);
                return;
            }

            PlayerInputField.text = text;
            OnPlayerInput(text);
        }

        public async void StartNewRound()
        {
            _history = new List<DialogueEntry>();
            _currentTurn = 1;
            _roundEnded = false;
            UpdateProgressBar(50);

            var character = GameManager.Instance.CurrentCharacter;
            var npc = GameManager.Instance.CurrentNPC;
            Debug.Log($"You are: {character.CharacterId} ({character.PlayerType})");

            if (NPCAvatar != null && npc.Avatar != null)
                NPCAvatar.sprite = npc.Avatar;

            if (PlayerAvatar != null && character.Avatar != null)
                PlayerAvatar.sprite = character.Avatar;
            DialogueText.text = npc.OpeningDialogue;

            await DisplayDialogue(npc.OpeningDialogue);
        }

        private async System.Threading.Tasks.Task DisplayDialogue(string text)
        {
            if (EnableTTS)
            {
                // while (TTS == null)
                //     await System.Threading.Tasks.Task.Yield();

                await TTS.SpeakAsync(text, GameManager.Instance.CurrentNPC.VoiceModel);
            }

            DialogueText.text = text;
            ShowNPCPanel();
            PlayerInputField.text = "";
        }

        private async System.Threading.Tasks.Task WaitForSpeechEnd()
        {
            if (!EnableTTS || TTS == null) return;
            while (TTS.audioSource.isPlaying)
                await System.Threading.Tasks.Task.Yield();
        }

        private string GetSpecificBehavior(PlayerType playerType)
        {
            var personality = GameManager.Instance.CurrentNPC;
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

            SetLoading(true);
            var ai = AIService.Instance;
            var request = new AIRequestData
            {
                PersonalityDescription = GameManager.Instance.CurrentNPC.PersonalityPrompt,
                SpecificBehavior = GetSpecificBehavior(GameManager.Instance.CurrentCharacter.PlayerType),
                PlayerCharacter = GameManager.Instance.CurrentCharacter.PlayerType.ToString(),
                RaiseSuspicionTriggers = GameManager.Instance.CurrentNPC.RaiseSuspicionTriggers,
                LowerSuspicionTriggers = GameManager.Instance.CurrentNPC.LowerSuspicionTriggers,
                Catchphrases = string.Join("\n- ", GameManager.Instance.CurrentNPC.Catchphrases ?? new string[0]),
                CurrentTurn = _currentTurn,
                MaxTurns = ai.MaxTurns,
                PlayerInput = playerInput,
                History = _history
            };

            var response = await ai.SendRequestAsync(request);

            if (response == null)
            {
                Debug.LogError("Failed to get AI response");
                SetLoading(false);
                return;
            }



            _history.Add(new DialogueEntry("Player", playerInput));
            _history.Add(new DialogueEntry("Cop", response.Dialogue));
            Debug.Log($"(Debug - Score: {response.LeniencyScore} | Decision: {response.Decision})");
            UpdateProgressBar(response.LeniencyScore);
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
            SetLoading(false);
            _currentTurn++;
        }

        private Coroutine _barAnim;

        private void UpdateProgressBar(int leniencyScore)
        {
            if (ProgressBarFill == null) return;

            float targetFill = leniencyScore / 100f;

            Color targetColor;
            if (leniencyScore < 40)
                targetColor = Color.red;
            else if (leniencyScore < 60)
                targetColor = Color.yellow;
            else
                targetColor = Color.green;

            if (_barAnim != null) StopCoroutine(_barAnim);
            _barAnim = StartCoroutine(AnimateProgressBar(targetFill, targetColor));
        }

        private IEnumerator AnimateProgressBar(float targetFill, Color targetColor)
        {
            float startFill = ProgressBarFill.fillAmount;
            Color startColor = ProgressBarFill.color;
            float elapsed = 0f;

            while (elapsed < BarAnimDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / BarAnimDuration;
                t = t * t * (3f - 2f * t);

                ProgressBarFill.fillAmount = Mathf.Lerp(startFill, targetFill, t);
                ProgressBarFill.color = Color.Lerp(startColor, targetColor, t);
                yield return null;
            }

            ProgressBarFill.fillAmount = targetFill;
            ProgressBarFill.color = targetColor;
        }
    }
}
