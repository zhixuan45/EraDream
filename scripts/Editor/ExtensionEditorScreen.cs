using Godot;
using System;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using UmaEraArchive.Editor.Models;
using UmaEraArchive.Core;

public partial class ExtensionEditorScreen : Control
{
    private string _projectPath = "";
    private ExtensionManifest _manifest = new();

    // UI Nodes
    private LineEdit _idEdit, _nameEdit, _authorEdit, _versionEdit, _minVerEdit;
    private TextEdit _descEdit;
    private OptionButton _typeOption;
    private Button _btnNew, _btnOpen, _btnExport, _btnBack;
    private Tree _fileTree;

    // File Interaction UI
    private Control _manifestVBox, _fileEditorVBox;
    private Label _fileNameLabel;
    private TextEdit _fileContentEdit;
    private Button _btnSaveFile;
    private PopupMenu _fileContextMenu;
    private ConfirmationDialog _deleteDialog;
    private AcceptDialog _renameDialog;
    private LineEdit _renameEdit;

    private string _currentEditingFilePath = "";
    private TreeItem _contextTargetItem = null;

    public override void _Ready()
    {
        // Bind UI
        _idEdit = GetNode<LineEdit>("SafeArea/HSplit/MainArea/ManifestVBox/Grid/IdEdit");
        _nameEdit = GetNode<LineEdit>("SafeArea/HSplit/MainArea/ManifestVBox/Grid/NameEdit");
        _authorEdit = GetNode<LineEdit>("SafeArea/HSplit/MainArea/ManifestVBox/Grid/AuthorEdit");
        _versionEdit = GetNode<LineEdit>("SafeArea/HSplit/MainArea/ManifestVBox/Grid/VersionEdit");
        _minVerEdit = GetNode<LineEdit>("SafeArea/HSplit/MainArea/ManifestVBox/Grid/MinVerEdit");
        _descEdit = GetNode<TextEdit>("SafeArea/HSplit/MainArea/ManifestVBox/Grid/DescEdit");
        _typeOption = GetNode<OptionButton>("SafeArea/HSplit/MainArea/ManifestVBox/Grid/TypeOption");

        _btnNew = GetNode<Button>("SafeArea/HSplit/SidePanel/VBox/BtnNewProject");
        _btnOpen = GetNode<Button>("SafeArea/HSplit/SidePanel/VBox/BtnOpenProject");
        _btnExport = GetNode<Button>("SafeArea/HSplit/SidePanel/VBox/BtnExport");
        _btnBack = GetNode<Button>("SafeArea/HSplit/SidePanel/VBox/BtnBack");
        _fileTree = GetNode<Tree>("SafeArea/HSplit/SidePanel/VBox/FileTree");

        // New File Interaction UI
        _manifestVBox = GetNode<Control>("SafeArea/HSplit/MainArea/ContentRoot/ManifestVBox");
        _fileEditorVBox = GetNode<Control>("SafeArea/HSplit/MainArea/ContentRoot/FileEditorVBox");
        _fileNameLabel = GetNode<Label>("SafeArea/HSplit/MainArea/ContentRoot/FileEditorVBox/FileHeader/FileNameLabel");
        _fileContentEdit = GetNode<TextEdit>("SafeArea/HSplit/MainArea/ContentRoot/FileEditorVBox/FileContentEdit");
        _btnSaveFile = GetNode<Button>("SafeArea/HSplit/MainArea/ContentRoot/FileEditorVBox/FileHeader/BtnSaveFile");
        
        _fileContextMenu = GetNode<PopupMenu>("FileContextMenu");
        _deleteDialog = GetNode<ConfirmationDialog>("DeleteConfirmDialog");
        _renameDialog = GetNode<AcceptDialog>("RenameDialog");
        _renameEdit = GetNode<LineEdit>("RenameDialog/VBox/RenameEdit");

        // Events
        _btnNew.Pressed += OnNewPressed;
        _btnOpen.Pressed += OnOpenPressed;
        _btnExport.Pressed += OnExportPressed;
        _btnBack.Pressed += () => {
            LoadingScreen.TargetScene = "res://scenes/MainMenuScreen.tscn";
            GetTree().ChangeSceneToFile("res://scenes/LoadingScreen.tscn");
        };

        _fileTree.ItemSelected += OnFileItemSelected;
        _fileTree.GuiInput += OnFileTreeGuiInput;
        _fileContextMenu.IdPressed += OnContextMenuIdPressed;
        _deleteDialog.Confirmed += OnDeleteConfirmed;
        _renameDialog.Confirmed += OnRenameConfirmed;
        _btnSaveFile.Pressed += OnSaveFilePressed;

        GetNode<Button>("SafeArea/HSplit/MainArea/ManifestVBox/ResHBox/BtnImportSprite").Pressed += () => ImportAsset("Assets/Sprites");
        GetNode<Button>("SafeArea/HSplit/MainArea/ManifestVBox/ResHBox/BtnImportBG").Pressed += () => ImportAsset("Assets/Backgrounds");
        GetNode<Button>("SafeArea/HSplit/MainArea/ManifestVBox/ResHBox/BtnImportAudio").Pressed += () => ImportAsset("Assets/Audio");
        GetNode<Button>("SafeArea/HSplit/MainArea/ManifestVBox/ResHBox/BtnImportDLL").Pressed += () => ImportAsset("Logic");

        UpdateUIFromManifest();
        RefreshFileTree();
    }

    private void OnFileItemSelected()
    {
        var item = _fileTree.GetSelected();
        if (item == null) return;

        string fullPath = item.GetMetadata(0).AsString();
        if (string.IsNullOrEmpty(fullPath)) 
        {
            // 点击根目录，显示 Manifest
            _manifestVBox.Show();
            _fileEditorVBox.Hide();
            return;
        }

        if (System.IO.Directory.Exists(fullPath)) return;

        OpenFileForEditing(fullPath);
    }

    private void OpenFileForEditing(string path)
    {
        _currentEditingFilePath = path;
        _fileNameLabel.Text = path.GetFile();
        
        try {
            using var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read);
            if (file != null) {
                _fileContentEdit.Text = file.GetAsText();
                _manifestVBox.Hide();
                _fileEditorVBox.Show();
            }
        } catch (Exception ex) {
            GetNode<ErrorNotifier>("/root/ErrorNotifier").ShowToast($"无法读取文件: {ex.Message}");
        }
    }

    private void OnSaveFilePressed()
    {
        if (string.IsNullOrEmpty(_currentEditingFilePath)) return;

        try {
            using var file = Godot.FileAccess.Open(_currentEditingFilePath, Godot.FileAccess.ModeFlags.Write);
            if (file != null) {
                file.StoreString(_fileContentEdit.Text);
                GetNode<ErrorNotifier>("/root/ErrorNotifier").ShowToast("文件已保存！");
            }
        } catch (Exception ex) {
            GetNode<ErrorNotifier>("/root/ErrorNotifier").ShowToast($"保存失败: {ex.Message}");
        }
    }

    private void OnFileTreeGuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Right)
        {
            var item = _fileTree.GetItemAtPosition(mb.Position);
            if (item != null && item.GetParent() != null) // 根目录不可操作
            {
                _contextTargetItem = item;
                _fileContextMenu.Position = (Vector2I)GetViewport().GetMousePosition() + (Vector2I)DisplayServer.WindowGetPosition();
                _fileContextMenu.Popup();
            }
        }
    }

    private void OnContextMenuIdPressed(long id)
    {
        if (_contextTargetItem == null) return;

        if (id == 0) // 重命名
        {
            _renameEdit.Text = _contextTargetItem.GetText(0);
            _renameDialog.PopupCentered();
        }
        else if (id == 1) // 删除
        {
            _deleteDialog.PopupCentered();
        }
    }

    private void OnDeleteConfirmed()
    {
        string path = _contextTargetItem.GetMetadata(0).AsString();
        try {
            if (System.IO.Directory.Exists(path)) System.IO.Directory.Delete(path, true);
            else if (System.IO.File.Exists(path)) System.IO.File.Delete(path);

            if (_currentEditingFilePath == path) {
                _currentEditingFilePath = "";
                _fileEditorVBox.Hide();
                _manifestVBox.Show();
            }
            RefreshFileTree();
            GetNode<ErrorNotifier>("/root/ErrorNotifier").ShowToast("已删除！");
        } catch (Exception ex) {
            GetNode<ErrorNotifier>("/root/ErrorNotifier").ShowToast($"删除失败: {ex.Message}");
        }
    }

    private void OnRenameConfirmed()
    {
        string oldPath = _contextTargetItem.GetMetadata(0).AsString();
        string newName = _renameEdit.Text.Trim();
        if (string.IsNullOrEmpty(newName)) return;

        string newPath = oldPath.GetBaseDir().PathJoin(newName);
        try {
            if (System.IO.Directory.Exists(oldPath)) System.IO.Directory.Move(oldPath, newPath);
            else if (System.IO.File.Exists(oldPath)) System.IO.File.Move(oldPath, newPath);

            if (_currentEditingFilePath == oldPath) _currentEditingFilePath = newPath;
            
            RefreshFileTree();
            GetNode<ErrorNotifier>("/root/ErrorNotifier").ShowToast("重命名成功！");
        } catch (Exception ex) {
            GetNode<ErrorNotifier>("/root/ErrorNotifier").ShowToast($"重命名失败: {ex.Message}");
        }
    }

    private void OnNewPressed()
    {
        FileIOManager.OpenFolderDialog("选择新扩展包保存文件夹", (path) => {
            _projectPath = path;
            InitializeFolderStructure();
            SaveManifest();
            RefreshFileTree();
            GetNode<ErrorNotifier>("/root/ErrorNotifier").ShowToast("新扩展包项目已初始化！");
        });
    }

    private void OnOpenPressed()
    {
        FileIOManager.OpenLoadDialog("选择扩展包清单 (manifest.json)", "manifest.json", (path) => {
            _projectPath = path.GetBaseDir();
            LoadManifest();
            UpdateUIFromManifest();
            RefreshFileTree();
            _manifestVBox.Show();
            _fileEditorVBox.Hide();
            GetNode<ErrorNotifier>("/root/ErrorNotifier").ShowToast("项目已打开！");
        });
    }

    private void RefreshFileTree()
    {
        _fileTree.Clear();
        if (string.IsNullOrEmpty(_projectPath)) return;

        TreeItem root = _fileTree.CreateItem();
        root.SetText(0, _projectPath.GetFile());
        root.SetMetadata(0, ""); // 根目录 Meta 为空表示 Manifest 视图
        
        ScanFolderToTree(_projectPath, root);
    }

    private void ScanFolderToTree(string path, TreeItem parent)
    {
        using var dir = Godot.DirAccess.Open(path);
        if (dir == null) return;

        dir.ListDirBegin();
        string fileName = dir.GetNext();
        while (fileName != "")
        {
            if (fileName == "." || fileName == "..") {
                fileName = dir.GetNext();
                continue;
            }

            string fullPath = path.PathJoin(fileName);
            TreeItem item = _fileTree.CreateItem(parent);
            item.SetText(0, fileName);
            item.SetMetadata(0, fullPath);

            if (dir.CurrentIsDir()) {
                item.SetSelectable(0, true); // 允许选择目录以执行操作
                ScanFolderToTree(fullPath, item);
            }
            
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
                
                string globalSource = ProjectSettings.GlobalizePath(_projectPath);
                string globalDest = ProjectSettings.GlobalizePath(path);
                
                ZipFile.CreateFromDirectory(globalSource, globalDest, CompressionLevel.Optimal, false);
                GetNode<ErrorNotifier>("/root/ErrorNotifier").ShowToast("导出成功！");
            } catch (Exception ex) {
                GD.PrintErr($"Export failed: {ex.Message}");
                GetNode<ErrorNotifier>("/root/ErrorNotifier").ShowErrorDialog("导出失败", ex.Message);
            }
        });
    }

    private void InitializeFolderStructure()
    {
        if (string.IsNullOrEmpty(_projectPath)) return;

        string[] dirs = { "Data", "Assets/Sprites", "Assets/Backgrounds", "Assets/Audio", "Logic" };
        foreach (string d in dirs)
        {
            string fullPath = _projectPath.PathJoin(d);
            Godot.DirAccess.MakeDirRecursiveAbsolute(fullPath);
        }
    }

    private void ImportAsset(string subDir)
    {
        if (string.IsNullOrEmpty(_projectPath)) {
            GetNode<ErrorNotifier>("/root/ErrorNotifier").ShowToast("请先新建或打开一个项目！");
            return;
        }

        FileIOManager.OpenLoadDialog("选择资源文件", "*.*", (sourcePath) => {
            string fileName = sourcePath.GetFile();
            string destPath = _projectPath.PathJoin(subDir).PathJoin(fileName);
            
            Godot.Error err = Godot.DirAccess.CopyAbsolute(sourcePath, destPath);
            if (err == Godot.Error.Ok) {
                RefreshFileTree();
                GetNode<ErrorNotifier>("/root/ErrorNotifier").ShowToast($"导入成功: {fileName}");
            } else {
                GetNode<ErrorNotifier>("/root/ErrorNotifier").ShowToast($"导入失败: {err}");
            }
        });
    }

    private void LoadManifest()
    {
        string path = _projectPath.PathJoin("manifest.json");
        if (Godot.FileAccess.FileExists(path))
        {
            using var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read);
            string json = file.GetAsText();
            _manifest = JsonSerializer.Deserialize<ExtensionManifest>(json) ?? new();
        }
    }

    private void SaveManifest()
    {
        if (string.IsNullOrEmpty(_projectPath)) return;
        SyncManifestFromUI();
        string json = JsonSerializer.Serialize(_manifest, new JsonSerializerOptions { WriteIndented = true });
        string path = _projectPath.PathJoin("manifest.json");
        using var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Write);
        file.StoreString(json);
    }

    private void UpdateUIFromManifest()
    {
        _idEdit.Text = _manifest.Id;
        _nameEdit.Text = _manifest.Name;
        _authorEdit.Text = _manifest.Author;
        _versionEdit.Text = _manifest.Version;
        _minVerEdit.Text = _manifest.MinGameVersion;
        _descEdit.Text = _manifest.Description;
        _typeOption.Selected = _manifest.Type == "gameplay" ? 1 : 0;
    }

    private void SyncManifestFromUI()
    {
        _manifest.Id = _idEdit.Text;
        _manifest.Name = _nameEdit.Text;
        _manifest.Author = _authorEdit.Text;
        _manifest.Version = _versionEdit.Text;
        _manifest.MinGameVersion = _minVerEdit.Text;
        _manifest.Description = _descEdit.Text;
        _manifest.Type = _typeOption.Selected == 1 ? "gameplay" : "character";
    }
}
