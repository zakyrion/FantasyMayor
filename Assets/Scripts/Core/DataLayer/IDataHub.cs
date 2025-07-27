namespace Core.DataLayer
{
    public interface IDataHub
    {
        IDataContainer<TDataLayer> GetDataLayer<TDataLayer>() where TDataLayer : struct;
    }
}
