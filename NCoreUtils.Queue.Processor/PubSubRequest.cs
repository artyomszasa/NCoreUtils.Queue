namespace NCoreUtils.Queue
{
    public class PubSubRequest
    {
        public PubSubMessage Message { get; set; }

        public string Subscription { get; set;  }
    }
}