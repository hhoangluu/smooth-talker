using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ConversionSystem.Core
{
    public class UIManager : MonoBehaviour
    {
        [Header("Start Screen")]
        public GameObject StartPanel;
        public Button StartButton;

        [Header("Game Over Screen")]
        public GameObject GameOverPanel;
        public Button RestartButton;
        public TMP_Text GameOverScoreText;

        [Header("Game Play Screen")]
        public GameObject GamePlayPanel;

        [Header("HUD")]
        public TMP_Text MoneyText;
        public TMP_Text ScoreText;

        private void OnEnable()
        {
            StartButton.onClick.AddListener(OnStartClicked);
            RestartButton.onClick.AddListener(OnRestartClicked);

            GameManager.OnGameStarted += HandleGameStarted;
            GameManager.OnStatsChanged += UpdateStats;
            GameManager.OnGameOver += HandleGameOver;
        }

        private void OnDisable()
        {
            StartButton.onClick.RemoveListener(OnStartClicked);
            RestartButton.onClick.RemoveListener(OnRestartClicked);

            GameManager.OnGameStarted -= HandleGameStarted;
            GameManager.OnStatsChanged -= UpdateStats;
            GameManager.OnGameOver -= HandleGameOver;
        }

        private void Start()
        {
            ShowStartScreen();
        }

        private void ShowStartScreen()
        {
            StartPanel.SetActive(true);
            GameOverPanel.SetActive(false);
            MoneyText.gameObject.SetActive(false);
            ScoreText.gameObject.SetActive(false);
        }

        private void OnStartClicked()
        {
            GameManager.Instance.StartGame();
        }

        private void HandleGameStarted()
        {
            StartPanel.SetActive(false);
            GameOverPanel.SetActive(false);
            MoneyText.gameObject.SetActive(true);
            ScoreText.gameObject.SetActive(true);
        }

        private void UpdateStats(int money, int score)
        {
            MoneyText.text = $"${money}";
            ScoreText.text = $"Score: {score}";
        }

        private void HandleGameOver()
        {
            GameOverPanel.SetActive(true);
            GameOverScoreText.text = $"Final Score: {GameManager.Instance.Score}";
        }

        private void OnRestartClicked()
        {
            GameManager.Instance.RestartGame();
            ShowStartScreen();
        }
    }
}
