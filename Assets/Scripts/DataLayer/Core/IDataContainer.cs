namespace DataLayer.Core
{
    /// <summary>
    ///     Used to mark classes that are used to store data as a part of the data layer
    /// </summary>
    public interface IDataContainer
    {
        void Dispose();
    }
}
