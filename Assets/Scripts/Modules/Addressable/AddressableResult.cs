using System;

namespace Modules.Addressable
{
    public struct AddressableResult<T> where T : class
    {
        public AddressableStatus Status { get; private set; }
        public T Value { get; }
        private readonly Action<T> _dispose;
        private readonly bool _disposed;

        private AddressableResult(T val, Action<T> dispose)
        {
            Value = val;
            Status = AddressableStatus.Success;
            _dispose = dispose;
            _disposed = false;
        }

        private AddressableResult(AddressableStatus status)
        {
            Value = null;
            Status = status;
            _dispose = null;
            _disposed = false;
        }

        public void Dispose()
        {
            if (Status != AddressableStatus.Success || _disposed)
                return;

            Status = AddressableStatus.Disposed;
            _dispose?.Invoke(Value);
        }

        public static AddressableResult<T> Empty(AddressableStatus status)
        {
            return new AddressableResult<T>(status);
        }

        public static AddressableResult<T> Cancelled()
        {
            return new AddressableResult<T>(AddressableStatus.Cancelled);
        }

        public static AddressableResult<T> Success(T value, Action<T> dispose)
        {
            return new AddressableResult<T>(value, dispose);
        }
    }
}
