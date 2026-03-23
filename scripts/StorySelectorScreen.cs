using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class StorySelectorScreen : Control
{
    public static bool IsForSimulation = false;
    
    private string _storiesDir = "user://stories/";
    private VBoxContainer _storyList;
    private LineEdit _searchEdit;
    private Button _btnPlay;
    
    // 经典单选模式
    private string _selectedStoryPath = "";
    // 养成模式复选
    private List<string> _selectedPaths = new List<string>();
    
    // 分组存储：记录分组名称与旗下的脚本路径
    private Dictionary<string, List<string>> _groupedStories = new Dictionary<string, List<string>>();

    public override void _Ready()
    {
        _storyList = GetNode<VBoxContainer>("SafeAreaAdapter/VBoxContainer/ScrollContainer/StoryList");
        _searchEdit = GetNode<LineEdit>("SafeAreaAdapter/VBoxContainer/Header/SearchEdit");
        _btnPlay = GetNode<Button>("SafeAreaAdapter/VBoxContainer/Footer/BtnPlay");
        
        _searchEdit.TextChanged += OnSearchTextChanged;
        GetNode<Button>("SafeAreaAdapter/VBoxContainer/Footer/BtnBack").Pressed += () => {
            IsForSimulation = false; 
            LoadingScreen.TargetScene = "res://scenes/MainMenuScreen.tscn";
            GetTree().ChangeSceneToFile("res://scenes/LoadingScreen.tscn");
        };
        _btnPlay.Pressed += LaunchStory;

        if (!DirAccess.DirExistsAbsolute(_storiesDir))
            DirAccess.MakeDirAbsolute(_storiesDir);

        RefreshStoryList();
    }

    private void RefreshStoryList()
    {
        _groupedStories.Clear();
        ScanDirectory(_storiesDir, "Root");
        RenderList(_groupedStories);
    }

    private void ScanDirectory(string currentPath, string groupName)
    {
        using var dir = DirAccess.Open(currentPath);
        if (dir != null)
        {
            if (!_groupedStories.ContainsKey(groupName))
                _groupedStories[groupName] = new List<string>();

            dir.ListDirBegin();
            string fileName = dir.GetNext();
            while (fileName != "")
            {
                if (dir.CurrentIsDir() && fileName != "." && fileName != "..")
                {
                    // 递归扫描子目录，将目录名作为新的 GroupName
                    ScanDirectory(currentPath + fileName + "/", fileName);
                }
                else if (!dir.CurrentIsDir() && (fileName.EndsWith(".json") || fileName.EndsWith(".era") || fileName.EndsWith(".zip")))
                {
                    _groupedStories[groupName].Add(currentPath + fileName);
                }
                fileName = dir.GetNext();
            }
        }
    }

    private void RenderList(Dictionary<string, List<string>> renderData)
    {
        foreach (Node child in _storyList.GetChildren()) child.QueueFree();

        ButtonGroup bg = IsForSimulation ? null : new ButtonGroup();
        int totalValidStories = 0;

        foreach (var kvp in renderData)
        {
            if (kvp.Value.Count == 0) continue;
            totalValidStories += kvp.Value.Count;

            string groupName = kvp.Key == "Root" ? "根目录 (Root)" : kvp.Key;
            
            var groupVBox = new VBoxContainer();
            // 边距缩进子节点
            var margin = new MarginContainer();
            margin.AddThemeConstantOverride("margin_left", 30);
            margin.AddChild(groupVBox);

            if (IsForSimulation)
            {
                var groupHeader = new CheckBox {
                    Text = $"📁 剧组: {groupName} (全选)",
                    CustomMinimumSize = new Vector2(0, 40)
                };
                _storyList.AddChild(groupHeader);
                _storyList.AddChild(margin);

                List<CheckBox> itemChecks = new List<CheckBox>();

                foreach (var fullPath in kvp.Value)
                {
                    string fileName = fullPath.Substring(fullPath.LastIndexOf('/') + 1);
                    string displayName = fileName.EndsWith(".era") || fileName.EndsWith(".zip") ? "[包] " + fileName : fileName;

                    CheckBox cb = new CheckBox {
                        Text = displayName,
                        CustomMinimumSize = new Vector2(0, 40)
                    };
                    cb.Toggled += (bool pressed) => {
                        if (pressed) {
                            if (!_selectedPaths.Contains(fullPath)) _selectedPaths.Add(fullPath);
                        } else {
                            _selectedPaths.Remove(fullPath);
                        }
                        _btnPlay.Disabled = _selectedPaths.Count == 0;
                    };
                    itemChecks.Add(cb);
                    groupVBox.AddChild(cb);
                }

                // 全选联动
                groupHeader.Toggled += (bool pressed) => {
                    foreach (var cb in itemChecks) {
                        cb.ButtonPressed = pressed;
                    }
                };
            }
            else
            {
                // 单选模式
                var lbl = new Label { Text = $"📁 剧组: {groupName}", CustomMinimumSize = new Vector2(0, 30) };
                lbl.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.7f));
                _storyList.AddChild(lbl);
                _storyList.AddChild(margin);

                foreach (var fullPath in kvp.Value)
                {
                    string fileName = fullPath.Substring(fullPath.LastIndexOf('/') + 1);
                    string displayName = fileName.EndsWith(".era") || fileName.EndsWith(".zip") ? "[包] " + fileName : fileName;

                    Button btn = new Button {
                        Text = displayName,
                        Alignment = HorizontalAlignment.Left,
                        CustomMinimumSize = new Vector2(0, 60),
                        ToggleMode = true,
                        ButtonGroup = bg
                    };
                    btn.Pressed += () => {
                        _selectedStoryPath = fullPath;
                        _btnPlay.Disabled = false;
                        GD.Print($"Selected Story: {_selectedStoryPath}");
                    };
                    groupVBox.AddChild(btn);
                }
            }
        }

        if (totalValidStories == 0)
        {
            _storyList.AddChild(new Label { Text = "No stories found. Create one in the Editor!", HorizontalAlignment = HorizontalAlignment.Center });
        }
    }

    private void OnSearchTextChanged(string newText)
    {
        if (string.IsNullOrWhiteSpace(newText))
        {
            RenderList(_groupedStories);
        }
        else
        {
            var filtered = new Dictionary<string, List<string>>();
            foreach(var kvp in _groupedStories)
            {
                var matches = kvp.Value.Where(s => s.ToLower().Contains(newText.ToLower())).ToList();
                if (matches.Count > 0)
                    filtered[kvp.Key] = matches;
            }
            RenderList(filtered);
        }
    }

    private void LaunchStory()
    {
        if (IsForSimulation)
        {
            if (_selectedPaths.Count == 0) return;
            
            if (umaEraArchive.Game.GameManager.Instance != null)
            {
                umaEraArchive.Game.GameManager.Instance.StartNewGame(_selectedPaths);
                umaEraArchive.Game.GameManager.Instance.AutoSave();
            }
            IsForSimulation = false;
            LoadingScreen.TargetScene = "res://scenes/SimulationMainScreen.tscn";
            GetTree().ChangeSceneToFile("res://scenes/LoadingScreen.tscn");
            return;
        }

        if (string.IsNullOrEmpty(_selectedStoryPath)) return;

        if (_selectedStoryPath.EndsWith(".era") || _selectedStoryPath.EndsWith(".zip"))
        {
            GD.Print($"[Selector] Loading Package: {_selectedStoryPath}");
            bool success = ProjectSettings.LoadResourcePack(_selectedStoryPath);
            if (success)
            {
                StoryPlayerEngine.CurrentStoryPath = "res://story.json";
                CharacterManager.LoadCharacters("res://characters.json");
            }
            else
            {
                GetNode<ErrorNotifier>("/root/ErrorNotifier").ShowErrorDialog("加载失败", "[Selector] Failed to load package!");
                return;
            }
        }
        else
        {
            StoryPlayerEngine.CurrentStoryPath = _selectedStoryPath;
            string baseDir = _selectedStoryPath.Substring(0, _selectedStoryPath.LastIndexOf('/'));
            string charFile = baseDir + "/characters.json";
            if (FileAccess.FileExists(charFile)) CharacterManager.LoadCharacters(charFile);
        }

        LoadingScreen.TargetScene = "res://scenes/StoryPlayerScreen.tscn";
        GetTree().ChangeSceneToFile("res://scenes/LoadingScreen.tscn");
    }
}

