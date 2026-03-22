using Godot;
using System;
using System.Collections.Generic;

// 贴纸列表管理窗口，复用 CharacterEditorUI 的 UI 结构
public partial class StickerEditorUI : Node
{
    public static void Open(Node parent)
    {
        var editor = new StickerEditorUI();
        parent.AddChild(editor);
        editor.CreateManagerWindow();
    }

    private void CreateManagerWindow()
    {
        Window window = new Window { 
            Title = "贴纸库管理", 
            Size = new Vector2I(550, 600), 
            Transient = true,
            Exclusive = true,
            InitialPosition = Window.WindowInitialPosition.CenterPrimaryScreen
        };
        
        VBoxContainer root = new VBoxContainer { 
            CustomMinimumSize = new Vector2(500, 550),
            OffsetLeft = 10, OffsetRight = -10, OffsetTop = 10, OffsetBottom = -10
        };
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);

        Panel background = new Panel();
        background.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        window.AddChild(background);
        background.AddChild(root);

        // 顶部操作区
        HBoxContainer topBar = new HBoxContainer();
        LineEdit nameInput = new LineEdit { PlaceholderText = "新贴纸名称", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        Button addBtn = new Button { Text = " 添加新贴纸 " };
        topBar.AddChild(nameInput);
        topBar.AddChild(addBtn);
        root.AddChild(topBar);

        // 滚动列表区
        ScrollContainer scroll = new ScrollContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        VBoxContainer listContainer = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        scroll.AddChild(listContainer);
        root.AddChild(scroll);

        // 渲染列表方法
        Action refreshList = () => {
            foreach (Node child in listContainer.GetChildren()) child.QueueFree();
            foreach (var sticker in StickerManager.Stickers)
            {
                listContainer.AddChild(CreateStickerItemUI(sticker));
            }
        };

        addBtn.Pressed += () => {
            if (!string.IsNullOrEmpty(nameInput.Text)) {
                StickerManager.AddSticker(nameInput.Text);
                nameInput.Text = "";
                refreshList();
            }
        };

        refreshList();
        
        AddChild(window);

        window.CloseRequested += () => {
            window.QueueFree();
            this.QueueFree();
        };
        window.Popup();
    }

    // 创建单个贴纸条目 UI
    private Control CreateStickerItemUI(StickerData stickerData)
    {
        VBoxContainer itemRoot = new VBoxContainer();
        
        // 头部按钮（可展开/收起）
        Button header = new Button { 
            Text = $" >  [{stickerData.Id}] {stickerData.Name}", 
            Alignment = HorizontalAlignment.Left,
            CustomMinimumSize = new Vector2(0, 35)
        };
        itemRoot.AddChild(header);

        // 详细面板
        VBoxContainer detail = new VBoxContainer { 
            Visible = false,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        detail.AddThemeConstantOverride("margin_left", 20);
        
        // 名称编辑
        HBoxContainer nameRow = new HBoxContainer();
        nameRow.AddChild(new Label { Text = "名称:", CustomMinimumSize = new Vector2(80, 0) });
        LineEdit nameEdit = new LineEdit { Text = stickerData.Name, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        nameRow.AddChild(nameEdit);
        detail.AddChild(nameRow);

        // 图片文件选择（复用 SpriteLibrary）
        HBoxContainer imageRow = new HBoxContainer();
        imageRow.AddChild(new Label { Text = "贴纸图片:", CustomMinimumSize = new Vector2(80, 0) });
        OptionButton imageSelect = new OptionButton { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        SpriteLibrary.PopulateOptionButton(imageSelect, stickerData.ImageFile);
        imageRow.AddChild(imageSelect);
        detail.AddChild(imageRow);

        // 删除贴纸按钮
        Button delBtn = new Button { Text = "删除此贴纸", Modulate = new Color(1, 0.4f, 0.4f) };
        delBtn.Pressed += () => {
            StickerManager.RemoveSticker(stickerData.Id);
            itemRoot.QueueFree();
        };
        detail.AddChild(delBtn);

        itemRoot.AddChild(detail);

        // 展开/收起逻辑
        header.Pressed += () => {
            detail.Visible = !detail.Visible;
            header.Text = (detail.Visible ? " v  " : " >  ") + $"[{stickerData.Id}] {stickerData.Name}";
        };

        // 实时同步数据
        nameEdit.TextChanged += (txt) => {
            stickerData.Name = txt;
            header.Text = (detail.Visible ? " v  " : " >  ") + $"[{stickerData.Id}] {txt}";
        };
        imageSelect.ItemSelected += (idx) => {
            stickerData.ImageFile = imageSelect.GetItemText((int)idx);
        };

        return itemRoot;
    }
}
