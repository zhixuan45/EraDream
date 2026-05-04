using Godot;
using System;
using System.Collections.Generic;
using umaEraArchive.Game;
using UmaEraArchive.Editor.Nodes;
using System.Text.Json;

namespace umaEraArchive.Tests;

public partial class TurnEventTest : Node
{
    public override async void _Ready()
    {
        GD.Print("\n[Test] === Starting Turn Event Tests ===\n");

        try
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

            VerifyEventTrigger();

            GD.Print("\n[Test] === All Turn Event Tests Passed! ===\n");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"\n[Test] !!! Turn Event Test Failed: {ex.Message}\n{ex.StackTrace}");
        }
    }

    private void VerifyEventTrigger()
    {
        GD.Print("[Test] Verifying Event Trigger...");

        var gm = GameManager.Instance;

        // 创建一个测试剧本
        string testScenarioPath = "user://test_scenario.json";
        var nodes = new List<BaseNodeData>();

        var startNode = new StartNodeData
        {
            Id = "start_1",
            TriggerCondition = "Affection >= 50",
            Timing = TriggerTiming.TurnStart
        };
        nodes.Add(startNode);

        string json = JsonSerializer.Serialize(nodes, new JsonSerializerOptions { WriteIndented = true });
        using (var file = FileAccess.Open(testScenarioPath, FileAccess.ModeFlags.Write))
        {
            file.StoreString(json);
        }

        // 初始化游戏并加载剧本
        gm.StartNewGame(new List<string> { testScenarioPath });
        var state = gm.CurrentState;

        // 测试条件不满足
        state.Uma.Affection = 10;
        bool triggered = gm.Events.CheckAndTriggerStory(TriggerTiming.TurnStart, state);
        if (triggered) throw new Exception("Story should not trigger when Affection is < 50");

        // 测试条件满足
        state.Uma.Affection = 60;
        triggered = gm.Events.CheckAndTriggerStory(TriggerTiming.TurnStart, state);

        // 验证触发逻辑：因为 TriggerStory 会调用 ChangeSceneToFile，在测试中这可能导致场景切换
        // 为了避免真实切换场景，我们可以只验证 CheckAndTriggerStory 返回 true

        if (!triggered) throw new Exception("Story should trigger when Affection is >= 50");

        GD.Print("[Test] Event Trigger OK.");
    }
}
