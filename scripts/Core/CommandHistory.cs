using System;
using System.Collections.Generic;

namespace UmaEraArchive.Core
{
    /// <summary>
    /// 纯 C# 实现的撤销/重做命令栈
    /// 替代 Godot 的 UndoRedo（其 AddDoMethod 不兼容 C# lambda）
    /// </summary>
    public class CommandHistory
    {
        // 每个命令由"执行"和"撤销"两个 Action 组成
        private record UndoableCommand(Action DoAction, Action UndoAction);

        private readonly Stack<UndoableCommand> _undoStack = new();
        private readonly Stack<UndoableCommand> _redoStack = new();

        // 批量操作缓存（用于一次性提交多步操作）
        private List<UndoableCommand> _pendingBatch = null;

        /// <summary>
        /// 开始一个批量操作（多个 Do/Undo 合并为一次撤销）
        /// </summary>
        public void BeginBatch()
        {
            _pendingBatch = new List<UndoableCommand>();
        }

        /// <summary>
        /// 向当前批量操作中添加一组 do/undo
        /// </summary>
        public void AddBatchStep(Action doAction, Action undoAction)
        {
            _pendingBatch?.Add(new UndoableCommand(doAction, undoAction));
        }

        /// <summary>
        /// 提交并立即执行当前批量操作
        /// </summary>
        public void CommitBatch()
        {
            if (_pendingBatch == null || _pendingBatch.Count == 0) return;

            var batch = _pendingBatch;
            _pendingBatch = null;

            // 合并为一条复合命令
            Action batchDo = () => { foreach (var c in batch) c.DoAction(); };
            Action batchUndo = () => { for (int i = batch.Count - 1; i >= 0; i--) batch[i].UndoAction(); };

            _undoStack.Push(new UndoableCommand(batchDo, batchUndo));
            _redoStack.Clear();

            batchDo();
        }

        /// <summary>
        /// 执行一个可撤销的操作
        /// </summary>
        public void Execute(Action doAction, Action undoAction)
        {
            var cmd = new UndoableCommand(doAction, undoAction);
            _undoStack.Push(cmd);
            _redoStack.Clear();
            doAction();
        }

        /// <summary>
        /// 撤销上一步操作
        /// </summary>
        public void Undo()
        {
            if (_undoStack.Count == 0) return;
            var cmd = _undoStack.Pop();
            cmd.UndoAction();
            _redoStack.Push(cmd);
        }

        /// <summary>
        /// 重做上一步被撤销的操作
        /// </summary>
        public void Redo()
        {
            if (_redoStack.Count == 0) return;
            var cmd = _redoStack.Pop();
            cmd.DoAction();
            _undoStack.Push(cmd);
        }

        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;
    }
}
