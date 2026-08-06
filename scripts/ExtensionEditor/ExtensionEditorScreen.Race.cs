using Godot;
using System;
using System.Collections.Generic;
using EraDream.Core.Extensions;

public partial class ExtensionEditorScreen : Control
{
    // ==================== 赛马赛事编辑器交互逻辑 ====================

    // 向行为包中添加一个新的赛马赛事定义，并重绘 UI 列表
    private void OnAddBehaviorRacePressed()
    {
        if (_currentBehaviorPack == null) _currentBehaviorPack = new();
        var newRace = new RaceDefinition { 
            Id = $"race_{_currentBehaviorPack.Races.Count + 1}", 
            Name = "新比赛" 
        };
        _currentBehaviorPack.Races.Add(newRace);
        RefreshBehaviorUI();
    }

    // 创建并配置单个赛马赛事定义的编辑卡片面板
    private Control CreateRaceUI(RaceDefinition race)
    {
        var panel = new PanelContainer { CustomMinimumSize = new Vector2(0, 150) };
        var vBox = new VBoxContainer();
        panel.AddChild(vBox);

        // 第一行：ID, 名称，删除
        var row1 = new HBoxContainer();
        vBox.AddChild(row1);

        row1.AddChild(new Label { Text = "赛事 ID:" });
        var idEdit = new LineEdit { Text = race.Id, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        idEdit.TextChanged += (val) => race.Id = val;
        row1.AddChild(idEdit);

        row1.AddChild(new Label { Text = " 赛事名称:" });
        var nameEdit = new LineEdit { Text = race.Name, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        nameEdit.TextChanged += (val) => race.Name = val;
        row1.AddChild(nameEdit);

        var btnDel = new Button { Text = "删除", Modulate = Colors.Salmon };
        btnDel.Pressed += () => {
            _currentBehaviorPack.Races.Remove(race);
            panel.QueueFree();
        };
        row1.AddChild(btnDel);

        // 第二行：描述介绍
        var row2 = new HBoxContainer();
        vBox.AddChild(row2);

        row2.AddChild(new Label { Text = "赛事描述:" });
        var descEdit = new LineEdit { Text = race.Description, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        descEdit.TextChanged += (val) => race.Description = val;
        row2.AddChild(descEdit);

        // 第三行：举办回合，属性门槛门限，奖励绑定
        var row3 = new HBoxContainer();
        vBox.AddChild(row3);

        row3.AddChild(new Label { Text = "举办回合:" });
        var turnEdit = new LineEdit { Text = race.Turn.ToString(), CustomMinimumSize = new Vector2(60, 0) };
        turnEdit.TextChanged += (val) => {
            if (int.TryParse(val, out int t)) race.Turn = t;
        };
        row3.AddChild(turnEdit);

        row3.AddChild(new Label { Text = " 最低速度要求:" });
        var speedEdit = new LineEdit { Text = race.MinSpeed.ToString(), CustomMinimumSize = new Vector2(70, 0) };
        speedEdit.TextChanged += (val) => {
            if (int.TryParse(val, out int s)) race.MinSpeed = s;
        };
        row3.AddChild(speedEdit);

        row3.AddChild(new Label { Text = " 奖励属性:" });
        var rewardStatEdit = new LineEdit { Text = race.RewardStat, PlaceholderText = "如: Uma.Speed", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        rewardStatEdit.TextChanged += (val) => race.RewardStat = val;
        row3.AddChild(rewardStatEdit);

        row3.AddChild(new Label { Text = " 奖励数值:" });
        var rewardValEdit = new LineEdit { Text = race.RewardValue.ToString(), CustomMinimumSize = new Vector2(60, 0) };
        rewardValEdit.TextChanged += (val) => {
            if (int.TryParse(val, out int v)) race.RewardValue = v;
        };
        row3.AddChild(rewardValEdit);

        var overrideCheck = new CheckBox { Text = "Override", ButtonPressed = race.Override };
        overrideCheck.Toggled += (val) => race.Override = val;
        row3.AddChild(overrideCheck);

        return panel;
    }
}
