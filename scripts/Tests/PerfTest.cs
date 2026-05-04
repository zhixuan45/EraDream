using Godot;
using System;
using System.Diagnostics;
using umaEraArchive.Game;
using umaEraArchive.Game.Models;
using UmaEraArchive.Core.Extensions;

namespace umaEraArchive.Tests;

public partial class PerfTest : SceneTree
{
    public override void _Initialize()
    {
        GD.Print("Starting PerfTest...");

        var registryNode = new BehaviorRegistry();
        var root = this.Root;
        root.AddChild(registryNode);

        // Load benchmark pack
        registryNode.LoadBehaviorPack("benchmark.behavior.json");

        var module = new InventoryModule();
        root.AddChild(module);

        var state = new GameState();
        // Add 10000 items to inventory
        for (int i = 0; i < 10000; i++)
        {
            state.Inventory.Items[$"item_{i}"] = 1;
        }

        var sw = new Stopwatch();

        // warmup
        for (int i = 0; i < 10; i++) {
            module.UpdateTurnEffects(state);
        }

        sw.Start();
        for (int i = 0; i < 100; i++)
        {
            module.UpdateTurnEffects(state);
        }
        sw.Stop();

        GD.Print($"Baseline Time for 100 iterations: {sw.ElapsedMilliseconds} ms");

        Quit();
    }
}
