using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EraDream.Core;

public static class ResourceManagerUI
{
    public enum ResourceType { Background, Audio, Sprite, Font }

    // 图片导入策略会保留在本次编辑器会话中，避免用户重复设置相同选项。
    private static readonly Dictionary<ResourceType, bool> _batchImportSettings = new()
    {
        [ResourceType.Background] = true,
        [ResourceType.Sprite] = true
    };

    /// <summary>资源导入成功后通知已打开的节点刷新下拉列表。</summary>
    public static event Action<ResourceType> ResourcesChanged;

    public static void OpenImportDialog(ResourceType type)
    {
        if (!ProjectManager.IsProjectOpened)
        {
            ((SceneTree)Engine.GetMainLoop()).Root.GetNodeOrNull<ErrorNotifier>("ErrorNotifier")?.ShowErrorDialog("导入失败", "Must open a project before importing resources.");
            return;
        }

        string title = "导入资源";
        string filter = "";
        string targetSubDir = "";

        switch (type)
        {
            case ResourceType.Background:
                title = "导入背景图";
                filter = "*.png,*.jpg,*.webp";
                targetSubDir = "backgrounds";
                break;
            case ResourceType.Audio:
                title = "导入音频文件";
                filter = "*.mp3,*.ogg,*.wav";
                targetSubDir = "audio";
                break;
            case ResourceType.Sprite:
                title = "导入角色立绘";
                filter = "*.png,*.webp";
                targetSubDir = "sprites";
                break;
            case ResourceType.Font:
                title = "导入字体文件";
                filter = "*.ttf,*.otf";
                targetSubDir = "fonts";
                break;
        }

        bool allowMultiple = type == ResourceType.Background || type == ResourceType.Sprite;
        if (allowMultiple)
        {
            // 背景和角色立绘都支持批量导入；角色差分的归属仍由角色管理器配置。
            OpenBatchImportSettings(type, title, filter, targetSubDir);
            return;
        }

        FileIOManager.OpenLoadDialog(title, filter, sourcePath => ImportProcess(sourcePath, type, targetSubDir));
    }

    private static void OpenBatchImportSettings(ResourceType type, string title, string filter, string targetSubDir)
    {
        Window window = new Window
        {
            Title = type == ResourceType.Background ? "背景批量导入设置" : "角色立绘批量导入设置",
            Size = new Vector2I(430, 190),
            Transient = true,
            Exclusive = true,
            InitialPosition = Window.WindowInitialPosition.CenterPrimaryScreen
        };

        VBoxContainer root = new VBoxContainer
        {
            CustomMinimumSize = new Vector2(390, 130),
            OffsetLeft = 20,
            OffsetRight = -20,
            OffsetTop = 16,
            OffsetBottom = -16
        };
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        root.AddChild(new Label
        {
            Text = type == ResourceType.Background
                ? "选择背景图片导入方式："
                : "选择角色立绘导入方式："
        });

        OptionButton mode = new OptionButton { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        if (type == ResourceType.Background)
        {
            mode.AddItem("批量选择，每张图片创建一个背景条目");
            mode.AddItem("单张选择，导入一个背景条目");
        }
        else
        {
            mode.AddItem("批量选择，导入多个立绘资源（用于变体/差分）");
            mode.AddItem("单张选择，导入一个立绘资源");
        }
        mode.Selected = _batchImportSettings.TryGetValue(type, out bool enabled) && enabled ? 0 : 1;
        root.AddChild(mode);

        HBoxContainer buttons = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.End
        };
        Button cancel = new Button { Text = "取消" };
        Button confirm = new Button { Text = "选择图片" };
        buttons.AddChild(cancel);
        buttons.AddChild(confirm);
        root.AddChild(buttons);
        window.AddChild(root);
        ((SceneTree)Engine.GetMainLoop()).Root.AddChild(window);

        cancel.Pressed += window.QueueFree;
        confirm.Pressed += () =>
        {
            _batchImportSettings[type] = mode.Selected == 0;
            window.QueueFree();
            bool allowMultiple = _batchImportSettings[type];
            FileIOManager.OpenLoadDialog(title, filter, allowMultiple, paths => ImportBatch(paths, type, targetSubDir));
        };
        window.CloseRequested += window.QueueFree;
        window.PopupCentered();
    }

    private static void ImportBatch(string[] sourcePaths, ResourceType type, string targetSubDir)
    {
        int importedCount = 0;
        foreach (string sourcePath in sourcePaths.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrEmpty(ImportProcess(sourcePath, type, targetSubDir)))
                importedCount++;
        }

        if (importedCount > 0)
            ResourcesChanged?.Invoke(type);
        else
            ((SceneTree)Engine.GetMainLoop()).Root.GetNodeOrNull<ErrorNotifier>("ErrorNotifier")?.ShowToast("没有成功导入图片");
    }

    private static string ImportProcess(string sourcePath, ResourceType type, string targetSubDir)
    {
        // 确保路径规范
        string absoluteSource = ProjectSettings.GlobalizePath(sourcePath);
        string fileName = ProjectManager.ImportFile(absoluteSource, targetSubDir);

        if (!string.IsNullOrEmpty(fileName))
        {
            GD.Print($"Successfully imported: {fileName} to {targetSubDir}");
            return fileName;
        }
        else
        {
            ((SceneTree)Engine.GetMainLoop()).Root.GetNodeOrNull<ErrorNotifier>("ErrorNotifier")?.ShowToast($"Failed to import: {sourcePath}");
            return "";
        }
    }
}
