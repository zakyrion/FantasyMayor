using System;
using Core;

namespace Modules.Addressable
{
    public struct AddressableResult<T> where T : class
    {
        public AddressableStatus Status { get; private set; }
        public Box<T> Box { get; }

        private AddressableResult(T val, Action<T> dispose)
        {
            Box = Box<T>.Wrap(val, dispose);
            Status = AddressableStatus.Success;

        }

        private AddressableResult(AddressableStatus status)
        {
            Box = Box<T>.Empty();
            Status = status;
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
