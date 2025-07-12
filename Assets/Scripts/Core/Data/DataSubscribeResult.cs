namespace Core.Data
{
    public struct DataSubscribeResult
    {
        public SubscribeResult SubscribeResult { get; }
        public SubscriptionId SubscriptionId { get; }
        
        public DataSubscribeResult(SubscribeResult subscribeResult, SubscriptionId subscriptionId)
        {
            SubscribeResult = subscribeResult;
            SubscriptionId = subscriptionId;
        }
    }
}
