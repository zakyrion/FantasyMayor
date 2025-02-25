using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class DisposedMono : MonoBehaviour
{
    private readonly List<IDisposable> _disposables = new();

    protected virtual void OnDestroy()
    {
        foreach (var disposable in _disposables)
        {
            disposable.Dispose();
        }
    }

    protected void AddDisposable(IDisposable disposable)
    {
        _disposables.Add(disposable);
    }

    protected void AddDisposable(Action disposeAction)
    {
        _disposables.Add(new Disposer(disposeAction));
    }
}