using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EraDream.Core.Models;

namespace EraDream.Game
{
    // 养成主界面 UI 绑定器
    public class SimulationMainScreenUI : MonoBehaviour
    {
        [Header("Stats Texts")]
        [SerializeField] private TextMeshProUGUI speedText;
        [SerializeField] private TextMeshProUGUI staminaText;
        [SerializeField] private TextMeshProUGUI powerText;
        [SerializeField] private TextMeshProUGUI gutsText;
        [SerializeField] private TextMeshProUGUI wisdomText;
        [SerializeField] private TextMeshProUGUI turnText;
        [SerializeField] private Slider energySlider;

        [Header("Training Buttons")]
        [SerializeField] private Button speedButton;
        [SerializeField] private Button staminaButton;
        [SerializeField] private Button powerButton;
        [SerializeField] private Button gutsButton;
        [SerializeField] private Button wisdomButton;
        [SerializeField] private Button restButton;

        private void Start()
        {
            if (speedButton != null) speedButton.onClick.AddListener(() => OnTrainClicked("speed"));
            if (staminaButton != null) staminaButton.onClick.AddListener(() => OnTrainClicked("stamina"));
            if (powerButton != null) powerButton.onClick.AddListener(() => OnTrainClicked("power"));
            if (gutsButton != null) gutsButton.onClick.AddListener(() => OnTrainClicked("guts"));
            if (wisdomButton != null) wisdomButton.onClick.AddListener(() => OnTrainClicked("wisdom"));
            if (restButton != null) restButton.onClick.AddListener(OnRestClicked);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateUpdated += RefreshUI;
                RefreshUI(GameManager.Instance.CurrentState);
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateUpdated -= RefreshUI;
            }
        }

        private void OnTrainClicked(string trainType)
        {
            GameManager.Instance?.PerformTraining(trainType);
        }

        private void OnRestClicked()
        {
            GameManager.Instance?.Rest();
        }

        public void RefreshUI(SimulationGameState state)
        {
            if (state == null) return;
            var uma = state.Uma;
            var player = state.Player;

            if (speedText != null) speedText.text = uma.Speed.ToString();
            if (staminaText != null) staminaText.text = uma.Stamina.ToString();
            if (powerText != null) powerText.text = uma.Power.ToString();
            if (gutsText != null) gutsText.text = uma.Guts.ToString();
            if (wisdomText != null) wisdomText.text = uma.Wisdom.ToString();
            if (turnText != null) turnText.text = $"Turn: {player.Turn} / {player.MaxTurns}";

            if (energySlider != null)
            {
                energySlider.maxValue = uma.MaxEnergy;
                energySlider.value = uma.Energy;
            }
        }
    }
}
