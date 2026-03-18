using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using UmaArchive.Editor.Nodes;
using UmaArchive.Core;

public partial class StoryPlayerEngine : Control
{
    // UI 组件
    private Label _nameLabel;
    private RichTextLabel _contentLabel;
    private VBoxContainer _choiceContainer;
    private Control _dialogueBox;
    private Button _interactButton;

    // 运行状态
    private List<BaseNodeData> _storyNodes = new List<BaseNodeData>();
    private BaseNodeData _currentNode;
    private bool _isTextAnimating = false;
    private float _textSpeed = 0.05f;
    private Tween _textTween;

    // 静态路径，用于从 Selector 传入
    public static string CurrentStoryPath = "";

    public override void _Ready()
    {
        // 获取节点引用
        _nameLabel = GetNode<Label>("UI_Layer/DialogueBox/NameLabel");
        _contentLabel = GetNode<RichTextLabel>("UI_Layer/DialogueBox/ContentLabel");
        _choiceContainer = GetNode<VBoxContainer>("UI_Layer/ChoiceContainer");
        _dialogueBox = GetNode<Control>("UI_Layer/DialogueBox");
        _interactButton = GetNode<Button>("UI_Layer/InteractButton");

        _interactButton.Pressed += OnInteraction;

        if (string.IsNullOrEmpty(CurrentStoryPath))
        {
            GD.PrintErr("Story path is empty! Returning to Menu.");
            GetTree().ChangeSceneToFile("res://scenes/MainMenuScreen.tscn");
            return;
        }

        LoadStory(CurrentStoryPath);
    }

    private void LoadStory(string path)
    {
        _storyNodes = StoryNodeManager.LoadProject(path);
        if (_storyNodes.Count == 0) return;

        // 默认从第一个节点开始（或寻找 ID 为 Start 的节点）
        _currentNode = _storyNodes[0];
        ProcessCurrentNode();
    }

    private void ProcessCurrentNode()
    {
        if (_currentNode == null)
        {
            GD.Print("End of Story.");
            GetTree().ChangeSceneToFile("res://scenes/MainMenuScreen.tscn");
            return;
        }

        // 重置 UI 状态
        _choiceContainer.Hide();
        _dialogueBox.Show();
        foreach (Node child in _choiceContainer.GetChildren()) child.QueueFree();

        // 核心多态分发
        switch (_currentNode)
        {
            case DialogueNodeData dialogue:
                string actorName = GetCharacterName(dialogue.CharacterId);
                UpdateDialogueUI(actorName, dialogue.Content);
                break;
            case NarrativeNodeData narrative:
                UpdateDialogueUI("", narrative.Content);
                break;
            case MusicNodeData music:
                // TODO: 播放音频逻辑
                GoToNextNode(music.NextNodeId);
                break;
            case ChoiceNodeData choice:
                ShowChoiceButtons(choice);
                break;
            case BranchNodeData branch:
                HandleBranchNode(branch);
                break;
        }
    }

    private string GetCharacterName(int id)
    {
        switch (id)
        {
            case 0: return Tr("KEY_CHAR_PROTAGONIST");
            case 1: return Tr("KEY_CHAR_SIDEKICK");
            case 2: return Tr("KEY_CHAR_NARRATOR");
            default: return "...";
        }
    }

    private void UpdateDialogueUI(string name, string content)
    {
        _nameLabel.Text = string.IsNullOrEmpty(name) ? "..." : name;
        _contentLabel.Text = "";
        
        // 打字机效果
        _isTextAnimating = true;
        if (_textTween != null) _textTween.Kill();
        _textTween = CreateTween();
        
        // 逐字显示
        _textTween.TweenProperty(_contentLabel, "visible_ratio", 1.0f, content.Length * _textSpeed)
                  .From(0.0f)
                  .SetTrans(Tween.TransitionType.Linear);
        
        _contentLabel.Text = content;
        _textTween.Finished += () => _isTextAnimating = false;
    }

    private void ShowChoiceButtons(ChoiceNodeData choice)
    {
        _dialogueBox.Hide(); // 选项出现时隐藏对话框（可选）
        _choiceContainer.Show();

        foreach (var option in choice.Options)
        {
            Button btn = new Button { Text = option.Text, CustomMinimumSize = new Vector2(300, 50) };
            btn.Pressed += () => GoToNextNode(option.TargetNodeId);
            _choiceContainer.AddChild(btn);
        }
    }

    private void HandleBranchNode(BranchNodeData branch)
    {
        float currentVal = GlobalGameState.Instance.GetVariable(branch.VariableId);
        float threshold = float.TryParse(branch.ComparisonValue, out var v) ? v : 0;

        if (currentVal >= threshold)
            GoToNextNode(branch.SuccessNodeId);
        else
            GoToNextNode(branch.FailNodeId);
    }

    private void OnInteraction()
    {
        // 如果正在打字，点击则瞬间显示全部
        if (_isTextAnimating)
        {
            _textTween.Stop();
            _contentLabel.VisibleRatio = 1.0f;
            _isTextAnimating = false;
            return;
        }

        // 否则跳转到下一节点
        if (_currentNode is DialogueNodeData || _currentNode is NarrativeNodeData)
        {
            GoToNextNode(_currentNode.NextNodeId);
        }
    }

    private void GoToNextNode(string nextId)
    {
        if (string.IsNullOrEmpty(nextId))
        {
            _currentNode = null;
        }
        else
        {
            _currentNode = _storyNodes.FirstOrDefault(n => n.Id == nextId);
        }
        ProcessCurrentNode();
    }
}
