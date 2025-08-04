namespace Core.Data
{
    public struct DataContext<T> where T : struct
    {
        public DataLayerResult<T> Old;
        public DataLayerResult<T> New;
    }
}
