namespace NCoreUtils.Queue
{
    public class PubSubRequest
    {
        public PubSubMessage Message { get; set; } = default!;

        public string Subscription { get; set; } = string.Empty;
    }
}