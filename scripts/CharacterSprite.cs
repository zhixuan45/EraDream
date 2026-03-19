using Godot;
using System.Collections.Generic;

public partial class CharacterSprite : Control
{
    private TextureRect _textureRect;
    public int CurrentCharacterId { get; private set; } = -1;

    public override void _Ready()
    {
        _textureRect = new TextureRect {
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Ignore
        };
        _textureRect.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(_textureRect);
    }

    public void UpdateCharacter(int charId, string emotion = "Neutral", bool silhouette = false)
    {
        CurrentCharacterId = charId;
        var charData = CharacterManager.Characters.Find(c => c.Id == charId);
        if (charData == null) {
            _textureRect.Texture = null;
            return;
        }

        string fileName = charData.DefaultSprite;
        if (!string.IsNullOrEmpty(emotion) && charData.Expressions.ContainsKey(emotion))
        {
            fileName = charData.Expressions[emotion];
        }

        if (string.IsNullOrEmpty(fileName)) {
            _textureRect.Texture = null;
            return;
        }

        string path = ProjectManager.IsProjectOpened ? 
            System.IO.Path.Combine(ProjectManager.SpriteDir, fileName) : 
            "res://sprites/" + fileName;
            
        string absolutePath = ProjectSettings.GlobalizePath(path);
        
        // 关键修复：对于外部路径或 res 路径，使用 Image 实时加载
        if (Godot.FileAccess.FileExists(absolutePath))
        {
            var image = Image.LoadFromFile(absolutePath);
            var texture = ImageTexture.CreateFromImage(image);
            _textureRect.Texture = texture;
            _textureRect.SelfModulate = silhouette ? new Color(0, 0, 0, 1) : new Color(1, 1, 1, 1);
            GD.Print($"[Sprite] Loaded character {charId} from {absolutePath}");
        }
        else
        {
            GD.PrintErr($"[Sprite] File not found: {absolutePath}");
        }
    }
}
