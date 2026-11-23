using System;
using System.Collections.Generic;

namespace Compis.App.Services;

public sealed class ActionHistory
{
    private readonly Stack<Action> _undoActions = new();

    public bool CanUndo => _undoActions.Count > 0;

    public event Action? Changed;

    public void Push(Action undoAction)
    {
        ArgumentNullException.ThrowIfNull(undoAction);
        _undoActions.Push(undoAction);
        Changed?.Invoke();
    }

    public bool Undo()
    {
        if (_undoActions.Count == 0)
            return false;

        Action undoAction = _undoActions.Pop();
        undoAction();
        Changed?.Invoke();
        return true;
    }

    public void Clear()
    {
        _undoActions.Clear();
        Changed?.Invoke();
    }
}
