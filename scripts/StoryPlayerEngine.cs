using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UmaEraArchive.Editor.Nodes;
using UmaEraArchive.Core;

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
    // 贴纸精灵：用 stickerId 的负数偏移作为 key，与角色 ID 区分
    private Dictionary<int, CharacterSprite> _activeStickerSprites = new Dictionary<int, CharacterSprite>();

    private List<BaseNodeData> _storyNodes = new List<BaseNodeData>();
    private BaseNodeData _currentNode;
    private bool _isTextAnimating = false;
    private float _textSpeed = 0.05f;
    private Tween _textTween;

    public static string CurrentStoryPath = "";
    public static List<BaseNodeData> PreviewNodes = null;
    public static string StartNodeId = null;
    public static bool EnableVisualEditing = false;
    public static string ReturnScenePath = "res://scenes/MainMenuScreen.tscn";
    public bool IsPreviewMode { get; set; } = false;

    private CharacterSprite _draggedSprite = null;
    private Vector2 _dragOffset = Vector2.Zero;

    [Signal] public delegate void StoryFinishedEventHandler();

    public override void _Ready()
    {
        _nameLabel = GetNode<Label>("UI_Layer/SafeAreaAdapter/Control_Root/DialogueBox/NameLabel");
        _contentLabel = GetNode<RichTextLabel>("UI_Layer/SafeAreaAdapter/Control_Root/DialogueBox/ContentLabel");
        _choiceContainer = GetNode<VBoxContainer>("UI_Layer/SafeAreaAdapter/Control_Root/ChoiceContainer");
        _dialogueBox = GetNode<Control>("UI_Layer/SafeAreaAdapter/Control_Root/DialogueBox");
        _interactButton = GetNode<Button>("UI_Layer/InteractButton");
        _backgroundRect = GetNode<TextureRect>("Background");
        _overlay = GetNode<Control>("ColorRectOverlay");
        _overlay.MouseFilter = MouseFilterEnum.Ignore; // 确保遮罩层不拦截点击

        // 通过 ResourceProxy 安全加载 blur shader（路径不存在时优雅降级）
        var blurShader = UmaEraArchive.Core.ResourceProxy.LoadBlurOverlayShader();
        var mat = new ShaderMaterial();
        if (blurShader != null)
        {
            mat.Shader = blurShader;
            mat.SetShaderParameter("color_over", new Color(0, 0, 0, 1));
            mat.SetShaderParameter("blur_amount", 0.0f);
            mat.SetShaderParameter("mix_amount", 0.0f);
        }
        _overlay.Material = mat;
        
        // 背景层初始化
        _backgroundRect.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        _backgroundRect.StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered;
        _backgroundRect.SetAnchorsPreset(LayoutPreset.FullRect);
        _backgroundRect.MouseFilter = MouseFilterEnum.Ignore; // 确保背景不拦截点击

        _characterContainer = new Control { Name = "CharacterContainer" };
        _characterContainer.SetAnchorsPreset(LayoutPreset.FullRect);
        _characterContainer.MouseFilter = Control.MouseFilterEnum.Ignore; // 立绘容器不拦截
        AddChild(_characterContainer);

        // 严格校正层级：背景(0) -> 遮罩(1) -> 立绘(2) -> UI
        MoveChild(_backgroundRect, 0);
        MoveChild(_overlay, 1);
        MoveChild(_characterContainer, 2);

        _choiceContainer.MouseFilter = MouseFilterEnum.Pass; // 允许子按钮接收事件
        
        _bgmPlayer = new AudioStreamPlayer();
        AddChild(_bgmPlayer);

        _interactButton.Pressed += OnInteraction;
        _interactButton.MouseFilter = MouseFilterEnum.Pass; // 交互按钮默认 Pass

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
            // 可视化编辑模式：禁用全屏交互按钮并隐藏对话框，避免遮挡立绘操作
            if (EnableVisualEditing)
            {
                _interactButton.MouseFilter = MouseFilterEnum.Ignore;
                _dialogueBox.Hide();
            }
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

    public override void _ExitTree()
    {
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
        _interactButton.Show(); // 默认显示全屏交互按钮
        foreach (Node child in _choiceContainer.GetChildren()) child.QueueFree();

        // 默认重置滤镜（如果当前节点没有指定，则恢复清晰）
        if (_currentNode is NarrativeNodeData narrative) ApplyVisualEffects(narrative.BlurValue, narrative.Darkness);
        else if (_currentNode is ChoiceNodeData choice) ApplyVisualEffects(choice.BlurValue, choice.Darkness);
        else ApplyVisualEffects(0, 0);

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
                PreExecuteVisualNodes(dialogue.NextNodeId);
                break;
            case NarrativeNodeData narrativeNode: 
                UpdateDialogueUI("", narrativeNode.Content); 
                // 叙述节点需等待点击才跳转，不做预执行
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
            case StickerNodeData sticker:
                HandleStickerNode(sticker);
                if (shouldPauseForEdit) return;
                GoToNextNode(sticker.NextNodeId);
                break;
            case ChoiceNodeData choiceNode: 
                ShowChoiceButtons(choiceNode); 
                break;
            case ValueNodeData valueNode:
                HandleValueNode(valueNode);
                GoToNextNode(valueNode.NextNodeId);
                break;
            case BranchNodeData branch: 
                if (shouldPauseForEdit) return;
                HandleBranchNode(branch); 
                break;
        }
    }

    private void ApplyVisualEffects(float blur, float darkness)
    {
        if (_overlay.Material is ShaderMaterial mat)
        {
            // 使用 Tween 实现平滑过渡
            var tween = CreateTween();
            tween.SetParallel(true);
            tween.TweenMethod(Callable.From((float v) => mat.SetShaderParameter("blur_amount", v)), (float)mat.GetShaderParameter("blur_amount"), blur, 0.5f);
            tween.TweenMethod(Callable.From((float v) => mat.SetShaderParameter("mix_amount", v)), (float)mat.GetShaderParameter("mix_amount"), darkness, 0.5f);
        }
    }

    private void HandleValueNode(ValueNodeData data)
    {
        var manager = umaEraArchive.Game.GameManager.Instance;
        var errorNotifier = GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier");
        string storyId = System.IO.Path.GetFileNameWithoutExtension(CurrentStoryPath);
        string valueId = data.TargetAttribute == "Custom" ? data.CustomId : data.TargetAttribute;

        if (manager == null || manager.CurrentState == null)
        {
            errorNotifier?.ShowToast($"{storyId} 访问了一个意外的数值 {valueId}!");
            return;
        }

        var state = manager.CurrentState;
        bool success = true;

        try
        {
            switch (data.TargetAttribute)
            {
                case "Money": state.Player.Money += data.ChangeValue; break;
                case "Stamina": state.Player.AddStamina(data.ChangeValue); break;
                case "Energy": state.Player.AddEnergy(data.ChangeValue); break;
                case "Speed": state.Uma.AddStat(umaEraArchive.Game.StatType.Speed, data.ChangeValue); break;
                case "Endurance": state.Uma.AddStat(umaEraArchive.Game.StatType.Stamina, data.ChangeValue); break;
                case "Power": state.Uma.AddStat(umaEraArchive.Game.StatType.Power, data.ChangeValue); break;
                case "Guts": state.Uma.AddStat(umaEraArchive.Game.StatType.Guts, data.ChangeValue); break;
                case "Intelligence": state.Uma.AddStat(umaEraArchive.Game.StatType.Intelligence, data.ChangeValue); break;
                case "SkillPoint": state.Uma.SkillPoints += data.ChangeValue; break;
                case "Custom":
                    if (string.IsNullOrWhiteSpace(data.CustomId))
                    {
                        errorNotifier?.ShowToast($"{storyId} 访问了一个意外的数值 {valueId}!");
                        success = false;
                    }
                    else
                    {
                        var globalState = UmaEraArchive.Core.GlobalGameState.Instance;
                        if (globalState != null)
                        {
                            float current = globalState.GetVariable(data.CustomId);
                            globalState.SetVariable(data.CustomId, current + data.ChangeValue);
                        }
                        else
                        {
                            errorNotifier?.ShowToast($"{storyId} 访问了一个意外的数值 {valueId}!");
                            success = false;
                        }
                    }
                    break;
                default:
                    errorNotifier?.ShowToast($"{storyId} 访问了一个意外的数值 {valueId}!");
                    success = false;
                    break;
            }
        }
        catch (Exception ex)
        {
            errorNotifier?.ShowToast($"{storyId} 访问了一个意外的数值 {valueId}!");
            success = false;
        }

        if (success)
        {
            GD.Print($"[Engine] Value Changed: {data.TargetAttribute} ({(data.TargetAttribute == "Custom" ? data.CustomId : "")}) by {data.ChangeValue}");
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

    // 处理贴纸节点：加载贴纸图片到场景
    private void HandleStickerNode(StickerNodeData data)
    {
        int stickerKey = -(data.StickerId + 1000); // 用负数 key 避免和角色 ID 冲突
        if (data.ActionType == "Hide")
        {
            if (_activeStickerSprites.TryGetValue(stickerKey, out var s)) { s.QueueFree(); _activeStickerSprites.Remove(stickerKey); }
            return;
        }

        var stickerData = StickerManager.Stickers.Find(s => s.Id == data.StickerId);
        if (stickerData == null || string.IsNullOrEmpty(stickerData.ImageFile)) return;

        if (!_activeStickerSprites.TryGetValue(stickerKey, out var targetSprite))
        {
            targetSprite = new CharacterSprite();
            _characterContainer.AddChild(targetSprite);
            _activeStickerSprites[stickerKey] = targetSprite;
        }

        // 复用 SpriteNodeData 格式传递变换参数
        var proxyData = new SpriteNodeData {
            OffsetX = data.OffsetX, OffsetY = data.OffsetY,
            Scale = data.Scale, FlipH = data.FlipH
        };
        targetSprite.SourceData = proxyData;

        // 设置贴纸尺寸和位置，通过 UpdateTextureDirect 加载纹理
        targetSprite.Size = new Vector2(400, 400);
        targetSprite.Position = new Vector2(
            (Size.X - 400) / 2 + data.OffsetX,
            (Size.Y - 400) / 2 + data.OffsetY
        );
        targetSprite.UpdateTextureDirect(stickerData.ImageFile);
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

        // 合并角色立绘和贴纸精灵，统一处理交互
        var allSprites = new List<CharacterSprite>(_activeSprites.Values);
        allSprites.AddRange(_activeStickerSprites.Values);

        if (@event is InputEventMouseButton mb)
        {
            if (mb.ButtonIndex == MouseButton.Left)
            {
                if (mb.Pressed) {
                    foreach (var sprite in allSprites) {
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
                foreach (var sprite in allSprites) {
                    if (sprite.GetGlobalRect().HasPoint(mb.GlobalPosition)) {
                        sprite.ToggleFlip();
                        GetViewport().SetInputAsHandled();
                        break;
                    }
                }
            }
            else if (mb.ButtonIndex == MouseButton.WheelUp && mb.Pressed) {
                foreach (var sprite in allSprites) {
                    if (sprite.GetGlobalRect().HasPoint(mb.GlobalPosition)) {
                        sprite.AdjustScale(0.05f);
                        GetViewport().SetInputAsHandled();
                        break;
                    }
                }
            }
            else if (mb.ButtonIndex == MouseButton.WheelDown && mb.Pressed) {
                foreach (var sprite in allSprites) {
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
        
        GD.Print($"[Engine] Attempting to load background: {file}");

        var bgTexture = UmaEraArchive.Core.ResourceProxy.LoadBackgroundTexture(file);
        if (bgTexture != null)
        {
            _backgroundRect.Texture = bgTexture;
            _backgroundRect.Modulate = new Color(1, 1, 1, 1);
            GD.Print($"[Engine] Background Loaded Successfully: {file}");
        }
        else
        {
            GetNode<ErrorNotifier>("/root/ErrorNotifier").ShowToast($"[Engine] Background file NOT found/failed to load: {file}");
        }
    }

    private void PlayBGM(string file)
    {
        if (string.IsNullOrEmpty(file)) { _bgmPlayer.Stop(); return; }
        
        AudioStream stream = UmaEraArchive.Core.ResourceProxy.LoadAudioFromProject(file);
        if (stream != null)
        {
            if (_bgmPlayer.Stream != stream) 
            { 
                _bgmPlayer.Stream = stream; 
                _bgmPlayer.Play(); 
            }
        }
    }

    private void FinishStory(string type = "Title")
    {
        if (IsPreviewMode) { EmitSignal(SignalName.StoryFinished); return; }
        GetTree().ChangeSceneToFile(ReturnScenePath);
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
        // 必须先赋值 Text，visible_ratio 动画才有效果
        _contentLabel.Text = translatedContent;
        _contentLabel.VisibleRatio = 0.0f;
        _isTextAnimating = true;
        if (_textTween != null) _textTween.Kill();
        _textTween = CreateTween();
        _textTween.TweenProperty(_contentLabel, "visible_ratio", 1.0f, translatedContent.Length * _textSpeed);
        _textTween.Finished += () => _isTextAnimating = false;
    }

    private void ShowChoiceButtons(ChoiceNodeData choice)
    {
        _dialogueBox.Hide(); 
        _choiceContainer.Show();
        _interactButton.Hide(); // 隐藏全屏交互按钮，否则它会挡住选项按钮点击
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

    private void PreExecuteVisualNodes(string nextId)
    {
        string currentCheckId = nextId;
        while (!string.IsNullOrEmpty(currentCheckId))
        {
            var node = _storyNodes.FirstOrDefault(n => n.Id == currentCheckId);
            if (node == null) break;

            if (node is SpriteNodeData sprite)
            {
                HandleSpriteNode(sprite);
                currentCheckId = sprite.NextNodeId;
            }
            else if (node is BackgroundNodeData bg)
            {
                UpdateBackground(bg.BackgroundFile, bg.TransitionType);
                currentCheckId = bg.NextNodeId;
            }
            else if (node is MusicNodeData music)
            {
                PlayBGM(music.AudioFile);
                currentCheckId = music.NextNodeId;
            }
            else if (node is StickerNodeData sticker)
            {
                HandleStickerNode(sticker);
                currentCheckId = sticker.NextNodeId;
            }
            else
            {
                break; // Stop when hitting a blocking node (Dialogue, Choice, Branch, etc.)
            }
        }
    }

    private void LoadLocalTranslations(string storyPath) { } 
    private string LocalTr(string key) => string.IsNullOrEmpty(key) ? key : Tr(key);
}
