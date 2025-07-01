namespace DataLayer.Core
{
    public interface IContainer
    {
    }

    public interface IContainer<TKey> : IContainer where TKey : class
    {
    }
}
