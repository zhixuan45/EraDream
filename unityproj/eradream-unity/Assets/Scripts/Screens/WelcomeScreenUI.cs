using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EraDream.Screens
{
    // 欢迎界面 UI 控制器
    public class WelcomeScreenUI : MonoBehaviour
    {
        [SerializeField] private Button startTouchButton;
        [SerializeField] private string nextSceneName = "MainMenuScene";

        private void Start()
        {
            if (startTouchButton != null)
            {
                startTouchButton.onClick.AddListener(EnterMainMenu);
            }
        }

        private void Update()
        {
            // 响应点击或按键
            if (Input.anyKeyDown)
            {
                EnterMainMenu();
            }
        }

        public void EnterMainMenu()
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }
}
