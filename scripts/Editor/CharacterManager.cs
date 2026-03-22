using Godot;
using System.Collections.Generic;
using System.Text.Json;
using FileAccess = Godot.FileAccess;

public class CharacterData
{
    public int Id { get; set; } = 0;
    public string Name { get; set; } = "新角色";
    public string DefaultSprite { get; set; } = "";
    // 表情/状态映射: "Angry" -> "char_a_angry.png"
    public Dictionary<string, string> Expressions { get; set; } = new Dictionary<string, string>();
}

public static class CharacterManager
{
    public static List<CharacterData> Characters { get; private set; } = new List<CharacterData>();

    public static void LoadCharacters(string path)
    {
        if (!FileAccess.FileExists(path)) {
            Characters = new List<CharacterData>();
            return;
        }
        
        try {
            using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
            string json = file.GetAsText();
            Characters = JsonSerializer.Deserialize<List<CharacterData>>(json) ?? new List<CharacterData>();
        } catch {
            Characters = new List<CharacterData>();
        }
    }

    public static void SaveCharacters(string path)
    {
        string json = JsonSerializer.Serialize(Characters, new JsonSerializerOptions { WriteIndented = true });
        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
        if (file != null)
        {
            file.StoreString(json);
        }
    }

    public static void AddCharacter(string name)
    {
        int newId = Characters.Count > 0 ? Characters[^1].Id + 1 : 0;
        Characters.Add(new CharacterData { Id = newId, Name = name });
    }

    public static void RemoveCharacter(int id)
    {
        Characters.RemoveAll(c => c.Id == id);
    }
}
