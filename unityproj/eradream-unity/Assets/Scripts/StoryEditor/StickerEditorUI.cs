using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EraDream.Core;
using EraDream.Services;

namespace EraDream.StoryEditor
{
    // 贴纸/CG叠加图层编辑器 UI
    public class StickerEditorUI : MonoBehaviour
    {
        [SerializeField] private TMP_InputField slotInput;
        [SerializeField] private TMP_InputField pathInput;
        [SerializeField] private Slider scaleSlider;
        [SerializeField] private Toggle visibleToggle;
        [SerializeField] private Button applyButton;

        private void Start()
        {
            if (applyButton != null)
            {
                applyButton.onClick.AddListener(OnApplyClicked);
            }
        }

        private void OnApplyClicked()
        {
            int slot = int.TryParse(slotInput != null ? slotInput.text : "0", out var s) ? s : 0;
            string path = pathInput != null ? pathInput.text : "";
            float scale = scaleSlider != null ? scaleSlider.value : 1.0f;
            bool isVisible = visibleToggle != null && visibleToggle.isOn;

            StickerManager.Instance?.SetSticker(slot, path, new Vector2(0.5f, 0.5f), scale, isVisible);
            ErrorNotifierUI.Instance?.ShowToast($"更新贴纸图层 [{slot}]");
        }
    }
}
