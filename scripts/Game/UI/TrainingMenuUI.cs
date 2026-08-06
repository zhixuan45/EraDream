using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using EraDream.Game;
using EraDream.Core.Extensions;

namespace EraDream.Game.UI
{
    /// <summary>
    /// 独立的训练菜单 UI 场景控制逻辑
    /// </summary>
    public partial class TrainingMenuUI : Control
    {
        [Signal] public delegate void TrainingSelectedEventHandler(int type);
        [Signal] public delegate void CustomTrainingSelectedEventHandler(string trainingId);
        [Signal] public delegate void DynamicOptionSelectedEventHandler(string menuId, string optionId);
        [Signal] public delegate void CloseRequestedEventHandler();

        private VBoxContainer _listContainer;
        private AnimationPlayer _animPlayer;
        private Button _btnClose;

        public override void _Ready()
        {
            _listContainer = GetNode<VBoxContainer>("PanelContainer/MarginContainer/VBoxContainer/ScrollContainer/ListContainer");
            _animPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
            _btnClose = GetNode<Button>("PanelContainer/MarginContainer/VBoxContainer/Header/BtnClose");

            _btnClose.Pressed += () => EmitSignal(SignalName.CloseRequested);

            SetupTrainingButtons();
            
            // 播放入场动画
            _animPlayer.Play("fade_in");
        }

        private void SetupTrainingButtons()
        {
            // 清理旧项
            foreach (Node child in _listContainer.GetChildren()) child.QueueFree();

            // 1. 动态加载所有注册的训练条目（包含内置默认五大训练）
            if (BehaviorRegistry.Instance != null)
            {
                var allTrainings = BehaviorRegistry.Instance.GetAllTrainings();
                var orderedTrainings = allTrainings.OrderBy(t => t.Id switch {
                    "Speed" => 1,
                    "Stamina" => 2,
                    "Power" => 3,
                    "Guts" => 4,
                    "Intelligence" => 5,
                    _ => 100
                }).ToList();

                foreach (var training in orderedTrainings)
                {
                    AddTrainingItem(training.Name, training.Description, training.Id, "res://icon.svg");
                }
            }

            // 2. 加载来自 BehaviorRegistry 的动态菜单项
            if (BehaviorRegistry.Instance != null && GameManager.Instance?.CurrentState != null)
            {
                var options = BehaviorRegistry.Instance.GetValidOptions("Training", GameManager.Instance.CurrentState);
                foreach (var option in options)
                {
                    AddDynamicItem(option, "Training");
                }
            }
        }

        private void AddTrainingItem(string title, string desc, string trainingId, string iconPath)
        {
            var btn = CreateBaseButton(title, desc, iconPath);
            btn.Pressed += () => {
                EmitSignal(SignalName.CustomTrainingSelected, trainingId);
                // 向后兼容旧版本的整数信号
                if (Enum.TryParse<TrainingType>(trainingId, true, out var tType))
                {
                    EmitSignal(SignalName.TrainingSelected, (int)tType);
                }
                EmitSignal(SignalName.CloseRequested);
            };
            _listContainer.AddChild(btn);
        }

        private void AddDynamicItem(UIOption option, string menuId)
        {
            var btn = CreateBaseButton(option.Name, option.Description, option.Icon);
            btn.Pressed += () => {
                EmitSignal(SignalName.DynamicOptionSelected, menuId, option.Id);
                EmitSignal(SignalName.CloseRequested);
            };
            _listContainer.AddChild(btn);
        }

        private Button CreateBaseButton(string title, string desc, string iconPath)
        {
            var btn = new Button {
                CustomMinimumSize = new Vector2(0, 80),
                MouseDefaultCursorShape = CursorShape.PointingHand
            };
            
            var hbox = new HBoxContainer();
            hbox.SetAnchorsPreset(LayoutPreset.FullRect);
            hbox.MouseFilter = MouseFilterEnum.Ignore;
            btn.AddChild(hbox);

            var icon = new TextureRect {
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                CustomMinimumSize = new Vector2(64, 64)
            };

            if (!string.IsNullOrEmpty(iconPath) && ResourceLoader.Exists(iconPath))
            {
                icon.Texture = GD.Load<Texture2D>(iconPath);
            }
            else
            {
                icon.Texture = GD.Load<Texture2D>("res://icon.svg");
            }
            hbox.AddChild(icon);

            var vbox = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, Alignment = BoxContainer.AlignmentMode.Center };
            hbox.AddChild(vbox);

            vbox.AddChild(new Label { Text = title, ThemeTypeVariation = "HeaderLarge" });
            vbox.AddChild(new Label { Text = desc, Modulate = new Color(0.8f, 0.8f, 0.8f) });

            return btn;
        }

        public void Close()
        {
            _animPlayer.PlayBackwards("fade_in");
            _animPlayer.AnimationFinished += (animName) => QueueFree();
        }
    }
}
