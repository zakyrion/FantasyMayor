using System;
using System.Collections.Generic;

public class Disposer : IDisposable
{
    private readonly Action _disposeAction;

    public Disposer(Action disposeAction)
    {
        _disposeAction = disposeAction;
    }

    public void Dispose()
    {
        _disposeAction?.Invoke();
    }
}

public class DisposeHandler
{
    private readonly List<IDisposable> _disposables = new();

    public void AddDisposable(IDisposable disposable)
    {
        _disposables.Add(disposable);
    }

    public void AddDisposable(Disposer disposer)
    {
        _disposables.Add(disposer);
    }

    public void AddDisposable(Action disposeAction)
    {
        _disposables.Add(new Disposer(disposeAction));
    }

    public void DisposeAll()
    {
        foreach (var disposable in _disposables) disposable.Dispose();
        _disposables.Clear();
    }
}