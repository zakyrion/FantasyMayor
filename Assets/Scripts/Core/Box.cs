using System;

namespace Core
{
    public readonly struct Box<T>
    {
        public T Value => _handle != null
            ? _handle.Value
            : throw new InvalidOperationException($"Box<{typeof(T).Name}> is empty.");
        public bool Exist => _handle != null && _handle.Exist;

        private readonly BoxHandle<T> _handle;

        private Box(BoxHandle<T> handle)
        {
            _handle = handle ?? throw new ArgumentNullException(nameof(handle));
        }

        public void Dispose()
        {
            _handle?.Dispose();
        }

        public static Box<T> Wrap(T value, Action<T> dispose)
        {
            return new Box<T>(new BoxHandle<T>(value, dispose));
        }

        public static Box<T> Empty()
        {
            return new Box<T>(BoxHandle<T>.CreateEmpty());
        }
    }

    internal sealed class BoxHandle<T>
    {
        public readonly T Value;

        private readonly Action<T> _dispose;
        private bool _disposed;
        public bool Exist { get; private set; }

        internal BoxHandle(T value, Action<T> dispose)
        {
            Value = value;
            _dispose = dispose;
            Exist = true;
        }

        private BoxHandle()
        {
            Exist = false;
            _disposed = true;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            Exist = false;
            _dispose?.Invoke(Value);
        }

        internal static BoxHandle<T> CreateEmpty() => new BoxHandle<T>();
    }
}
