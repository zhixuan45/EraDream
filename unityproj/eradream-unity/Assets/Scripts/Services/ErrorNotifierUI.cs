using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EraDream.Services
{
    // 全局提示框与错误通知控制单例 (Toast Notifications)
    public class ErrorNotifierUI : MonoBehaviour
    {
        public static ErrorNotifierUI Instance { get; private set; }

        [Header("Toast Components")]
        [SerializeField] private GameObject toastPanel;
        [SerializeField] private TextMeshProUGUI toastText;
        [SerializeField] private float defaultDuration = 3.0f;

        [Header("Modal Error Dialog")]
        [SerializeField] private GameObject dialogPanel;
        [SerializeField] private TextMeshProUGUI dialogTitleText;
        [SerializeField] private TextMeshProUGUI dialogMessageText;
        [SerializeField] private Button dialogConfirmButton;

        private Coroutine _toastCoroutine;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                if (toastPanel != null) toastPanel.SetActive(false);
                if (dialogPanel != null) dialogPanel.SetActive(false);

                if (dialogConfirmButton != null)
                {
                    dialogConfirmButton.onClick.AddListener(HideDialog);
                }
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void ShowToast(string message, float duration = 3.0f)
        {
            if (toastPanel == null || toastText == null)
            {
                Debug.Log($"[Toast] {message}");
                return;
            }

            if (_toastCoroutine != null) StopCoroutine(_toastCoroutine);
            _toastCoroutine = StartCoroutine(CoShowToast(message, duration));
        }

        private IEnumerator CoShowToast(string message, float duration)
        {
            toastText.text = message;
            toastPanel.SetActive(true);
            yield return new WaitForSeconds(duration);
            toastPanel.SetActive(false);
        }

        public void ShowErrorDialog(string title, string message)
        {
            if (dialogPanel == null)
            {
                Debug.LogError($"[ErrorDialog] {title}: {message}");
                return;
            }

            if (dialogTitleText != null) dialogTitleText.text = title;
            if (dialogMessageText != null) dialogMessageText.text = message;
            dialogPanel.SetActive(true);
        }

        public void HideDialog()
        {
            if (dialogPanel != null) dialogPanel.SetActive(false);
        }
    }
}
