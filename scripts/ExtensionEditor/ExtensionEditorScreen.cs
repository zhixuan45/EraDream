using Godot;
using System;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using EraDream.Editor.Models;
using EraDream.Core;
using EraDream.Core.Models;
using EraDream.Core.Extensions;
using ExtensionManifest = EraDream.Editor.Models.ExtensionManifest;

public partial class ExtensionEditorScreen : Control
{
    private string _projectPath = "";
    private string _cachedNormalizedProjectBase = "";
    private string _cachedNormalizedProjectWithSlash = "";

    private void SetProjectPath(string path)
    {
        _projectPath = path;
        if (!string.IsNullOrEmpty(_projectPath)) {
            string absoluteProjectPath = System.IO.Path.GetFullPath(ProjectSettings.GlobalizePath(_projectPath));
            _cachedNormalizedProjectBase = absoluteProjectPath.Replace('\\', '/').TrimEnd('/');
            _cachedNormalizedProjectWithSlash = _cachedNormalizedProjectBase + "/";
        } else {
            _cachedNormalizedProjectBase = "";
            _cachedNormalizedProjectWithSlash = "";
        }
    }

    private EraDream.Editor.Models.ExtensionManifest _manifest = new();
    private ActorConfigData _currentActorConfig = null;
    private SimulationData _currentSimData = null;
    private BehaviorPack _currentBehaviorPack = null;

    // UI Nodes - SidePanel
    private Button _btnNew, _btnOpen, _btnExport, _btnBack;
    private MenuButton _btnCreateJSON;
    private Tree _fileTree;

    // UI Nodes - MainArea Containers
    private Control _manifestVBox, _fileEditorVBox, _imagePreviewVBox, _charEditorRoot, _behaviorEditorRoot;
    private TabContainer _charTabs;
    
    // Manifest Fields
    private LineEdit _idEdit, _nameEdit, _authorEdit, _versionEdit, _minVerEdit;
    private TextEdit _descEdit;
    private OptionButton _typeOption;

    // Behavior Editor Fields
    private VBoxContainer _behaviorRulesContainer;
    private VBoxContainer _behaviorItemsContainer;
    private VBoxContainer _behaviorMenusContainer;
    private Button _btnSaveBehavior;
    private Button _btnAddBehaviorRule;

    // Code Fields
    private Label _fileNameLabel;
    private TextEdit _fileContentEdit;
    private Button _btnSaveFile;

    // Image Fields
    private TextureRect _previewTexture;
    private Label _fileInfoLabel;

    // Character Editor Fields (Visuals)
    private LineEdit _charIdEdit, _charNameEdit, _charSpriteEdit;
    // Character Editor Fields (Simulation)
    private LineEdit _simInternalIdEdit, _simFullNameEdit, _simShortNameEdit, _simPersonalityEdit;
    private Button _btnSaveCombined;

    // Dialogs
    private PopupMenu _fileContextMenu;
    private ConfirmationDialog _deleteDialog;
    private AcceptDialog _renameDialog;
    private LineEdit _renameEdit;

    private string _currentEditingFilePath = "";
    private TreeItem _contextTargetItem = null;

    public override void _Ready()
    {
        var rootPath = "SafeArea/HSplit/MainPanel/MainArea/Margin/ContentRoot/";

        // SidePanel
        _btnNew = GetNode<Button>("SafeArea/HSplit/SidePanel/VBox/ProjBtns/BtnNewProject");
        _btnOpen = GetNode<Button>("SafeArea/HSplit/SidePanel/VBox/ProjBtns/BtnOpenProject");
        _btnCreateJSON = GetNode<MenuButton>("SafeArea/HSplit/SidePanel/VBox/ProjBtns/BtnCreateJSON");
        
        // 动态添加并列的“创建文件夹”按钮，使按钮均分空间
        var projBtns = _btnCreateJSON.GetParent();
        int btnIdx = _btnCreateJSON.GetIndex();
        projBtns.RemoveChild(_btnCreateJSON);
        
        HBoxContainer createHBox = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        projBtns.AddChild(createHBox);
        projBtns.MoveChild(createHBox, btnIdx);
        
        _btnCreateJSON.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        createHBox.AddChild(_btnCreateJSON);
        
        Button btnCreateFolder = new Button { 
            Text = "创建文件夹", 
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 35)
        };
        btnCreateFolder.Pressed += OnCreateFolderPressed;
        createHBox.AddChild(btnCreateFolder);
        _btnExport = GetNode<Button>("SafeArea/HSplit/SidePanel/VBox/FooterBtns/BtnExport");
        _btnBack = GetNode<Button>("SafeArea/HSplit/SidePanel/VBox/FooterBtns/BtnBack");
        _fileTree = GetNode<Tree>("SafeArea/HSplit/SidePanel/VBox/FileTree");
        _fileTree.AllowRmbSelect = true;

        // MainArea Views
        _manifestVBox = GetNode<Control>(rootPath + "ManifestVBox");
        _fileEditorVBox = GetNode<Control>(rootPath + "FileEditorVBox");
        _imagePreviewVBox = GetNode<Control>(rootPath + "ImagePreviewVBox");
        _charEditorRoot = GetNode<Control>(rootPath + "CharacterEditorRoot");
        _charTabs = GetNode<TabContainer>(rootPath + "CharacterEditorRoot/TabContainer");
        _behaviorEditorRoot = GetNode<Control>(rootPath + "BehaviorEditorRoot");

        // Manifest
        _idEdit = GetNode<LineEdit>(rootPath + "ManifestVBox/Grid/IdEdit");
        _nameEdit = GetNode<LineEdit>(rootPath + "ManifestVBox/Grid/NameEdit");
        _authorEdit = GetNode<LineEdit>(rootPath + "ManifestVBox/Grid/AuthorEdit");
        _versionEdit = GetNode<LineEdit>(rootPath + "ManifestVBox/Grid/VersionEdit");
        _minVerEdit = GetNode<LineEdit>(rootPath + "ManifestVBox/Grid/MinVerEdit");
        _descEdit = GetNode<TextEdit>(rootPath + "ManifestVBox/Grid/DescEdit");
        _typeOption = GetNode<OptionButton>(rootPath + "ManifestVBox/Grid/TypeOption");

        // Code
        _fileNameLabel = GetNode<Label>(rootPath + "FileEditorVBox/FileHeader/FileNameLabel");
        _fileContentEdit = GetNode<TextEdit>(rootPath + "FileEditorVBox/FileContentEdit");
        _btnSaveFile = GetNode<Button>(rootPath + "FileEditorVBox/FileHeader/BtnSaveFile");

        // Image
        _previewTexture = GetNode<TextureRect>(rootPath + "ImagePreviewVBox/TextureRect");
        _fileInfoLabel = GetNode<Label>(rootPath + "ImagePreviewVBox/FileInfoLabel");

        // Character Visuals (Match with .tscn node names!)
        _charIdEdit = GetNode<LineEdit>(rootPath + "CharacterEditorRoot/TabContainer/Visuals/Grid/CharIdEdit");
        _charNameEdit = GetNode<LineEdit>(rootPath + "CharacterEditorRoot/TabContainer/Visuals/Grid/CharNameEdit");
        _charSpriteEdit = GetNode<LineEdit>(rootPath + "CharacterEditorRoot/TabContainer/Visuals/Grid/CharSpriteEdit");
        
        // Character Simulation (Match with .tscn node names!)
        _simInternalIdEdit = GetNode<LineEdit>(rootPath + "CharacterEditorRoot/TabContainer/Simulation/Grid/SimInternalIdEdit");
        _simFullNameEdit = GetNode<LineEdit>(rootPath + "CharacterEditorRoot/TabContainer/Simulation/Grid/SimFullNameEdit");
        _simShortNameEdit = GetNode<LineEdit>(rootPath + "CharacterEditorRoot/TabContainer/Simulation/Grid/SimShortNameEdit");
        _simPersonalityEdit = GetNode<LineEdit>(rootPath + "CharacterEditorRoot/TabContainer/Simulation/Grid/SimPersonalityEdit");
        
        _btnSaveCombined = GetNode<Button>(rootPath + "CharacterEditorRoot/Header/BtnSaveCombined");

        // Behavior Editor
        _behaviorEditorRoot = GetNode<Control>(rootPath + "BehaviorEditorRoot");
        _btnSaveBehavior = GetNode<Button>(rootPath + "BehaviorEditorRoot/Header/BtnSaveBehavior");
        _btnAddBehaviorRule = GetNode<Button>(rootPath + "BehaviorEditorRoot/Header/BtnAddRule");
        
        // 动态隐藏原始 Header 上的添加按钮，改为各 Tab 内置
        _btnAddBehaviorRule.Hide();

        var scroll = _behaviorEditorRoot.GetNode<ScrollContainer>("ScrollContainer");
        _behaviorRulesContainer = scroll.GetNode<VBoxContainer>("RulesVBox");
        _behaviorEditorRoot.RemoveChild(scroll);

        TabContainer behaviorTabs = new TabContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        
        // Tab 1: 事件规则
        scroll.Name = "事件规则";
        behaviorTabs.AddChild(scroll);
        
        // Tab 2: 扩展道具
        var itemsScroll = new ScrollContainer { Name = "扩展道具", HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        _behaviorItemsContainer = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        itemsScroll.AddChild(_behaviorItemsContainer);
        behaviorTabs.AddChild(itemsScroll);
        
        // Tab 3: 交互菜单
        var menusScroll = new ScrollContainer { Name = "交互菜单", HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        _behaviorMenusContainer = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        menusScroll.AddChild(_behaviorMenusContainer);
        behaviorTabs.AddChild(menusScroll);

        _behaviorEditorRoot.AddChild(behaviorTabs);

        // Common UI
        _fileContextMenu = GetNode<PopupMenu>("FileContextMenu");
        _deleteDialog = GetNode<ConfirmationDialog>("DeleteConfirmDialog");
        _renameDialog = GetNode<AcceptDialog>("RenameDialog");
        _renameEdit = GetNode<LineEdit>("RenameDialog/VBox/RenameEdit");

        // Events
        _btnNew.Pressed += OnNewPressed;
        _btnOpen.Pressed += OnOpenPressed;
        _btnCreateJSON.GetPopup().IdPressed += OnCreateJsonIdPressed;
        _btnExport.Pressed += OnExportPressed;
        _btnBack.Pressed += () => {
            LoadingScreen.TargetScene = "res://scenes/MainMenuScreen.tscn";
            GetTree().ChangeSceneToFile("res://scenes/LoadingScreen.tscn");
        };

        _fileTree.ItemSelected += OnFileItemSelected;
        _fileTree.ItemActivated += OnFileItemSelected;
        _fileTree.GuiInput += OnFileTreeGuiInput;
        
        _fileContextMenu.IdPressed += OnContextMenuIdPressed;
        _deleteDialog.Confirmed += OnDeleteConfirmed;
        _renameDialog.Confirmed += OnRenameConfirmed;
        
        _btnSaveFile.Pressed += OnSaveFilePressed;
        _btnSaveCombined.Pressed += OnSaveCombinedPressed;
        _btnSaveBehavior.Pressed += OnSaveBehaviorPressed;

        GetNode<Button>(rootPath + "ManifestVBox/ResHBox/BtnImportSprite").Pressed += () => ImportAsset("Assets/Sprites");
        GetNode<Button>(rootPath + "ManifestVBox/ResHBox/BtnImportBG").Pressed += () => ImportAsset("Assets/Backgrounds");
        GetNode<Button>(rootPath + "ManifestVBox/ResHBox/BtnImportAudio").Pressed += () => ImportAsset("Assets/Audio");
        GetNode<Button>(rootPath + "ManifestVBox/ResHBox/BtnImportDLL").Pressed += () => ImportAsset("Logic");

        UpdateUIFromManifest();
        RefreshFileTree();
    }

    private void HideAllViews()
    {
        _manifestVBox.Hide();
        _fileEditorVBox.Hide();
        _imagePreviewVBox.Hide();
        _charEditorRoot.Hide();
        _behaviorEditorRoot.Hide();
    }

    private void OnFileItemSelected()
    {
        var item = _fileTree.GetSelected();
        if (item == null) return;
        string path = item.GetMetadata(0).AsString();
        if (string.IsNullOrEmpty(path)) { ShowManifestView(); return; }
        if (System.IO.Directory.Exists(path)) return;
        DispatchFileView(path);
    }

    private void DispatchFileView(string path)
    {
        _currentEditingFilePath = path;
        string fileName = path.GetFile().ToLower();
        string ext = path.GetExtension().ToLower();
        HideAllViews();

        if (fileName == "manifest.json") ShowManifestView();
        else if (fileName == "characters.json" || fileName == "actor_config.json" || fileName == "simulation.json") ShowCharacterEditor(path);
        else if (fileName.EndsWith(".behavior.json") || fileName == "behavior.json") ShowBehaviorEditor(path);
        else if (ext == "png" || ext == "jpg" || ext == "jpeg" || ext == "webp") ShowImageView(path);
        else ShowCodeView(path);
    }

    private void ShowManifestView()
    {
        HideAllViews();
        LoadManifest();
        UpdateUIFromManifest();
        _manifestVBox.Show();
    }

    private void ShowCodeView(string path)
    {
        _fileNameLabel.Text = path.GetFile();
        try {
            if (!Godot.FileAccess.FileExists(path)) return;
            using var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read);
            if (file != null) {
                _fileContentEdit.Text = file.GetAsText();
                _fileEditorVBox.Show();
            }
        } catch (Exception ex) {
            GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast($"无法读取文件: {ex.Message}");
        }
    }

    private void ShowImageView(string path)
    {
        try {
            // 使用格式自适应且物理路径友好的 ResourceProxy.LoadImageTexture 载入图像纹理。
            var tex = EraDream.Core.ResourceProxy.LoadImageTexture(path);
            if (tex != null) {
                _previewTexture.Texture = tex;
                var image = tex.GetImage();
                string sizeStr = image != null ? $"{image.GetWidth()}x{image.GetHeight()}" : "未知尺寸";
                _fileInfoLabel.Text = $"{path.GetFile()} ({sizeStr})";
                _imagePreviewVBox.Show();
            }
            else {
                GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast("图片加载失败，文件可能损坏或是不支持的格式");
            }
        } catch (Exception ex) {
            GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast($"图片加载失败: {ex.Message}");
        }
    }

    private void ShowCharacterEditor(string path)
    {
        string fileName = path.GetFile().ToLower();
        string baseDir = path.GetBaseDir();
        _charEditorRoot.Show();
        _currentActorConfig = null; _currentSimData = null;

        try {
            // 加载当前选中的文件
            if (Godot.FileAccess.FileExists(path)) {
                using var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read);
                string json = file.GetAsText();
                
                if (fileName == "simulation.json") {
                    _charTabs.CurrentTab = 1;
                    _currentSimData = string.IsNullOrWhiteSpace(json) ? new() : JsonSerializer.Deserialize<SimulationData>(json) ?? new();
                } else {
                    _charTabs.CurrentTab = 0;
                    if (!string.IsNullOrWhiteSpace(json) && json.TrimStart().StartsWith("[")) {
                        var list = JsonSerializer.Deserialize<List<ActorConfigData>>(json);
                        _currentActorConfig = list?.FirstOrDefault() ?? new ActorConfigData();
                    } else {
                        _currentActorConfig = string.IsNullOrWhiteSpace(json) ? new() : JsonSerializer.Deserialize<ActorConfigData>(json) ?? new();
                    }
                }
            }

            // 尝试加载配对文件 (如果是 Pack 模式)
            if (fileName == "simulation.json") {
                string actorPath = baseDir.PathJoin("actor_config.json");
                if (Godot.FileAccess.FileExists(actorPath)) {
                    using var f = Godot.FileAccess.Open(actorPath, Godot.FileAccess.ModeFlags.Read);
                    _currentActorConfig = JsonSerializer.Deserialize<ActorConfigData>(f.GetAsText());
                }
            } else if (fileName == "actor_config.json") {
                string simPath = baseDir.PathJoin("simulation.json");
                if (Godot.FileAccess.FileExists(simPath)) {
                    using var f = Godot.FileAccess.Open(simPath, Godot.FileAccess.ModeFlags.Read);
                    _currentSimData = JsonSerializer.Deserialize<SimulationData>(f.GetAsText());
                }
            }

            // 更新 UI 字段
            if (_currentSimData != null) {
                _simInternalIdEdit.Text = _currentSimData.Identity?.InternalId ?? "";
                _simFullNameEdit.Text = _currentSimData.Identity?.FullName ?? "";
                _simShortNameEdit.Text = _currentSimData.Identity?.ShortName ?? "";
                _simPersonalityEdit.Text = _currentSimData.Identity?.PersonalityId ?? "";
            }
            if (_currentActorConfig != null) {
                _charIdEdit.Text = _currentActorConfig.ActorId ?? "";
                _charNameEdit.Text = _currentActorConfig.DisplayName ?? "";
                _charSpriteEdit.Text = _currentActorConfig.Visuals?.DefaultSprite ?? "";
            }
        } catch (Exception ex) {
            GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast($"加载失败: {ex.Message}");
            ShowCodeView(path);
        }
    }

    private void OnSaveCombinedPressed()
    {
        if (string.IsNullOrEmpty(_currentEditingFilePath)) return;

        string absolutePath = System.IO.Path.GetFullPath(ProjectSettings.GlobalizePath(_currentEditingFilePath));
        if (!IsPathWithinProject(absolutePath)) {
            GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast("保存失败: 文件不在项目内！");
            return;
        }

        try {
            var options = new JsonSerializerOptions { WriteIndented = true };
            
            // 同步 UI 到数据对象
            if (_currentSimData != null) {
                if (_currentSimData.Identity == null) _currentSimData.Identity = new CharacterIdentity();
                _currentSimData.Identity.InternalId = _simInternalIdEdit.Text;
                _currentSimData.Identity.FullName = _simFullNameEdit.Text;
                _currentSimData.Identity.ShortName = _simShortNameEdit.Text;
                _currentSimData.Identity.PersonalityId = _simPersonalityEdit.Text;
            }
            if (_currentActorConfig != null) {
                _currentActorConfig.ActorId = _charIdEdit.Text;
                _currentActorConfig.DisplayName = _charNameEdit.Text;
                if (_currentActorConfig.Visuals == null) _currentActorConfig.Visuals = new ActorVisuals();
                _currentActorConfig.Visuals.DefaultSprite = _charSpriteEdit.Text;
            }

            // 保存当前编辑的文件
            string fileName = _currentEditingFilePath.GetFile().ToLower();
            string json = "";
            if (fileName == "simulation.json" && _currentSimData != null) {
                json = JsonSerializer.Serialize(_currentSimData, options);
            } else if (fileName == "actor_config.json" && _currentActorConfig != null) {
                json = JsonSerializer.Serialize(_currentActorConfig, options);
            } else if (fileName == "characters.json" && _currentActorConfig != null) {
                json = JsonSerializer.Serialize(new List<ActorConfigData> { _currentActorConfig }, options);
            }

            if (!string.IsNullOrEmpty(json)) {
                using var file = Godot.FileAccess.Open(_currentEditingFilePath, Godot.FileAccess.ModeFlags.Write);
                file.StoreString(json);
            }

            // 如果是配对文件，且 ID 一致，则提示是否同步保存另一半 (可选，此处直接静默保存以符合"模板化修改"预期)
            if (fileName == "simulation.json" && _currentActorConfig != null) {
                _currentActorConfig.ActorId = _currentSimData.Identity.InternalId; // 保持 ID 一致
                string actorPath = _currentEditingFilePath.GetBaseDir().PathJoin("actor_config.json");
                if (Godot.FileAccess.FileExists(actorPath)) {
                    using var f = Godot.FileAccess.Open(actorPath, Godot.FileAccess.ModeFlags.Write);
                    f.StoreString(JsonSerializer.Serialize(_currentActorConfig, options));
                }
            } else if (fileName == "actor_config.json" && _currentSimData != null) {
                if (_currentSimData.Identity == null) _currentSimData.Identity = new CharacterIdentity();
                _currentSimData.Identity.InternalId = _currentActorConfig.ActorId; // 保持 ID 一致
                string simPath = _currentEditingFilePath.GetBaseDir().PathJoin("simulation.json");
                if (Godot.FileAccess.FileExists(simPath)) {
                    using var f = Godot.FileAccess.Open(simPath, Godot.FileAccess.ModeFlags.Write);
                    f.StoreString(JsonSerializer.Serialize(_currentSimData, options));
                }
            }

            GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast("配置已同步保存！");
        } catch (Exception ex) {
            GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast($"保存失败: {ex.Message}");
        }
    }

    private void OnSaveFilePressed()
    {
        if (string.IsNullOrEmpty(_currentEditingFilePath)) return;

        string absolutePath = System.IO.Path.GetFullPath(ProjectSettings.GlobalizePath(_currentEditingFilePath));
        if (!IsPathWithinProject(absolutePath)) {
            GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast("保存失败: 文件不在项目内！");
            return;
        }

        try {
            using var file = Godot.FileAccess.Open(_currentEditingFilePath, Godot.FileAccess.ModeFlags.Write);
            if (file != null) {
                file.StoreString(_fileContentEdit.Text);
                GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast("文件已保存！");
            }
        } catch (Exception ex) {
            GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast($"保存失败: {ex.Message}");
        }
    }

    private void OnFileTreeGuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Right)
        {
            var item = _fileTree.GetItemAtPosition(mb.Position);
            if (item != null && item.GetParent() != null) 
            {
                _contextTargetItem = item;
                item.Select(0);
                UIUtils.ShowContextMenu(_fileContextMenu, this);
            }
        }
    }

    private void OnContextMenuIdPressed(long id)
    {
        if (_contextTargetItem == null) return;
        if (id == 0) {
            _renameEdit.Text = _contextTargetItem.GetText(0);
            _renameDialog.PopupCentered();
        } else if (id == 1) {
            _deleteDialog.PopupCentered();
        }
    }

    private bool IsPathWithinProject(string absolutePath)
    {
        if (string.IsNullOrEmpty(_cachedNormalizedProjectBase)) return false;

        var normalizedPath = absolutePath.Replace('\\', '/').TrimEnd('/');

        // Use OS-aware string comparison to prevent bypasses on case-insensitive systems (Windows)
        // while remaining secure on case-sensitive ones (Linux/macOS).
        var comparison = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        return normalizedPath.Equals(_cachedNormalizedProjectBase, comparison) ||
               normalizedPath.StartsWith(_cachedNormalizedProjectWithSlash, comparison);
    }

    private void OnDeleteConfirmed()
    {
        string path = _contextTargetItem.GetMetadata(0).AsString();
        string globalPath = ProjectSettings.GlobalizePath(path);

        try {
            string absolutePath = System.IO.Path.GetFullPath(globalPath);

            if (!IsPathWithinProject(absolutePath)) {
                GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast("删除失败: 不能删除项目外的文件！");
                return;
            }

            if (System.IO.Directory.Exists(absolutePath)) System.IO.Directory.Delete(absolutePath, true);
            else if (System.IO.File.Exists(absolutePath)) System.IO.File.Delete(absolutePath);

            if (_currentEditingFilePath == path) {
                _currentEditingFilePath = "";
                ShowManifestView();
            }
            RefreshFileTree();
            GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast("已删除！");
        } catch (Exception ex) {
            GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast($"删除失败: {ex.Message}");
        }
    }

    private void OnRenameConfirmed()
    {
        string oldPath = _contextTargetItem.GetMetadata(0).AsString();
        string newName = _renameEdit.Text.Trim();
        if (string.IsNullOrEmpty(newName)) return;

        string newPath = oldPath.GetBaseDir().PathJoin(newName);
        
        string globalOldPath = ProjectSettings.GlobalizePath(oldPath);
        string globalNewPath = ProjectSettings.GlobalizePath(newPath);

        try {
            string absoluteOldPath = System.IO.Path.GetFullPath(globalOldPath);
            string absoluteNewPath = System.IO.Path.GetFullPath(globalNewPath);

            if (!IsPathWithinProject(absoluteOldPath) || !IsPathWithinProject(absoluteNewPath)) {
                GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast("重命名失败: 目标文件必须在项目内！");
                return;
            }

            if (System.IO.Directory.Exists(absoluteOldPath)) System.IO.Directory.Move(absoluteOldPath, absoluteNewPath);
            else if (System.IO.File.Exists(absoluteOldPath)) System.IO.File.Move(absoluteOldPath, absoluteNewPath);
            if (_currentEditingFilePath == oldPath) _currentEditingFilePath = newPath;
            RefreshFileTree();
            GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast("重命名成功！");
        } catch (Exception ex) {
            GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast($"重命名失败: {ex.Message}");
        }
    }

    private void OnNewPressed()
    {
        FileIOManager.OpenFolderDialog("选择新扩展包保存文件夹", (path) => {
            SetProjectPath(path);
            InitializeFolderStructure();
            SaveManifest();
            RefreshFileTree();
            GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast("新扩展包项目已初始化！");
        });
    }

    private void OnOpenPressed()
    {
        FileIOManager.OpenLoadDialog("选择扩展包清单 (manifest.json)", "manifest.json", (path) => {
            SetProjectPath(path.GetBaseDir());
            LoadManifest();
            UpdateUIFromManifest();
            RefreshFileTree();
            ShowManifestView();
            GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast("项目已打开！");
        });
    }

    private void OnCreateJsonIdPressed(long id)
    {
        if (string.IsNullOrEmpty(_projectPath)) {
            // 处于活跃场景树时给出打开项目提示
            if (IsInsideTree()) GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast("请先打开项目！");
            return;
        }

        // 智能根据选中节点决定新配置文件的目标创建路径
        string baseDir = _projectPath;
        var selectedItem = _fileTree.GetSelected();
        if (selectedItem != null)
        {
            string selectedPath = selectedItem.GetMetadata(0).AsString();
            if (!string.IsNullOrEmpty(selectedPath))
            {
                if (System.IO.Directory.Exists(ProjectSettings.GlobalizePath(selectedPath)))
                {
                    baseDir = selectedPath;
                }
                else
                {
                    baseDir = selectedPath.GetBaseDir();
                }
            }
        }

        string fileName = id switch {
            0 => "simulation.json",
            1 => "actor_config.json",
            2 => "characters.json",
            _ => "behavior.json"
        };
        string path = baseDir.PathJoin(fileName);
        
        if (Godot.FileAccess.FileExists(path)) {
            DispatchFileView(path);
            return;
        }

        string template = id switch {
            // 养成数据模板：包含基础数值、心情、成长率等
            0 => "{\n  \"identity\": {\n    \"internal_id\": \"author:uma_id\",\n    \"full_name\": \"新马娘\",\n    \"short_name\": \"马娘\",\n    \"personality_id\": \"normal\"\n  },\n  \"stats\": {\n    \"initial\": {\n      \"speed\": 120, \"stamina\": 110, \"power\": 115, \"guts\": 95, \"intelligence\": 85, \"skill_points\": 10\n    },\n    \"conditions\": {\n      \"motivation\": 3,\n      \"energy\": 100,\n      \"affection\": 0\n    },\n    \"growth_bonus\": {\n      \"speed\": 0.20,\n      \"stamina\": 0.10,\n      \"power\": 0.0,\n      \"guts\": 0.0,\n      \"intelligence\": 0.0\n    },\n    \"custom_stats\": {}\n  }\n}",
            // 表现数据模板：包含立绘、表情、悬浮对话及语音
            1 => "{\n  \"actor_id\": \"author:uma_id\",\n  \"display_name\": \"新角色\",\n  \"visuals\": {\n    \"default_sprite\": \"Assets/Sprites/body_idle.png\",\n    \"expressions\": {\n      \"0\": \"Assets/Sprites/body_normal.png\"\n    },\n    \"stickers\": {}\n  },\n  \"barks\": [\n    {\n      \"condition\": \"energy < 30\",\n      \"text\": \"有点累了...\",\n      \"expression\": \"0\"\n    }\n  ],\n  \"audio\": {\n    \"typing_sound\": \"\",\n    \"fallback_voices\": []\n  }\n}",
            // 剧本客串角色模板：数组形式的表现数据
            2 => "[\n  {\n    \"actor_id\": \"guest_actor_01\",\n    \"display_name\": \"客串角色\",\n    \"visuals\": {\n      \"default_sprite\": \"\",\n      \"expressions\": {},\n      \"stickers\": {}\n    },\n    \"barks\": [],\n    \"audio\": {}\n  }\n]",
            // 行为包模板
            _ => "{\n  \"rules\": [\n    {\n      \"id\": \"rule_01\",\n      \"hook\": \"OnTraining\",\n      \"conditions\": [],\n      \"probability\": 1.0,\n      \"action\": {\n        \"type\": \"DetailedStory\",\n        \"path\": \"\"\n      }\n    }\n  ]\n}"
        };

        try {
            var dirPath = path.GetBaseDir();
            if (!DirAccess.DirExistsAbsolute(dirPath)) DirAccess.MakeDirRecursiveAbsolute(dirPath);

            using (var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Write)) {
                file.StoreString(template);
            }
            RefreshFileTree();
            DispatchFileView(path);
            // 处于活跃场景树时触发 Toast 状态提醒
            if (IsInsideTree()) GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast($"已创建: {fileName.GetFile()}");
        } catch (Exception ex) {
            // 处于活跃场景树时报告创建失败异常
            if (IsInsideTree()) GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast($"创建失败: {ex.Message}");
        }
    }

    private void RefreshFileTree()
    {
        _fileTree.Clear();
        if (string.IsNullOrEmpty(_projectPath)) return;
        TreeItem root = _fileTree.CreateItem();
        root.SetText(0, _projectPath.GetFile());
        root.SetMetadata(0, ""); 
        ScanFolderToTree(_projectPath, root);
    }

    private void ScanFolderToTree(string path, TreeItem parent)
    {
        using var dir = Godot.DirAccess.Open(path);
        if (dir == null) return;
        dir.ListDirBegin();
        string fileName = dir.GetNext();
        while (fileName != "") {
            if (fileName == "." || fileName == "..") { fileName = dir.GetNext(); continue; }
            string fullPath = path.PathJoin(fileName);
            TreeItem item = _fileTree.CreateItem(parent);
            item.SetText(0, fileName);
            item.SetMetadata(0, fullPath);
            item.SetSelectable(0, true);
            if (dir.CurrentIsDir()) { ScanFolderToTree(fullPath, item); }
            fileName = dir.GetNext();
        }
    }

    private void OnExportPressed()
    {
        if (string.IsNullOrEmpty(_projectPath)) return;
        SyncManifestFromUI();
        SaveManifest();
        string exportName = $"{_manifest.Id}_{_manifest.Version}.umaext";
        FileIOManager.OpenSaveDialog("导出扩展包", exportName, "*.umaext", (path) => {
            try {
                if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
                ZipFile.CreateFromDirectory(ProjectSettings.GlobalizePath(_projectPath), ProjectSettings.GlobalizePath(path), CompressionLevel.Optimal, false);
                GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast("导出成功！");
            } catch (Exception ex) {
                GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowErrorDialog("导出失败", ex.Message);
            }
        });
    }

    private void InitializeFolderStructure()
    {
        if (string.IsNullOrEmpty(_projectPath)) return;
        string[] dirs = { "Data", "Assets/Sprites", "Assets/Backgrounds", "Assets/Audio", "Logic" };
        foreach (string d in dirs) Godot.DirAccess.MakeDirRecursiveAbsolute(_projectPath.PathJoin(d));
    }

    private void ImportAsset(string subDir)
    {
        if (string.IsNullOrEmpty(_projectPath)) { GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast("请先新建或打开一个项目！"); return; }
        FileIOManager.OpenLoadDialog("选择资源文件", "*.*", (sourcePath) => {
            string destPath = _projectPath.PathJoin(subDir).PathJoin(sourcePath.GetFile());
            if (Godot.DirAccess.CopyAbsolute(sourcePath, destPath) == Godot.Error.Ok) {
                RefreshFileTree();
                GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast($"导入成功: {sourcePath.GetFile()}");
            }
        });
    }

    private void LoadManifest()
    {
        string path = _projectPath.PathJoin("manifest.json");
        if (Godot.FileAccess.FileExists(path)) {
            using var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read);
            _manifest = JsonSerializer.Deserialize<ExtensionManifest>(file.GetAsText()) ?? new();
        }
    }

    private void SaveManifest()
    {
        if (string.IsNullOrEmpty(_projectPath)) return;
        SyncManifestFromUI();
        string json = JsonSerializer.Serialize(_manifest, new JsonSerializerOptions { WriteIndented = true });
        using (var file = Godot.FileAccess.Open(_projectPath.PathJoin("manifest.json"), Godot.FileAccess.ModeFlags.Write)) {
            file.StoreString(json);
        }
    }

    private void OnCreateFolderPressed()
    {
        if (string.IsNullOrEmpty(_projectPath)) {
            GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast("请先打开项目！");
            return;
        }

        string baseDir = _projectPath;
        var selectedItem = _fileTree.GetSelected();
        if (selectedItem != null)
        {
            string selectedPath = selectedItem.GetMetadata(0).AsString();
            if (!string.IsNullOrEmpty(selectedPath))
            {
                if (System.IO.Directory.Exists(ProjectSettings.GlobalizePath(selectedPath)))
                {
                    baseDir = selectedPath;
                }
                else
                {
                    baseDir = selectedPath.GetBaseDir();
                }
            }
        }

        string newFolderName = "NewFolder";
        string newFolderPath = baseDir.PathJoin(newFolderName);
        int counter = 1;
        while (System.IO.Directory.Exists(ProjectSettings.GlobalizePath(newFolderPath)))
        {
            newFolderPath = baseDir.PathJoin($"{newFolderName}_{counter}");
            counter++;
        }

        try {
            string globalPath = ProjectSettings.GlobalizePath(newFolderPath);
            System.IO.Directory.CreateDirectory(globalPath);
            RefreshFileTree();
            GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast("文件夹创建成功！可在右键菜单中重命名。");
        }
        catch (Exception ex) {
            GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast($"文件夹创建失败: {ex.Message}");
        }
    }

    private void UpdateUIFromManifest()
    {
        _idEdit.Text = _manifest.Id ?? ""; _nameEdit.Text = _manifest.Name ?? ""; _authorEdit.Text = _manifest.Author ?? "";
        _versionEdit.Text = _manifest.Version ?? ""; _minVerEdit.Text = _manifest.MinGameVersion ?? "";
        _descEdit.Text = _manifest.Description ?? ""; _typeOption.Selected = _manifest.Type == EraDream.Editor.Models.PackType.Gameplay ? 1 : 0;
    }

    private void SyncManifestFromUI()
    {
        _manifest.Id = _idEdit.Text; _manifest.Name = _nameEdit.Text; _manifest.Author = _authorEdit.Text;
        _manifest.Version = _versionEdit.Text; _manifest.MinGameVersion = _minVerEdit.Text;
        _manifest.Description = _descEdit.Text; _manifest.Type = _typeOption.Selected == 1 ? EraDream.Editor.Models.PackType.Gameplay : EraDream.Editor.Models.PackType.Character;
    }
}
