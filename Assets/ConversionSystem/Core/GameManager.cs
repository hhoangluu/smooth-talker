using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using ConversionSystem.Data;
using ConversionSystem.Example;

namespace ConversionSystem.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("References")]
        public NPCDialogueController DialogueController;

        [Header("Economy")]
        public int StartingMoney = 2000;
        public int TicketPenalty = 500;
        public int WarningBonus = 100;

        [Header("Scene Transition")]
        public Image FadeImage;
        public float FadeDuration = 0.5f;

        public int Money => _money;
        public int Score => _score;

        public static event Action<int, int> OnStatsChanged;
        public static event Action OnGameStarted;
        public static event Action OnGameOver;
        public static event Action<string> OnRoundEnded;

        private int _money;
        private int _score;
        private bool _isTransitioning;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (FadeImage != null)
            {
                FadeImage.color = new Color(0, 0, 0, 0);
                FadeImage.raycastTarget = false;
            }
        }

        public void StartGame()
        {
            _money = StartingMoney;
            _score = 0;
            OnStatsChanged?.Invoke(_money, _score);
            OnGameStarted?.Invoke();
            DialogueController.StartNewRound();
            TransitionToScene("CutScene");
        }

        public void OnRoundResult(DecisionType decision)
        {
            if (decision == DecisionType.Warning)
            {
                _score += WarningBonus;
                OnStatsChanged?.Invoke(_money, _score);
                OnRoundEnded?.Invoke("You got off with a WARNING!");
                DialogueController.StartNewRound();
            }
            else
            {
                _money -= TicketPenalty;
                if (_money < 0) _money = 0;
                OnStatsChanged?.Invoke(_money, _score);

                if (_money <= 0)
                {
                    OnRoundEnded?.Invoke($"TICKET! -${TicketPenalty}. You're broke!");
                    OnGameOver?.Invoke();
                }
                else
                {
                    OnRoundEnded?.Invoke($"TICKET! -${TicketPenalty}");
                    DialogueController.StartNewRound();
                }
            }
        }

        public void RestartGame()
        {
            _money = StartingMoney;
            _score = 0;
            OnStatsChanged?.Invoke(_money, _score);
        }

        public void TransitionToScene(string sceneName, Action onSceneLoaded = null)
        {
            if (_isTransitioning) return;
            StartCoroutine(FadeAndLoadScene(sceneName, onSceneLoaded));
        }

        private IEnumerator FadeAndLoadScene(string sceneName, Action onSceneLoaded)
        {
            _isTransitioning = true;
            yield return StartCoroutine(Fade(0f, 1f));
            yield return SceneManager.LoadSceneAsync(sceneName);
            onSceneLoaded?.Invoke();
            yield return StartCoroutine(Fade(1f, 0f));
            _isTransitioning = false;
        }

        private IEnumerator Fade(float from, float to)
        {
            FadeImage.raycastTarget = true;
            float elapsed = 0f;

            while (elapsed < FadeDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(from, to, elapsed / FadeDuration);
                FadeImage.color = new Color(0, 0, 0, alpha);
                yield return null;
            }

            FadeImage.color = new Color(0, 0, 0, to);

            if (to == 0f)
                FadeImage.raycastTarget = false;
        }
    }
}
