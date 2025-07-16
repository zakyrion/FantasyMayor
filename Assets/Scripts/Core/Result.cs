namespace Core
{
    public struct Result<T>
    {
        public T Value { get; init; }
        public Status Status { get; init; }

        public static Result<T> Success(T value)
        {
            return new Result<T>
            {
                Value = value,
                Status = Status.Success
            };
        }

        private static Result<T> Failure(Status status)
        {
            return new Result<T>
            {
                Value = default,
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
