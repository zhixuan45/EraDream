using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class StorySelectorScreen : Control
{
    private string _storiesDir = "user://stories/";
    private VBoxContainer _storyList;
    private LineEdit _searchEdit;
    private Button _btnPlay;
    
    private string _selectedStoryPath = "";
    private List<string> _allStories = new List<string>();

    public override void _Ready()
    {
        _storyList = GetNode<VBoxContainer>("MarginContainer/VBoxContainer/ScrollContainer/StoryList");
        _searchEdit = GetNode<LineEdit>("MarginContainer/VBoxContainer/Header/SearchEdit");
        _btnPlay = GetNode<Button>("MarginContainer/VBoxContainer/Footer/BtnPlay");
        
        // 绑定事件
        _searchEdit.TextChanged += OnSearchTextChanged;
        GetNode<Button>("MarginContainer/VBoxContainer/Footer/BtnBack").Pressed += () => {
            LoadingScreen.TargetScene = "res://scenes/MainMenuScreen.tscn";
            GetTree().ChangeSceneToFile("res://scenes/LoadingScreen.tscn");
        };
        _btnPlay.Pressed += LaunchStory;

        // 确保目录存在
        if (!DirAccess.DirExistsAbsolute(_storiesDir))
            DirAccess.MakeDirAbsolute(_storiesDir);

        RefreshStoryList();
    }

    private void RefreshStoryList()
    {
        _allStories.Clear();
        using var dir = DirAccess.Open(_storiesDir);
        if (dir != null)
        {
            dir.ListDirBegin();
            string fileName = dir.GetNext();
            while (fileName != "")
            {
                if (!dir.CurrentIsDir() && (fileName.EndsWith(".json") || fileName.EndsWith(".era")))
                {
                    _allStories.Add(fileName);
                }
                fileName = dir.GetNext();
            }
        }

        RenderList(_allStories);
    }

    private void RenderList(List<string> stories)
    {
        // 清空列表
        foreach (Node child in _storyList.GetChildren()) child.QueueFree();

        foreach (var story in stories)
        {
            string displayName = story.EndsWith(".era") ? "[包] " + story.Replace(".era", "") : story.Replace(".json", "");
            Button btn = new Button
            {
                Text = displayName,
                Alignment = HorizontalAlignment.Left,
                CustomMinimumSize = new Vector2(0, 60),
                ToggleMode = true,
                ButtonGroup = new ButtonGroup() // 互斥选择
            };
            
            btn.Pressed += () => {
                _selectedStoryPath = _storiesDir + story;
                _btnPlay.Disabled = false;
                GD.Print($"Selected Story: {_selectedStoryPath}");
            };

            _storyList.AddChild(btn);
        }

        if (stories.Count == 0)
        {
            _storyList.AddChild(new Label { Text = "No stories found. Create one in the Editor!", HorizontalAlignment = HorizontalAlignment.Center });
        }
    }

    private void OnSearchTextChanged(string newText)
    {
        if (string.IsNullOrWhiteSpace(newText))
        {
            RenderList(_allStories);
        }
        else
        {
            var filtered = _allStories.Where(s => s.ToLower().Contains(newText.ToLower())).ToList();
            RenderList(filtered);
        }
    }

    private void LaunchStory()
    {
        if (string.IsNullOrEmpty(_selectedStoryPath)) return;
        
        if (_selectedStoryPath.EndsWith(".era"))
        {
            GD.Print($"[Selector] Loading ERA Package: {_selectedStoryPath}");
            // 挂载资源包
            bool success = ProjectSettings.LoadResourcePack(_selectedStoryPath);
            if (success)
            {
                // 挂载后，包内资源在 res:// 根目录
                StoryPlayerEngine.CurrentStoryPath = "res://story.json";
                CharacterManager.LoadCharacters("res://characters.json");
            }
            else
            {
                GetNode<ErrorNotifier>("/root/ErrorNotifier").ShowErrorDialog("加载失败", "[Selector] Failed to load ERA package!");
                return;
            }
        }
        else
        {
            StoryPlayerEngine.CurrentStoryPath = _selectedStoryPath;
            // 尝试加载同级目录下的角色文件（如果是文件夹模式）
            string charFile = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(_selectedStoryPath), "characters.json");
            if (System.IO.File.Exists(charFile)) CharacterManager.LoadCharacters(charFile);
        }

        LoadingScreen.TargetScene = "res://scenes/StoryPlayerScreen.tscn";
        GetTree().ChangeSceneToFile("res://scenes/LoadingScreen.tscn");
    }
}
