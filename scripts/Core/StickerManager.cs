using Godot;
using System.Collections.Generic;
using System.Text.Json;
using FileAccess = Godot.FileAccess;

// 贴纸数据模型
public class StickerData
{
    public int Id { get; set; } = 0;
    public string Name { get; set; } = "新贴纸";
    // 贴纸对应的图片文件名（位于项目 sprites 目录下）
    public string ImageFile { get; set; } = "";
}

// 贴纸管理器，复用 CharacterManager 的数据与存储结构
public static class StickerManager
{
    public static List<StickerData> Stickers { get; private set; } = new List<StickerData>();

    public static void LoadStickers(string path)
    {
        if (!FileAccess.FileExists(path)) {
            Stickers = new List<StickerData>();
            return;
        }
        
        try {
            using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
            string json = file.GetAsText();
            Stickers = JsonSerializer.Deserialize<List<StickerData>>(json) ?? new List<StickerData>();
        } catch {
            Stickers = new List<StickerData>();
        }
    }

    public static void SaveStickers(string path)
    {
        string json = JsonSerializer.Serialize(Stickers, new JsonSerializerOptions { WriteIndented = true });
        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
        if (file != null)
        {
            file.StoreString(json);
        }
    }

    public static void AddSticker(string name)
    {
        int newId = Stickers.Count > 0 ? Stickers[^1].Id + 1 : 0;
        Stickers.Add(new StickerData { Id = newId, Name = name });
    }

    public static void RemoveSticker(int id)
    {
        Stickers.RemoveAll(s => s.Id == id);
    }
}
