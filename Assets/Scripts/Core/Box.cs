using System;

namespace Core
{
    public struct Box<T>
    {
        public T Value { get; init; }
        public bool Exist { get; private set; }

        private readonly Action<T> _dispose;
        private bool _disposed;

        private Box(T value, Action<T> dispose)
        {
            Value = value;
            Exist = true;
            _dispose = dispose;
            _disposed = false;
        }

        public T Get()
        {
            return Value;
        }

        public void Dispose()
        {
            if (Exist && !_disposed)
            {
                Exist = false;
                _dispose?.Invoke(Value);
                _disposed = true;
            }
        }

        public static Box<T> Empty()
        {
            return new Box<T>
            {
                Exist = false,
                Value = default
            };
        }

        public static Box<T> Wrap(T value, Action<T> dispose)
        {
            return new Box<T>(value, dispose);
        }
    }
}
