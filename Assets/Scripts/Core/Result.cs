using System;

namespace Core
{
    public struct Result<T>
    {
        public Box<T> Box { get; init; }
        public Status Status { get; init; }

        public static Result<T> Success(T value, Action<T> dispose = null)
        {
            return new Result<T>
            {
                Box = Box<T>.Wrap(value, dispose),
                Status = Status.Success
            };
        }

        private static Result<T> Failure(Status status)
        {
            return new Result<T>
            {
                Box = default,
                Status = status
            };
        }

        public static Result<T> Cancelled()
        {
            return Failure(Status.Cancelled);
        }

        public static Result<T> Fail()
        {
            return Failure(Status.Failed);
        }
    }
}
