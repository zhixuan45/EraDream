using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;
using EraDream.Core.Extensions;
using EraDream.Game.Models;

public partial class ExtensionEditorScreen : Control
{
    // ==================== 行为编辑器初始化与加载 ====================

    // 显示行为编辑器主面板，读取并解析 behavior.json 数据
    private void ShowBehaviorEditor(string path)
    {
        _behaviorEditorRoot.Show();
        _currentBehaviorPack = null;

        try {
            if (Godot.FileAccess.FileExists(path)) {
                using var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read);
                string json = file.GetAsText();
                _currentBehaviorPack = string.IsNullOrWhiteSpace(json) ? new() : JsonSerializer.Deserialize<BehaviorPack>(json) ?? new();
            } else {
                _currentBehaviorPack = new();
            }

            RefreshBehaviorUI();
        } catch (Exception ex) {
            GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast($"加载失败: {ex.Message}");
            ShowCodeView(path);
        }
    }

    // 重新刷新三个 Tab 页（规则、道具、交互菜单）的视图内容
    private void RefreshBehaviorUI()
    {
        if (_currentBehaviorPack == null) return;

        // 1. 刷新规则 Tab
        foreach (Node child in _behaviorRulesContainer.GetChildren()) child.QueueFree();
        Button addRuleBtn = new Button { Text = "+ 添加新触发规则", CustomMinimumSize = new Vector2(0, 35) };
        addRuleBtn.Pressed += OnAddBehaviorRulePressed;
        _behaviorRulesContainer.AddChild(addRuleBtn);
        foreach (var rule in _currentBehaviorPack.Rules) {
            _behaviorRulesContainer.AddChild(CreateRuleUI(rule));
        }

        // 2. 刷新道具 Tab
        foreach (Node child in _behaviorItemsContainer.GetChildren()) child.QueueFree();
        Button addItemBtn = new Button { Text = "+ 添加新扩展道具", CustomMinimumSize = new Vector2(0, 35) };
        addItemBtn.Pressed += OnAddBehaviorItemPressed;
        _behaviorItemsContainer.AddChild(addItemBtn);
        foreach (var item in _currentBehaviorPack.Items) {
            _behaviorItemsContainer.AddChild(CreateItemUI(item));
        }

        // 3. 刷新菜单 Tab
        foreach (Node child in _behaviorMenusContainer.GetChildren()) child.QueueFree();
        Button addMenuBtn = new Button { Text = "+ 添加新交互菜单组", CustomMinimumSize = new Vector2(0, 35) };
        addMenuBtn.Pressed += OnAddBehaviorMenuPressed;
        _behaviorMenusContainer.AddChild(addMenuBtn);
        foreach (var menu in _currentBehaviorPack.Menus) {
            _behaviorMenusContainer.AddChild(CreateMenuUI(menu));
        }
    }

    // ==================== 添加项按钮回调 ====================

    // 向行为包添加新的事件触发规则，并刷新界面展示
    private void OnAddBehaviorRulePressed()
    {
        if (_currentBehaviorPack == null) _currentBehaviorPack = new();
        var newRule = new BehaviorRule { Id = $"rule_{_currentBehaviorPack.Rules.Count + 1}", Hook = "OnTraining" };
        _currentBehaviorPack.Rules.Add(newRule);
        RefreshBehaviorUI();
    }

    // 向行为包添加新的道具定义项，并刷新界面展示
    private void OnAddBehaviorItemPressed()
    {
        if (_currentBehaviorPack == null) _currentBehaviorPack = new();
        var newItem = new ItemDefinition { Id = $"item_{_currentBehaviorPack.Items.Count + 1}", Name = "新物品" };
        _currentBehaviorPack.Items.Add(newItem);
        RefreshBehaviorUI();
    }

    // 向行为包添加新的交互菜单组，并刷新界面展示
    private void OnAddBehaviorMenuPressed()
    {
        if (_currentBehaviorPack == null) _currentBehaviorPack = new();
        var newMenu = new UIMenuDefinition { MenuId = $"menu_{_currentBehaviorPack.Menus.Count + 1}" };
        _currentBehaviorPack.Menus.Add(newMenu);
        RefreshBehaviorUI();
    }

    // ==================== 规则 (Rules) 渲染 ====================

    // 创建并配置单个事件触发规则的编辑面板
    private Control CreateRuleUI(BehaviorRule rule)
    {
        var panel = new PanelContainer { CustomMinimumSize = new Vector2(0, 150) };
        var vBox = new VBoxContainer();
        panel.AddChild(vBox);

        // ID 与 挂钩 Hook
        var header = new HBoxContainer();
        vBox.AddChild(header);
        
        header.AddChild(new Label { Text = "规则 ID:" });
        var idEdit = new LineEdit { Text = rule.Id, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        idEdit.TextChanged += (val) => rule.Id = val;
        header.AddChild(idEdit);

        header.AddChild(new Label { Text = " Hook类型:" });
        var hookOption = new OptionButton { CustomMinimumSize = new Vector2(150, 0) };
        string[] hooks = { "OnTraining", "OnOuting", "OnTurnStart", "OnTurnEnd", "OnRaceStart", "OnRaceEnd" };
        foreach (var h in hooks) hookOption.AddItem(h);
        hookOption.Text = rule.Hook;
        hookOption.ItemSelected += (idx) => rule.Hook = hookOption.GetItemText((int)idx);
        header.AddChild(hookOption);

        var btnDel = new Button { Text = "删除", Modulate = Colors.Salmon };
        btnDel.Pressed += () => {
            _currentBehaviorPack.Rules.Remove(rule);
            panel.QueueFree();
        };
        header.AddChild(btnDel);

        // 触发概率与覆盖选项
        var probHBox = new HBoxContainer();
        vBox.AddChild(probHBox);
        probHBox.AddChild(new Label { Text = "触发概率:" });
        var probEdit = new LineEdit { Text = rule.Probability.ToString() };
        probEdit.TextChanged += (val) => {
            if (float.TryParse(val, out float p)) rule.Probability = p;
        };
        probHBox.AddChild(probEdit);

        var overrideCheck = new CheckBox { Text = "Override (强行覆盖已有设定)", ButtonPressed = rule.Override };
        overrideCheck.Toggled += (val) => rule.Override = val;
        probHBox.AddChild(overrideCheck);

        // 绑定动作
        var actionHBox = new HBoxContainer();
        vBox.AddChild(actionHBox);
        actionHBox.AddChild(new Label { Text = "动作类型:" });
        var typeOption = new OptionButton();
        typeOption.AddItem("DetailedStory");
        typeOption.AddItem("BriefStory");
        typeOption.AddItem("ChangeStat");
        typeOption.Text = rule.Action.Type;
        typeOption.ItemSelected += (idx) => rule.Action.Type = typeOption.GetItemText((int)idx);
        actionHBox.AddChild(typeOption);

        actionHBox.AddChild(new Label { Text = " 路径/变更项:" });
        var pathEdit = new LineEdit { Text = rule.Action.Path, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        pathEdit.TextChanged += (val) => rule.Action.Path = val;
        actionHBox.AddChild(pathEdit);

        // 条件配置
        var condTitleHBox = new HBoxContainer();
        vBox.AddChild(condTitleHBox);
        condTitleHBox.AddChild(new Label { Text = "触发条件列表:" });
        var btnAddCond = new Button { Text = "+" };
        condTitleHBox.AddChild(btnAddCond);

        var condVBox = new VBoxContainer();
        vBox.AddChild(condVBox);
        foreach (var cond in rule.Conditions) {
            condVBox.AddChild(CreateConditionUI(cond, rule.Conditions, condVBox));
        }

        btnAddCond.Pressed += () => {
            var newCond = new BehaviorCondition { Property = "Player.Money", Operator = ">=", Value = "100" };
            rule.Conditions.Add(newCond);
            condVBox.AddChild(CreateConditionUI(newCond, rule.Conditions, condVBox));
        };

        return panel;
    }

    // ==================== 道具 (Items) 渲染 ====================

    // 创建并配置道具定义的编辑面板
    private Control CreateItemUI(ItemDefinition item)
    {
        var panel = new PanelContainer { CustomMinimumSize = new Vector2(0, 150) };
        var vBox = new VBoxContainer();
        panel.AddChild(vBox);

        // 第一行：ID, 名称，删除
        var row1 = new HBoxContainer();
        vBox.AddChild(row1);

        row1.AddChild(new Label { Text = "道具 ID:" });
        var idEdit = new LineEdit { Text = item.Id, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        idEdit.TextChanged += (val) => item.Id = val;
        row1.AddChild(idEdit);

        row1.AddChild(new Label { Text = " 道具名称:" });
        var nameEdit = new LineEdit { Text = item.Name, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        nameEdit.TextChanged += (val) => item.Name = val;
        row1.AddChild(nameEdit);

        var btnDel = new Button { Text = "删除", Modulate = Colors.Salmon };
        btnDel.Pressed += () => {
            _currentBehaviorPack.Items.Remove(item);
            panel.QueueFree();
        };
        row1.AddChild(btnDel);

        // 第二行：描述，图标路径
        var row2 = new HBoxContainer();
        vBox.AddChild(row2);

        row2.AddChild(new Label { Text = "描述文本:" });
        var descEdit = new LineEdit { Text = item.Description, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        descEdit.TextChanged += (val) => item.Description = val;
        row2.AddChild(descEdit);

        row2.AddChild(new Label { Text = " 图标路径:" });
        var iconEdit = new LineEdit { Text = item.IconPath, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        iconEdit.TextChanged += (val) => item.IconPath = val;
        row2.AddChild(iconEdit);

        // 第三行：类型，限额，持续回合，价格，覆盖
        var row3 = new HBoxContainer();
        vBox.AddChild(row3);

        row3.AddChild(new Label { Text = "道具类型:" });
        var typeOption = new OptionButton();
        typeOption.AddItem("Consumable");
        typeOption.AddItem("Duration");
        typeOption.AddItem("Passive");
        typeOption.Text = item.Type.ToString();
        typeOption.ItemSelected += (idx) => {
            if (Enum.TryParse<ItemType>(typeOption.GetItemText((int)idx), out var t)) item.Type = t;
        };
        row3.AddChild(typeOption);

        row3.AddChild(new Label { Text = " 最大堆叠:" });
        var maxStackEdit = new LineEdit { Text = item.MaxStack.ToString(), CustomMinimumSize = new Vector2(60, 0) };
        maxStackEdit.TextChanged += (val) => {
            if (int.TryParse(val, out int s)) item.MaxStack = s;
        };
        row3.AddChild(maxStackEdit);

        row3.AddChild(new Label { Text = " 持续回合:" });
        var durationEdit = new LineEdit { Text = item.DurationTurns.ToString(), CustomMinimumSize = new Vector2(60, 0) };
        durationEdit.TextChanged += (val) => {
            if (int.TryParse(val, out int d)) item.DurationTurns = d;
        };
        row3.AddChild(durationEdit);

        row3.AddChild(new Label { Text = " 商店售价:" });
        var priceEdit = new LineEdit { Text = item.Price.ToString(), CustomMinimumSize = new Vector2(70, 0) };
        priceEdit.TextChanged += (val) => {
            if (int.TryParse(val, out int p)) item.Price = p;
        };
        row3.AddChild(priceEdit);

        var overrideCheck = new CheckBox { Text = "Override", ButtonPressed = item.Override };
        overrideCheck.Toggled += (val) => item.Override = val;
        row3.AddChild(overrideCheck);

        return panel;
    }

    // ==================== 菜单 (Menus) 渲染 ====================

    // 创建并配置整个交互菜单组的编辑容器
    private Control CreateMenuUI(UIMenuDefinition menu)
    {
        var panel = new PanelContainer { CustomMinimumSize = new Vector2(0, 180) };
        var vBox = new VBoxContainer();
        panel.AddChild(vBox);

        // 菜单组头部
        var header = new HBoxContainer();
        vBox.AddChild(header);

        header.AddChild(new Label { Text = "菜单容器 ID (如: SimulationMenu):" });
        var menuIdEdit = new LineEdit { Text = menu.MenuId, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        menuIdEdit.TextChanged += (val) => menu.MenuId = val;
        header.AddChild(menuIdEdit);

        var btnDelMenu = new Button { Text = "删除菜单组", Modulate = Colors.Salmon };
        btnDelMenu.Pressed += () => {
            _currentBehaviorPack.Menus.Remove(menu);
            panel.QueueFree();
        };
        header.AddChild(btnDelMenu);

        // 选项集合
        var optionsVBox = new VBoxContainer();
        vBox.AddChild(optionsVBox);

        Button addOptionBtn = new Button { Text = "+ 添加交互选项 (UIOption)", Flat = true };
        vBox.AddChild(addOptionBtn);

        foreach (var opt in menu.Options) {
            optionsVBox.AddChild(CreateOptionUI(opt, menu, optionsVBox));
        }

        addOptionBtn.Pressed += () => {
            var newOpt = new UIOption { Id = $"option_{menu.Options.Count + 1}", Name = "新菜单选项" };
            menu.Options.Add(newOpt);
            optionsVBox.AddChild(CreateOptionUI(newOpt, menu, optionsVBox));
        };

        return panel;
    }

    // 创建菜单组内单个交互选项的编辑卡片
    private Control CreateOptionUI(UIOption option, UIMenuDefinition menu, VBoxContainer parentVBox)
    {
        var card = new PanelContainer { SelfModulate = new Color(0.9f, 0.9f, 0.9f), CustomMinimumSize = new Vector2(0, 120) };
        var vBox = new VBoxContainer();
        card.AddChild(vBox);

        // 头部字段：Id，Name，删除
        var row1 = new HBoxContainer();
        vBox.AddChild(row1);

        row1.AddChild(new Label { Text = "选项 ID:" });
        var idEdit = new LineEdit { Text = option.Id, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        idEdit.TextChanged += (val) => option.Id = val;
        row1.AddChild(idEdit);

        row1.AddChild(new Label { Text = " 显示名称:" });
        var nameEdit = new LineEdit { Text = option.Name, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        nameEdit.TextChanged += (val) => option.Name = val;
        row1.AddChild(nameEdit);

        var btnDel = new Button { Text = "x", Modulate = Colors.Salmon };
        btnDel.Pressed += () => {
            menu.Options.Remove(option);
            card.QueueFree();
        };
        row1.AddChild(btnDel);

        // 第二行：描述，图标，覆盖
        var row2 = new HBoxContainer();
        vBox.AddChild(row2);

        row2.AddChild(new Label { Text = "悬浮描述:" });
        var descEdit = new LineEdit { Text = option.Description, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        descEdit.TextChanged += (val) => option.Description = val;
        row2.AddChild(descEdit);

        row2.AddChild(new Label { Text = " 按钮图标:" });
        var iconEdit = new LineEdit { Text = option.Icon, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        iconEdit.TextChanged += (val) => option.Icon = val;
        row2.AddChild(iconEdit);

        var overrideCheck = new CheckBox { Text = "Override", ButtonPressed = option.Override };
        overrideCheck.Toggled += (val) => option.Override = val;
        row2.AddChild(overrideCheck);

        // 第三行：绑定动作 (Action)
        var row3 = new HBoxContainer();
        vBox.AddChild(row3);

        row3.AddChild(new Label { Text = "触发动作:" });
        var actionType = new OptionButton();
        actionType.AddItem("DetailedStory");
        actionType.AddItem("BriefStory");
        actionType.AddItem("ChangeStat");
        actionType.Text = option.Action.Type;
        actionType.ItemSelected += (idx) => option.Action.Type = actionType.GetItemText((int)idx);
        row3.AddChild(actionType);

        row3.AddChild(new Label { Text = " 路径/变更项:" });
        var actionPath = new LineEdit { Text = option.Action.Path, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        actionPath.TextChanged += (val) => option.Action.Path = val;
        row3.AddChild(actionPath);

        // 第四行：显示条件 (Conditions)
        var condTitleHBox = new HBoxContainer();
        vBox.AddChild(condTitleHBox);
        condTitleHBox.AddChild(new Label { Text = "显示限制条件:" });
        var btnAddCond = new Button { Text = "+" };
        condTitleHBox.AddChild(btnAddCond);

        var condVBox = new VBoxContainer();
        vBox.AddChild(condVBox);
        foreach (var cond in option.Conditions) {
            condVBox.AddChild(CreateConditionUI(cond, option.Conditions, condVBox));
        }

        btnAddCond.Pressed += () => {
            var newCond = new BehaviorCondition { Property = "Player.Money", Operator = ">=", Value = "500" };
            option.Conditions.Add(newCond);
            condVBox.AddChild(CreateConditionUI(newCond, option.Conditions, condVBox));
        };

        return card;
    }

    // ==================== 条件 (Conditions) 通用渲染 ====================

    // 创建并渲染单个触发条件行的 UI 控件，可被 Rules 和 UI Options 完美复用
    private Control CreateConditionUI(BehaviorCondition cond, List<BehaviorCondition> conditionsList, VBoxContainer parentVBox)
    {
        var hbox = new HBoxContainer();
        var propEdit = new LineEdit { Text = cond.Property, PlaceholderText = "Property", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        propEdit.TextChanged += (val) => cond.Property = val;
        hbox.AddChild(propEdit);

        var opOption = new OptionButton();
        string[] ops = { "==", "!=", ">", "<", ">=", "<=" };
        foreach (var op in ops) opOption.AddItem(op);
        opOption.Text = cond.Operator;
        opOption.ItemSelected += (idx) => cond.Operator = opOption.GetItemText((int)idx);
        hbox.AddChild(opOption);

        var valEdit = new LineEdit { Text = cond.Value, PlaceholderText = "Value" };
        valEdit.TextChanged += (val) => cond.Value = val;
        hbox.AddChild(valEdit);

        var btnDelCond = new Button { Text = "x", Modulate = Colors.Salmon };
        btnDelCond.Pressed += () => {
            conditionsList.Remove(cond);
            hbox.QueueFree();
        };
        hbox.AddChild(btnDelCond);

        return hbox;
    }

    // ==================== 行为文件保存 ====================

    // 保存行为编辑器中的当前修改内容到 behavior.json 文件
    private void OnSaveBehaviorPressed()
    {
        if (string.IsNullOrEmpty(_currentEditingFilePath) || _currentBehaviorPack == null) return;

        string absolutePath = System.IO.Path.GetFullPath(ProjectSettings.GlobalizePath(_currentEditingFilePath));
        if (!IsPathWithinProject(absolutePath)) {
            GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast("保存失败: 文件不在项目内！");
            return;
        }

        try {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(_currentBehaviorPack, options);
            using var file = Godot.FileAccess.Open(_currentEditingFilePath, Godot.FileAccess.ModeFlags.Write);
            file.StoreString(json);
            GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast("行为包已保存！");
        } catch (Exception ex) {
            GetNodeOrNull<ErrorNotifier>("/root/ErrorNotifier")?.ShowToast($"保存失败: {ex.Message}");
        }
    }
}
