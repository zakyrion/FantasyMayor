namespace Core.Data
{
    public struct DataLayerResult<T> where T : struct
    {
        public T DataLayer { get; }
        public bool Exist { get; }

        public DataLayerResult(T dataLayer, bool exist)
        {
            DataLayer = dataLayer;
            Exist = exist;
        }
    }
}
