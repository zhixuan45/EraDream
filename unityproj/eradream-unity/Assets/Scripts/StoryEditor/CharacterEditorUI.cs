using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EraDream.Core;
using EraDream.Core.Models;
using EraDream.Services;

namespace EraDream.StoryEditor
{
    // 角色数据库可视化编辑器界面 UI
    public class CharacterEditorUI : MonoBehaviour
    {
        [SerializeField] private TMP_InputField characterIdInput;
        [SerializeField] private TMP_InputField characterNameInput;
        [SerializeField] private TMP_InputField avatarPathInput;
        [SerializeField] private Button addCharacterButton;
        [SerializeField] private Button removeCharacterButton;
        [SerializeField] private Transform characterListContainer;
        [SerializeField] private GameObject characterItemPrefab;

        private void Start()
        {
            if (addCharacterButton != null) addCharacterButton.onClick.AddListener(OnAddCharacterClicked);
            if (removeCharacterButton != null) removeCharacterButton.onClick.AddListener(OnRemoveCharacterClicked);

            if (CharacterManager.Instance != null)
            {
                CharacterManager.Instance.OnCharacterListChanged += RefreshList;
                RefreshList();
            }
        }

        private void OnDestroy()
        {
            if (CharacterManager.Instance != null)
            {
                CharacterManager.Instance.OnCharacterListChanged -= RefreshList;
            }
        }

        private void OnAddCharacterClicked()
        {
            string id = characterIdInput != null ? characterIdInput.text : "";
            string name = characterNameInput != null ? characterNameInput.text : "";
            string avatar = avatarPathInput != null ? avatarPathInput.text : "";

            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(name))
            {
                ErrorNotifierUI.Instance?.ShowToast("ID 与角色姓名不能为空!");
                return;
            }

            var data = new CharacterData
            {
                Id = id,
                Name = name,
                DefaultAvatarPath = avatar
            };

            CharacterManager.Instance?.AddOrUpdateCharacter(data);
            ErrorNotifierUI.Instance?.ShowToast($"成功保存角色: {name}");
        }

        private void OnRemoveCharacterClicked()
        {
            string id = characterIdInput != null ? characterIdInput.text : "";
            if (!string.IsNullOrEmpty(id) && CharacterManager.Instance != null)
            {
                CharacterManager.Instance.RemoveCharacter(id);
                ErrorNotifierUI.Instance?.ShowToast($"删除角色: {id}");
            }
        }

        public void RefreshList()
        {
            UIUtils.ClearChildren(characterListContainer);
            if (characterListContainer == null || characterItemPrefab == null || CharacterManager.Instance == null) return;

            foreach (var kvp in CharacterManager.Instance.Characters)
            {
                var obj = Instantiate(characterItemPrefab, characterListContainer);
                var tmp = obj.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null) tmp.text = $"{kvp.Value.Name} ({kvp.Key})";

                var btn = obj.GetComponent<Button>();
                if (btn != null)
                {
                    var data = kvp.Value;
                    btn.onClick.AddListener(() =>
                    {
                        if (characterIdInput != null) characterIdInput.text = data.Id;
                        if (characterNameInput != null) characterNameInput.text = data.Name;
                        if (avatarPathInput != null) avatarPathInput.text = data.DefaultAvatarPath;
                    });
                }
            }
        }
    }
}
