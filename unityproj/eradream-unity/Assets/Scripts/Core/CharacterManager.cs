using System;
using System.Collections.Generic;
using System.IO;
using EraDream.Core.Models;
using EraDream.Services;
using UnityEngine;

namespace EraDream.Core
{
    // 角色数据库与持久化管理器
    public class CharacterManager : MonoBehaviour
    {
        public static CharacterManager Instance { get; private set; }

        private readonly Dictionary<string, CharacterData> _characters = new Dictionary<string, CharacterData>();

        public IReadOnlyDictionary<string, CharacterData> Characters => _characters;

        public event Action OnCharacterListChanged;

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

        public void AddOrUpdateCharacter(CharacterData character)
        {
            if (character == null || string.IsNullOrEmpty(character.Id)) return;
            _characters[character.Id] = character;
            OnCharacterListChanged?.Invoke();
        }

        public CharacterData GetCharacter(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            _characters.TryGetValue(id, out var data);
            return data;
        }

        public bool RemoveCharacter(string id)
        {
            bool removed = _characters.Remove(id);
            if (removed) OnCharacterListChanged?.Invoke();
            return removed;
        }

        public void SaveToDirectory(string dirPath)
        {
            string filePath = Path.Combine(dirPath, "characters.json");
            FileIOManager.SaveJson(filePath, new List<CharacterData>(_characters.Values));
        }

        public void LoadFromDirectory(string dirPath)
        {
            string filePath = Path.Combine(dirPath, "characters.json");
            var list = FileIOManager.LoadJson<List<CharacterData>>(filePath);
            _characters.Clear();
            if (list != null)
            {
                foreach (var c in list)
                {
                    if (c != null && !string.IsNullOrEmpty(c.Id))
                    {
                        _characters[c.Id] = c;
                    }
                }
            }
            OnCharacterListChanged?.Invoke();
        }
    }
}
