using System;
using EraDream.Core.Models;
using UnityEngine;

namespace EraDream.Game
{
    // 养成与模拟游戏核心流程驱动器
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public SimulationGameState CurrentState { get; private set; } = new SimulationGameState();

        public event Action<SimulationGameState> OnGameStateUpdated;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void StartNewGame()
        {
            CurrentState = new SimulationGameState();
            OnGameStateUpdated?.Invoke(CurrentState);
        }

        public void PerformTraining(string trainType)
        {
            if (CurrentState == null) return;
            var uma = CurrentState.Uma;
            var player = CurrentState.Player;

            if (uma.Energy < 15)
            {
                Debug.LogWarning("[GameManager] 体力不足，训练效果降低或面临失败风险!");
            }

            switch (trainType.ToLower())
            {
                case "speed":
                    uma.Speed += 12;
                    uma.Power += 5;
                    uma.Energy = Mathf.Max(0, uma.Energy - 20);
                    break;
                case "stamina":
                    uma.Stamina += 12;
                    uma.Guts += 5;
                    uma.Energy = Mathf.Max(0, uma.Energy - 20);
                    break;
                case "power":
                    uma.Power += 12;
                    uma.Stamina += 5;
                    uma.Energy = Mathf.Max(0, uma.Energy - 20);
                    break;
                case "guts":
                    uma.Guts += 12;
                    uma.Speed += 4;
                    uma.Power += 4;
                    uma.Energy = Mathf.Max(0, uma.Energy - 22);
                    break;
                case "wisdom":
                    uma.Wisdom += 10;
                    uma.SkillPoints += 15;
                    uma.Energy = Mathf.Min(uma.MaxEnergy, uma.Energy + 5); // 智力训练恢复体力
                    break;
            }

            AdvanceTurn();
        }

        public void Rest()
        {
            if (CurrentState == null) return;
            var uma = CurrentState.Uma;
            uma.Energy = Mathf.Min(uma.MaxEnergy, uma.Energy + 50);
            AdvanceTurn();
        }

        private void AdvanceTurn()
        {
            CurrentState.Player.Turn++;
            OnGameStateUpdated?.Invoke(CurrentState);
        }
    }
}
