using System.Threading;
using UnityEngine;

/// <summary>
///     Global application lifecycle monitor. Provides a singleton entry point and a shared
///     <see cref="CancellationToken" /> that is cancelled when the application exits, allowing
///     all long-running tasks to observe a single stop signal.
/// </summary>
public class StatusMonitor : MonoBehaviour
{
    /// <summary>Gets the single active instance of <see cref="StatusMonitor" />.</summary>
    public static StatusMonitor Instance { get; private set; }

    /// <summary>Gets the source used to issue the global cancellation signal.</summary>
    public static CancellationTokenSource CancellationTokenSource { get; private set; }

    /// <summary>
    ///     Shorthand for <c>CancellationTokenSource.Token</c>. Pass this to any async task or
    ///     UniTask that should stop when the application shuts down.
    /// </summary>
    public static CancellationToken Token => CancellationTokenSource.Token;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        CancellationTokenSource = new CancellationTokenSource();
    }

    /// <summary>
    ///     Signals cancellation so all active tasks can observe it and stop gracefully before
    ///     the object is destroyed. Fires before <see cref="OnDestroy" />, giving Unity at least
    ///     one update cycle for tasks to react.
    /// </summary>
    private void OnApplicationQuit()
    {
        if (Instance != this)
            return;

        CancellationTokenSource.Cancel();
    }

    /// <summary>
    ///     Disposes the <see cref="CancellationTokenSource" /> after tasks have had a chance to
    ///     observe the cancellation signal issued in <see cref="OnApplicationQuit" />.
    /// </summary>
    private void OnDestroy()
    {
        if (Instance != this)
            return;

        CancellationTokenSource.Dispose();
        Instance = null;
    }
}
