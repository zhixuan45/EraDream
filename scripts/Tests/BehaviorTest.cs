using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using UmaEraArchive.Core.Extensions;
using umaEraArchive.Game;

namespace UmaEraArchive.Tests;

public partial class BehaviorTest : Node
{
    public override void _Ready()
    {
        GD.Print("[BehaviorTest] Starting tests...");
        TestBehaviorLoading();
        TestConditionEvaluation();
        TestOutingHook();
        GD.Print("[BehaviorTest] Tests finished.");
    }

    private void TestBehaviorLoading()
    {
        string testPath = "user://test_behavior.json";
        string content = @"
        {
            ""rules"": [
                {
                    ""id"": ""test_rule"",
                    ""hook"": ""OnTest"",
                    ""conditions"": [
                        { ""property"": ""Player.Money"", ""operator"": "">="", ""value"": ""100"" }
                    ],
                    ""probability"": 1.0,
                    ""action"": {
                        ""type"": ""BriefStory"",
                        ""path"": ""test_story""
                    }
                }
            ]
        }";
        
        File.WriteAllText(ProjectSettings.GlobalizePath(testPath), content);
        BehaviorRegistry.Instance.LoadBehaviorPack(testPath);
        GD.Print("[BehaviorTest] Load test completed.");
    }

    private void TestConditionEvaluation()
    {
        var state = new GameState();
        state.Player.Money = 200;
        
        GD.Print("[BehaviorTest] Triggering OnTest hook with Money=200...");
        BehaviorRegistry.Instance.TriggerHook("OnTest", state);
        
        state.Player.Money = 50;
        GD.Print("[BehaviorTest] Triggering OnTest hook with Money=50 (should NOT trigger)...");
        BehaviorRegistry.Instance.TriggerHook("OnTest", state);
    }

    private void TestOutingHook()
    {
        var outing = new OutingModule();
        AddChild(outing);
        
        var state = new GameState();
        state.Player.Money = 1000;
        state.Player.Stamina = 100;
        state.Player.Energy = 100;
        
        GD.Print("[BehaviorTest] Executing outing...");
        outing.ExecuteOuting(state);
    }
}
