using Godot;
using System;
using UmaEraArchive.Core;

namespace umaEraArchive.Tests;

/// <summary>
/// CommandHistory 撤销/重做系统自动化测试
/// </summary>
public partial class CommandHistoryTest : Node
{
    public override async void _Ready()
    {
        GD.Print("\n[Test] === Starting CommandHistory Tests ===\n");

        try
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

            VerifyExecuteUndoRedo();
            VerifyRedoStackClearing();
            VerifyBatchOperations();
            VerifyEmptyStackBehavior();

            GD.Print("\n[Test] === CommandHistory Tests Passed! ===\n");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"\n[Test] !!! CommandHistory Test Failed: {ex.Message}\n{ex.StackTrace}");
        }
    }

    private void VerifyExecuteUndoRedo()
    {
        GD.Print("[Test] Verifying Execute/Undo/Redo...");
        var history = new CommandHistory();
        int value = 0;

        history.Execute(() => value = 1, () => value = 0);
        if (value != 1) throw new Exception("Execute failed to change value.");
        if (!history.CanUndo) throw new Exception("CanUndo should be true after execute.");

        history.Undo();
        if (value != 0) throw new Exception("Undo failed to revert value.");
        if (!history.CanRedo) throw new Exception("CanRedo should be true after undo.");

        history.Redo();
        if (value != 1) throw new Exception("Redo failed to re-apply value.");
        if (!history.CanUndo) throw new Exception("CanUndo should be true after redo.");

        GD.Print("[Test] Execute/Undo/Redo OK.");
    }

    private void VerifyRedoStackClearing()
    {
        GD.Print("[Test] Verifying Redo Stack Clearing...");
        var history = new CommandHistory();
        int value = 0;

        history.Execute(() => value = 1, () => value = 0);
        history.Undo();
        if (!history.CanRedo) throw new Exception("CanRedo should be true after undo.");

        history.Execute(() => value = 2, () => value = 1);
        if (history.CanRedo) throw new Exception("Redo stack should be cleared after new Execute.");

        GD.Print("[Test] Redo Stack Clearing OK.");
    }

    private void VerifyBatchOperations()
    {
        GD.Print("[Test] Verifying Batch Operations...");
        var history = new CommandHistory();
        int value1 = 0;
        int value2 = 0;

        history.BeginBatch();
        history.AddBatchStep(() => value1 = 1, () => value1 = 0);
        history.AddBatchStep(() => value2 = 1, () => value2 = 0);

        if (value1 != 0 || value2 != 0) throw new Exception("Batch steps should not execute until commit.");

        history.CommitBatch();
        if (value1 != 1 || value2 != 1) throw new Exception("CommitBatch failed to execute steps.");
        if (!history.CanUndo) throw new Exception("CanUndo should be true after CommitBatch.");

        history.Undo();
        if (value1 != 0 || value2 != 0) throw new Exception("Undo failed to revert batch steps.");

        history.Redo();
        if (value1 != 1 || value2 != 1) throw new Exception("Redo failed to re-apply batch steps.");

        GD.Print("[Test] Batch Operations OK.");
    }

    private void VerifyEmptyStackBehavior()
    {
        GD.Print("[Test] Verifying Empty Stack Behavior...");
        var history = new CommandHistory();

        // Should not throw
        history.Undo();
        history.Redo();

        if (history.CanUndo || history.CanRedo) throw new Exception("Stacks should be empty.");

        GD.Print("[Test] Empty Stack Behavior OK.");
    }
}
