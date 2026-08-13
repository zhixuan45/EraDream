using Godot;
using System;
using System.Collections.Generic;
using EraDream.Core.Models;

public partial class CharacterEditorUI : Node
{
    private Action<Action> _applyEditorDataChange;

    public static void Open(Node parent, Action<Action> applyEditorDataChange = null)
    {
        var editor = new CharacterEditorUI
        {
            _applyEditorDataChange = applyEditorDataChange
        };
        parent.AddChild(editor);
        editor.CreateManagerWindow();
    }

    private void ApplyDataChange(Action change)
    {
        if (_applyEditorDataChange != null)
            _applyEditorDataChange(change);
        else
            change();
    }

    private void CreateManagerWindow()
    {
        Window window = new Window { 
            Title = "角色库管理", 
            Size = new Vector2I(550, 650), 
            Transient = true,
            Exclusive = true,
            InitialPosition = Window.WindowInitialPosition.CenterPrimaryScreen
        };
        
        VBoxContainer root = new VBoxContainer { 
            CustomMinimumSize = new Vector2(500, 600),
            OffsetLeft = 10, OffsetRight = -10, OffsetTop = 10, OffsetBottom = -10
        };
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);

        Panel background = new Panel();
        background.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        window.AddChild(background);
        background.AddChild(root);

        // 顶部操作区
        HBoxContainer topBar = new HBoxContainer();
        LineEdit nameInput = new LineEdit { PlaceholderText = "新角色 ID", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        Button addBtn = new Button { Text = " 添加新角色 " };
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
            foreach (var character in CharacterManager.Characters)
            {
                listContainer.AddChild(CreateCharacterItemUI(character));
            }
        };

        addBtn.Pressed += () => {
            if (!string.IsNullOrEmpty(nameInput.Text)) {
                // 暂时不通过 CharacterManager.AddCharacter 增加，因为其 API 已变
                string actorId = nameInput.Text;
                ApplyDataChange(() => CharacterManager.GuestActors[actorId] = new ActorConfigData { ActorId = actorId, DisplayName = "新角色" });
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

    private Control CreateCharacterItemUI(ActorConfigData charData)
    {
        VBoxContainer itemRoot = new VBoxContainer();
        
        // --- 头部 (Header) ---
        Button header = new Button { 
            Text = $" >  [{charData.ActorId}] {charData.DisplayName}", 
            Alignment = HorizontalAlignment.Left,
            CustomMinimumSize = new Vector2(0, 35)
        };
        itemRoot.AddChild(header);

        // --- 详细面板 (Detail) ---
        VBoxContainer detail = new VBoxContainer { 
            Visible = false,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        detail.AddThemeConstantOverride("margin_left", 20);
        
        // 名称编辑
        HBoxContainer nameRow = new HBoxContainer();
        nameRow.AddChild(new Label { Text = "显示名称:", CustomMinimumSize = new Vector2(80, 0) });
        LineEdit nameEdit = new LineEdit { Text = charData.DisplayName, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        nameRow.AddChild(nameEdit);
        detail.AddChild(nameRow);

        // 默认立绘选择
        HBoxContainer spriteRow = new HBoxContainer();
        spriteRow.AddChild(new Label { Text = "默认立绘:", CustomMinimumSize = new Vector2(80, 0) });
        OptionButton spriteSelect = new OptionButton { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        SpriteLibrary.PopulateOptionButton(spriteSelect, charData.Visuals.DefaultSprite);
        spriteRow.AddChild(spriteSelect);
        detail.AddChild(spriteRow);

        // 表情映射
        detail.AddChild(new HSeparator());
        detail.AddChild(new Label { Text = "表情/状态映射:" });
        VBoxContainer exprList = new VBoxContainer();
        foreach (var pair in charData.Visuals.Expressions)
        {
            exprList.AddChild(CreateExpressionRow(charData, pair.Key, pair.Value));
        }
        detail.AddChild(exprList);

        Button addExprBtn = new Button { Text = "+ 添加新表情", Flat = true };
        addExprBtn.Pressed += () => {
            ApplyDataChange(() => charData.Visuals.Expressions["新状态"] = "");
            exprList.AddChild(CreateExpressionRow(charData, "新状态", ""));
        };
        detail.AddChild(addExprBtn);

        // 删除角色
        Button delBtn = new Button { Text = "删除此角色", Modulate = new Color(1, 0.4f, 0.4f) };
        delBtn.Pressed += () => {
            ApplyDataChange(() => CharacterManager.GuestActors.Remove(charData.ActorId));
            itemRoot.QueueFree();
        };
        detail.AddChild(delBtn);

        itemRoot.AddChild(detail);

        // 展开/收起逻辑
        header.Pressed += () => {
            detail.Visible = !detail.Visible;
            header.Text = (detail.Visible ? " v  " : " >  ") + $"[{charData.ActorId}] {charData.DisplayName}";
        };

        // 实时同步
        nameEdit.TextChanged += (txt) => {
            ApplyDataChange(() => charData.DisplayName = txt);
            header.Text = (detail.Visible ? " v  " : " >  ") + $"[{charData.ActorId}] {txt}";
        };
        spriteSelect.ItemSelected += (idx) => {
            string selectedSprite = spriteSelect.GetItemText((int)idx);
            ApplyDataChange(() => charData.Visuals.DefaultSprite = selectedSprite);
        };

        return itemRoot;
    }

    private Control CreateExpressionRow(ActorConfigData charData, string key, string val)
    {
        HBoxContainer row = new HBoxContainer();
        LineEdit keyEdit = new LineEdit { Text = key, CustomMinimumSize = new Vector2(120, 0) };
        OptionButton valSelect = new OptionButton { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        SpriteLibrary.PopulateOptionButton(valSelect, val);
        Button delBtn = new Button { Text = "×", Flat = true };
        string currentKey = key;
        
        row.AddChild(keyEdit);
        row.AddChild(valSelect);
        row.AddChild(delBtn);

        keyEdit.TextChanged += (newKey) => {
            string previousKey = currentKey;
            string selectedSprite = valSelect.GetItemText(valSelect.Selected);
            ApplyDataChange(() => {
                // TextChanged 会在每次输入时触发，必须删除上一次输入产生的键。
                charData.Visuals.Expressions.Remove(previousKey);
                charData.Visuals.Expressions[newKey] = selectedSprite;
            });
            currentKey = newKey;
        };
        valSelect.ItemSelected += (idx) => {
            string selectedSprite = valSelect.GetItemText((int)idx);
            ApplyDataChange(() => charData.Visuals.Expressions[currentKey] = selectedSprite);
        };
        delBtn.Pressed += () => {
            ApplyDataChange(() => charData.Visuals.Expressions.Remove(currentKey));
            row.QueueFree();
        };

        return row;
    }
}
