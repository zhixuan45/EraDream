using System.Collections.Generic;

namespace EraDream.Core
{
    public interface ICommand
    {
        void Execute();
        void Undo();
        string Description { get; }
    }

    // 撤销/重做命令管理器
    public class CommandHistory
    {
        private readonly Stack<ICommand> _undoStack = new Stack<ICommand>();
        private readonly Stack<ICommand> _redoStack = new Stack<ICommand>();
        private readonly int _maxHistory;

        public CommandHistory(int maxHistory = 50)
        {
            _maxHistory = maxHistory;
        }

        public void ExecuteCommand(ICommand cmd)
        {
            cmd.Execute();
            _undoStack.Push(cmd);
            _redoStack.Clear();
        }

        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;

        public bool Undo()
        {
            if (!CanUndo) return false;
            var cmd = _undoStack.Pop();
            cmd.Undo();
            _redoStack.Push(cmd);
            return true;
        }

        public bool Redo()
        {
            if (!CanRedo) return false;
            var cmd = _redoStack.Pop();
            cmd.Execute();
            _undoStack.Push(cmd);
            return true;
        }

        public void Clear()
        {
            _undoStack.Clear();
            _redoStack.Clear();
        }
    }
}
