using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EraDream.Screens
{
    // 主菜单 UI 控制器
    public class MainMenuScreenUI : MonoBehaviour
    {
        [SerializeField] private Button startStoryPlayerButton;
        [SerializeField] private Button startSimulationButton;
        [SerializeField] private Button openEditorButton;
        [SerializeField] private Button openSettingsButton;
        [SerializeField] private Button quitGameButton;

        [SerializeField] private GameObject settingsOverlayPanel;

        private void Start()
        {
            if (startStoryPlayerButton != null)
                startStoryPlayerButton.onClick.AddListener(() => SceneManager.LoadScene("StoryPlayerScene"));

            if (startSimulationButton != null)
                startSimulationButton.onClick.AddListener(() => SceneManager.LoadScene("SimulationScene"));

            if (openEditorButton != null)
                openEditorButton.onClick.AddListener(() => SceneManager.LoadScene("StoryEditorScene"));

            if (openSettingsButton != null && settingsOverlayPanel != null)
                openSettingsButton.onClick.AddListener(() => settingsOverlayPanel.SetActive(true));

            if (quitGameButton != null)
                quitGameButton.onClick.AddListener(QuitGame);
        }

        private void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
