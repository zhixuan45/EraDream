using System;
using System.IO;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EraDream.Core.Models;
using EraDream.Services;

namespace EraDream.Screens
{
    public class SaveSlotData
    {
        public int SlotIndex { get; set; }
        public string SaveTime { get; set; } = "";
        public string ChapterTitle { get; set; } = "";
        public string PreviewText { get; set; } = "";
    }

    // 存档与读档槽位 UI 控制器
    public class SaveSlotScreenUI : MonoBehaviour
    {
        public enum Mode { Save, Load }

        [SerializeField] private Mode currentMode = Mode.Save;
        [SerializeField] private Transform slotContainer;
        [SerializeField] private GameObject slotPrefab;
        [SerializeField] private Button backButton;

        private void Start()
        {
            if (backButton != null)
            {
                backButton.onClick.AddListener(OnBackClicked);
            }
            RefreshSlots();
        }

        public void RefreshSlots()
        {
            UIUtils.ClearChildren(slotContainer);
            if (slotContainer == null || slotPrefab == null) return;

            string saveDir = Path.Combine(Application.persistentDataPath, "Saves");
            if (!Directory.Exists(saveDir)) Directory.CreateDirectory(saveDir);

            for (int i = 1; i <= 9; i++)
            {
                int slotIndex = i;
                string filePath = Path.Combine(saveDir, $"save_slot_{slotIndex}.json");
                SaveSlotData data = FileIOManager.LoadJson<SaveSlotData>(filePath);

                var obj = Instantiate(slotPrefab, slotContainer);
                var texts = obj.GetComponentsInChildren<TextMeshProUGUI>();
                if (texts.Length > 0)
                {
                    texts[0].text = data != null ? $"Slot {slotIndex}: {data.ChapterTitle}" : $"Slot {slotIndex}: [空槽位]";
                }

                var btn = obj.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.AddListener(() => OnSlotSelected(slotIndex, filePath, data));
                }
            }
        }

        private void OnSlotSelected(int slotIndex, string filePath, SaveSlotData data)
        {
            if (currentMode == Mode.Save)
            {
                var saveData = new SaveSlotData
                {
                    SlotIndex = slotIndex,
                    SaveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    ChapterTitle = "第一章 梦开始的地方",
                    PreviewText = "剧情保存进度记录..."
                };
                FileIOManager.SaveJson(filePath, saveData);
                ErrorNotifierUI.Instance?.ShowToast($"成功保存至槽位 {slotIndex}");
                RefreshSlots();
            }
            else
            {
                if (data == null)
                {
                    ErrorNotifierUI.Instance?.ShowToast($"槽位 {slotIndex} 为空");
                }
                else
                {
                    ErrorNotifierUI.Instance?.ShowToast($"正在读取槽位 {slotIndex}...");
                }
            }
        }

        private void OnBackClicked()
        {
            gameObject.SetActive(false);
        }
    }
}
