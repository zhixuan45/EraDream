using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EraDream.Services;

namespace EraDream.Screens
{
    // 外部剧本与扩展包选择器 UI 控制器
    public class StorySelectorScreenUI : MonoBehaviour
    {
        [SerializeField] private Transform storyListContainer;
        [SerializeField] private GameObject storyItemPrefab;
        [SerializeField] private Button openExternalFileButton;

        private void Start()
        {
            if (openExternalFileButton != null)
            {
                openExternalFileButton.onClick.AddListener(OnOpenExternalFileClicked);
            }
            ScanLocalStoryPacks();
        }

        public void ScanLocalStoryPacks()
        {
            UIUtils.ClearChildren(storyListContainer);
            if (storyListContainer == null || storyItemPrefab == null) return;

            string packDir = Path.Combine(Application.persistentDataPath, "StoryPacks");
            if (!Directory.Exists(packDir)) Directory.CreateDirectory(packDir);

            string[] files = Directory.GetFiles(packDir, "*.era");
            foreach (var file in files)
            {
                string fileName = Path.GetFileName(file);
                var obj = Instantiate(storyItemPrefab, storyListContainer);
                var tmp = obj.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null) tmp.text = fileName;

                var btn = obj.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.AddListener(() => OnStoryPackSelected(file));
                }
            }
        }

        private void OnStoryPackSelected(string packPath)
        {
            ErrorNotifierUI.Instance?.ShowToast($"加载剧本包: {Path.GetFileName(packPath)}");
        }

        private void OnOpenExternalFileClicked()
        {
            ErrorNotifierUI.Instance?.ShowToast("请选择外置 .era / .json 剧情文件");
        }
    }
}
