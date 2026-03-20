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
    public static string StartNodeId = null;
    public static bool EnableVisualEditing = false;
    public bool IsPreviewMode { get; set; } = false;

    private CharacterSprite _draggedSprite = null;
    private Vector2 _dragOffset = Vector2.Zero;

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
            
            if (!string.IsNullOrEmpty(StartNodeId)) {
                _currentNode = _storyNodes.FirstOrDefault(n => n.Id == StartNodeId);
            } else {
                _currentNode = _storyNodes.FirstOrDefault(n => n is StartNodeData) ?? _storyNodes[0];
            }
            
            ProcessCurrentNode();
            PreviewNodes = null; 
            return;
        }

        if (string.IsNullOrEmpty(CurrentStoryPath))
        {
            GetNode<ErrorNotifier>("/root/ErrorNotifier").ShowErrorDialog("加载失败", "[Engine] Story path is empty!");
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

        // 可视化编辑模式拦截：如果当前节点是目标编辑节点，则执行完逻辑后停止，不自动跳转
        bool shouldPauseForEdit = EnableVisualEditing && _currentNode.Id == StartNodeId;

        switch (_currentNode)
        {
            case StartNodeData start: 
                if (shouldPauseForEdit) return;
                GoToNextNode(start.NextNodeId); 
                break;
            case EndNodeData end: 
                FinishStory(end.EndType); 
                break;
            case BackgroundNodeData bg: 
                UpdateBackground(bg.BackgroundFile, bg.TransitionType); 
                if (shouldPauseForEdit) return;
                GoToNextNode(bg.NextNodeId); 
                break;
            case DialogueNodeData dialogue: 
                UpdateDialogueAndCharacter(dialogue); 
                break;
            case NarrativeNodeData narrative: 
                UpdateDialogueUI("", narrative.Content); 
                break;
            case MusicNodeData music: 
                PlayBGM(music.AudioFile); 
                if (shouldPauseForEdit) return;
                GoToNextNode(music.NextNodeId); 
                break;
            case SpriteNodeData sprite: 
                HandleSpriteNode(sprite); 
                if (shouldPauseForEdit) return;
                GoToNextNode(sprite.NextNodeId); 
                break;
            case ChoiceNodeData choice: 
                ShowChoiceButtons(choice); 
                break;
            case BranchNodeData branch: 
                if (shouldPauseForEdit) return;
                HandleBranchNode(branch); 
                break;
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
        targetSprite.SourceData = data; // 传递数据引用以供回写
        targetSprite.UpdateCharacter(data.CharacterId, data.Expression, data.IsSilhouette);
        UpdateSpritePosition(targetSprite, data.Position);
    }

    public override void _Input(InputEvent @event)
    {
        // 全局退出预览快捷键
        if (IsPreviewMode && @event is InputEventKey ek && ek.Pressed && ek.Keycode == Key.Escape)
        {
            GetViewport().SetInputAsHandled();
            FinishStory();
            return;
        }

        if (!IsPreviewMode || !EnableVisualEditing) return;

        if (@event is InputEventMouseButton mb)
        {
            if (mb.ButtonIndex == MouseButton.Left)
            {
                if (mb.Pressed) {
                    foreach (var sprite in _activeSprites.Values) {
                        if (sprite.GetGlobalRect().HasPoint(mb.GlobalPosition)) {
                            _draggedSprite = sprite;
                            _dragOffset = sprite.GlobalPosition - mb.GlobalPosition;
                            GetViewport().SetInputAsHandled();
                            break;
                        }
                    }
                } else {
                    _draggedSprite = null;
                }
            }
            else if (mb.ButtonIndex == MouseButton.Right && mb.Pressed) {
                foreach (var sprite in _activeSprites.Values) {
                    if (sprite.GetGlobalRect().HasPoint(mb.GlobalPosition)) {
                        sprite.ToggleFlip();
                        GetViewport().SetInputAsHandled();
                        break;
                    }
                }
            }
            else if (mb.ButtonIndex == MouseButton.WheelUp && mb.Pressed) {
                foreach (var sprite in _activeSprites.Values) {
                    if (sprite.GetGlobalRect().HasPoint(mb.GlobalPosition)) {
                        sprite.AdjustScale(0.05f);
                        GetViewport().SetInputAsHandled();
                        break;
                    }
                }
            }
            else if (mb.ButtonIndex == MouseButton.WheelDown && mb.Pressed) {
                foreach (var sprite in _activeSprites.Values) {
                    if (sprite.GetGlobalRect().HasPoint(mb.GlobalPosition)) {
                        sprite.AdjustScale(-0.05f);
                        GetViewport().SetInputAsHandled();
                        break;
                    }
                }
            }
        }
        else if (@event is InputEventMouseMotion mm && _draggedSprite != null)
        {
            _draggedSprite.ApplyDrag(mm.GlobalPosition + _dragOffset);
            GetViewport().SetInputAsHandled();
        }
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
        
        float offsetX = sprite.SourceData != null ? sprite.SourceData.OffsetX : 0;
        float offsetY = sprite.SourceData != null ? sprite.SourceData.OffsetY : 0;
        
        sprite.Position = new Vector2(xPos + offsetX, Size.Y - size.Y + offsetY);
    }

    private void UpdateBackground(string file, string transition)
    {
        if (string.IsNullOrEmpty(file)) return;
        
        string rawPath = ProjectManager.IsProjectOpened ? Path.Combine(ProjectManager.BackgroundDir, file) : "res://backgrounds/" + file;
        
        GD.Print($"[Engine] Attempting to load background from: {rawPath}");

        if (Godot.FileAccess.FileExists(rawPath))
        {
            // 对于 res:// 路径（资源包内），我们需要通过 FileAccess 读取原始数据以绕过物理路径限制
            using var fileAccess = Godot.FileAccess.Open(rawPath, Godot.FileAccess.ModeFlags.Read);
            byte[] data = fileAccess.GetBuffer((long)fileAccess.GetLength());
            var image = new Image();
            var error = image.LoadPngFromBuffer(data); // 假设是 PNG
            if (error != Error.Ok) error = image.LoadJpgFromBuffer(data); // 尝试 JPG
            if (error != Error.Ok) error = image.LoadWebpFromBuffer(data); // 尝试 WebP

            if (error == Error.Ok)
            {
                var texture = ImageTexture.CreateFromImage(image);
                _backgroundRect.Texture = texture;
                _backgroundRect.Modulate = new Color(1, 1, 1, 1);
                GD.Print($"[Engine] Background Loaded Successfully: {file}");
            }
            else
            {
                GetNode<ErrorNotifier>("/root/ErrorNotifier").ShowToast($"[Engine] Failed to load Image buffer from: {rawPath}, Error: {error}");
            }
        }
        else GetNode<ErrorNotifier>("/root/ErrorNotifier").ShowToast($"[Engine] Background file NOT found at: {rawPath}");
    }

    private void PlayBGM(string file)
    {
        if (string.IsNullOrEmpty(file)) { _bgmPlayer.Stop(); return; }
        string rawPath = ProjectManager.IsProjectOpened ? Path.Combine(ProjectManager.AudioDir, file) : "res://audio/" + file;
        
        if (Godot.FileAccess.FileExists(rawPath))
        {
            // 如果是项目编辑模式且是 Godot 导入的资源
            if (rawPath.Contains(".godot/imported")) {
                var stream = GD.Load<AudioStream>(rawPath);
                if (_bgmPlayer.Stream != stream) { _bgmPlayer.Stream = stream; _bgmPlayer.Play(); }
                return;
            }

            // 加载原始音频文件
            using var fileAccess = Godot.FileAccess.Open(rawPath, Godot.FileAccess.ModeFlags.Read);
            byte[] data = fileAccess.GetBuffer((long)fileAccess.GetLength());
            AudioStream newStream = null;
            
            if (file.ToLower().EndsWith(".mp3")) {
                var mp3 = new AudioStreamMP3(); mp3.Data = data; newStream = mp3;
            } else if (file.ToLower().EndsWith(".wav")) {
                var wav = new AudioStreamWav(); wav.Data = data; newStream = wav;
            } else if (file.ToLower().EndsWith(".ogg")) {
                var ogg = AudioStreamOggVorbis.LoadFromBuffer(data); newStream = ogg;
            }

            if (newStream != null) {
                if (_bgmPlayer.Stream != newStream) { _bgmPlayer.Stream = newStream; _bgmPlayer.Play(); }
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
