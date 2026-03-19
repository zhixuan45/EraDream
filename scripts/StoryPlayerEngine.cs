using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UmaArchive.Editor.Nodes;
using UmaArchive.Core;

public partial class StoryPlayerEngine : Control
{
    private Label _nameLabel;
    private RichTextLabel _contentLabel;
    private VBoxContainer _choiceContainer;
    private Control _dialogueBox;
    private Button _interactButton;
    private TextureRect _backgroundRect;
    private AudioStreamPlayer _bgmPlayer;
    private Control _characterContainer;
    private Control _overlay;
    private Dictionary<int, CharacterSprite> _activeSprites = new Dictionary<int, CharacterSprite>();

    private List<BaseNodeData> _storyNodes = new List<BaseNodeData>();
    private BaseNodeData _currentNode;
    private bool _isTextAnimating = false;
    private float _textSpeed = 0.05f;
    private Tween _textTween;

    public static string CurrentStoryPath = "";
    public static List<BaseNodeData> PreviewNodes = null;
    public bool IsPreviewMode { get; set; } = false;

    [Signal] public delegate void StoryFinishedEventHandler();

    public override void _Ready()
    {
        _nameLabel = GetNode<Label>("UI_Layer/DialogueBox/NameLabel");
        _contentLabel = GetNode<RichTextLabel>("UI_Layer/DialogueBox/ContentLabel");
        _choiceContainer = GetNode<VBoxContainer>("UI_Layer/ChoiceContainer");
        _dialogueBox = GetNode<Control>("UI_Layer/DialogueBox");
        _interactButton = GetNode<Button>("UI_Layer/InteractButton");
        _backgroundRect = GetNode<TextureRect>("Background");
        _overlay = GetNode<Control>("ColorRectOverlay");
        
        // 背景层初始化
        _backgroundRect.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        _backgroundRect.StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered;
        _backgroundRect.SetAnchorsPreset(LayoutPreset.FullRect);
        _backgroundRect.MouseFilter = MouseFilterEnum.Ignore;

        _characterContainer = new Control { Name = "CharacterContainer" };
        _characterContainer.SetAnchorsPreset(LayoutPreset.FullRect);
        _characterContainer.MouseFilter = Control.MouseFilterEnum.Ignore;
        AddChild(_characterContainer);

        // 严格校正层级：背景(0) -> 遮罩(1) -> 立绘(2) -> UI
        MoveChild(_backgroundRect, 0);
        MoveChild(_overlay, 1);
        MoveChild(_characterContainer, 2);

        _bgmPlayer = new AudioStreamPlayer();
        AddChild(_bgmPlayer);

        _interactButton.Pressed += OnInteraction;

        if (PreviewNodes != null && PreviewNodes.Count > 0)
        {
            GD.Print("[Engine] Entering Preview Mode...");
            IsPreviewMode = true;
            _storyNodes = PreviewNodes;
            _currentNode = _storyNodes.FirstOrDefault(n => n is StartNodeData) ?? _storyNodes[0];
            ProcessCurrentNode();
            PreviewNodes = null; 
            return;
        }

        if (string.IsNullOrEmpty(CurrentStoryPath))
        {
            GD.PrintErr("[Engine] Story path is empty!");
            GetTree().ChangeSceneToFile("res://scenes/MainMenuScreen.tscn");
            return;
        }

        LoadStory(CurrentStoryPath);
    }

    private void LoadStory(string path)
    {
        _storyNodes = StoryNodeManager.LoadProject(path);
        if (_storyNodes.Count == 0) return;
        _currentNode = _storyNodes.FirstOrDefault(n => n is StartNodeData) ?? _storyNodes[0];
        ProcessCurrentNode();
    }

    private void ProcessCurrentNode()
    {
        if (_currentNode == null) { FinishStory(); return; }

        GD.Print($"[Engine] Node: {_currentNode.GetType().Name} (ID: {_currentNode.Id})");

        _choiceContainer.Hide();
        _dialogueBox.Show();
        foreach (Node child in _choiceContainer.GetChildren()) child.QueueFree();

        switch (_currentNode)
        {
            case StartNodeData start: GoToNextNode(start.NextNodeId); break;
            case EndNodeData end: FinishStory(end.EndType); break;
            case BackgroundNodeData bg: UpdateBackground(bg.BackgroundFile, bg.TransitionType); GoToNextNode(bg.NextNodeId); break;
            case DialogueNodeData dialogue: UpdateDialogueAndCharacter(dialogue); break;
            case NarrativeNodeData narrative: UpdateDialogueUI("", narrative.Content); break;
            case MusicNodeData music: PlayBGM(music.AudioFile); GoToNextNode(music.NextNodeId); break;
            case SpriteNodeData sprite: HandleSpriteNode(sprite); GoToNextNode(sprite.NextNodeId); break;
            case ChoiceNodeData choice: ShowChoiceButtons(choice); break;
            case BranchNodeData branch: HandleBranchNode(branch); break;
        }
    }

    private void UpdateDialogueAndCharacter(DialogueNodeData dialogue)
    {
        string actorName = GetCharacterName(dialogue.CharacterId);
        UpdateDialogueUI(actorName, dialogue.Content);
        if (_activeSprites.TryGetValue(dialogue.CharacterId, out var existingSprite))
            existingSprite.UpdateCharacter(dialogue.CharacterId, dialogue.Emotion);
    }

    private void HandleSpriteNode(SpriteNodeData data)
    {
        if (data.ActionType == "Hide")
        {
            if (_activeSprites.TryGetValue(data.CharacterId, out var s)) { s.QueueFree(); _activeSprites.Remove(data.CharacterId); }
            return;
        }
        if (!_activeSprites.TryGetValue(data.CharacterId, out var targetSprite))
        {
            targetSprite = new CharacterSprite();
            _characterContainer.AddChild(targetSprite);
            _activeSprites[data.CharacterId] = targetSprite;
        }
        targetSprite.UpdateCharacter(data.CharacterId, data.Expression, data.IsSilhouette);
        UpdateSpritePosition(targetSprite, data.Position);
    }

    private void UpdateSpritePosition(CharacterSprite sprite, string position)
    {
        Vector2 size = new Vector2(600, 800); 
        sprite.Size = size;
        float xPos = position switch {
            "Left" => Size.X * 0.25f - size.X / 2,
            "Right" => Size.X * 0.75f - size.X / 2,
            _ => (Size.X - size.X) / 2
        };
        sprite.Position = new Vector2(xPos, Size.Y - size.Y);
    }

    private void UpdateBackground(string file, string transition)
    {
        if (string.IsNullOrEmpty(file)) return;
        
        string rawPath = ProjectManager.IsProjectOpened ? Path.Combine(ProjectManager.BackgroundDir, file) : "res://backgrounds/" + file;
        string absolutePath = rawPath.StartsWith("res://") ? ProjectSettings.GlobalizePath(rawPath) : rawPath;
        
        GD.Print($"[Engine] Attempting to load background from: {absolutePath}");

        if (Godot.FileAccess.FileExists(absolutePath))
        {
            var image = Image.LoadFromFile(absolutePath);
            if (image != null)
            {
                var texture = ImageTexture.CreateFromImage(image);
                _backgroundRect.Texture = texture;
                _backgroundRect.Modulate = new Color(1, 1, 1, 1);
                GD.Print($"[Engine] Background Loaded Successfully: {file}");
            }
            else
            {
                GD.PrintErr($"[Engine] Failed to create Image from file: {absolutePath}");
            }
        }
        else GD.PrintErr($"[Engine] Background file NOT found at: {absolutePath}");
    }

    private void PlayBGM(string file)
    {
        if (string.IsNullOrEmpty(file)) { _bgmPlayer.Stop(); return; }
        string rawPath = ProjectManager.IsProjectOpened ? Path.Combine(ProjectManager.AudioDir, file) : "res://audio/" + file;
        string absolutePath = rawPath.StartsWith("res://") ? ProjectSettings.GlobalizePath(rawPath) : rawPath;
        
        if (Godot.FileAccess.FileExists(absolutePath))
        {
            // 音频加载在外部文件模式下比较特殊，目前主要支持 res:// 内部资源
            if (absolutePath.Contains(".godot/imported")) {
                var stream = GD.Load<AudioStream>(absolutePath);
                if (_bgmPlayer.Stream != stream) { _bgmPlayer.Stream = stream; _bgmPlayer.Play(); }
            }
        }
    }

    private void FinishStory(string type = "Title")
    {
        if (IsPreviewMode) { EmitSignal(SignalName.StoryFinished); return; }
        GetTree().ChangeSceneToFile("res://scenes/MainMenuScreen.tscn");
    }

    private string GetCharacterName(int id)
    {
        var charData = CharacterManager.Characters.Find(c => c.Id == id);
        return charData != null ? LocalTr(charData.Name) : "...";
    }

    private void UpdateDialogueUI(string name, string content)
    {
        _nameLabel.Text = LocalTr(name);
        string translatedContent = LocalTr(content);
        _contentLabel.Text = "";
        _isTextAnimating = true;
        if (_textTween != null) _textTween.Kill();
        _textTween = CreateTween();
        _textTween.TweenProperty(_contentLabel, "visible_ratio", 1.0f, translatedContent.Length * _textSpeed).From(0.0f);
        _contentLabel.Text = translatedContent;
        _textTween.Finished += () => _isTextAnimating = false;
    }

    private void ShowChoiceButtons(ChoiceNodeData choice)
    {
        _dialogueBox.Hide(); _choiceContainer.Show();
        foreach (var option in choice.Options)
        {
            Button btn = new Button { Text = LocalTr(option.Text), CustomMinimumSize = new Vector2(300, 50) };
            btn.Pressed += () => GoToNextNode(option.TargetNodeId);
            _choiceContainer.AddChild(btn);
        }
    }

    private void HandleBranchNode(BranchNodeData branch)
    {
        float currentVal = GlobalGameState.Instance.GetVariable(branch.VariableId);
        float threshold = float.TryParse(branch.ComparisonValue, out var v) ? v : 0;
        GoToNextNode(currentVal >= threshold ? branch.SuccessNodeId : branch.FailNodeId);
    }

    private void OnInteraction()
    {
        if (_isTextAnimating) { _textTween.Stop(); _contentLabel.VisibleRatio = 1.0f; _isTextAnimating = false; return; }
        if (_currentNode is DialogueNodeData || _currentNode is NarrativeNodeData) GoToNextNode(_currentNode.NextNodeId);
    }

    private void GoToNextNode(string nextId)
    {
        _currentNode = string.IsNullOrEmpty(nextId) ? null : _storyNodes.FirstOrDefault(n => n.Id == nextId);
        ProcessCurrentNode();
    }

    private void LoadLocalTranslations(string storyPath) { } 
    private string LocalTr(string key) => string.IsNullOrEmpty(key) ? key : Tr(key);
}
