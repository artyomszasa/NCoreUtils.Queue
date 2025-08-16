
namespace NCoreUtils.Queue;

internal partial class SubscriberClient
{
    private class ResultVisitor(SubscriberClient client, string? messageId) : IResultVisitor
    {
        public SubscriberClient Client { get; } = client;

        public string? MessageId { get; private set; } = messageId;

        public bool Ack { get; private set; }

        public ResultVisitor Update(string? messageId)
        {
            MessageId = messageId;
            return this;
        }

        void IResultVisitor.VisitFailedDueToException(Exception exn)
        {
            Client.Logger.LogSubscriberClientFailedException(exn, MessageId);
            Ack = false;
        }

        void IResultVisitor.VisitFailedDueToReason(string reason)
        {
            Client.Logger.LogSubscriberClientFailedReason(reason, MessageId);
            Ack = false;
        }

        void IResultVisitor.VisitSucceeded()
        {
            Client.Logger.LogSubscriberClientSuccess(MessageId);
            Ack = true;
        }

        void IResultVisitor.VisitUnprocessableDueToException(Exception exn)
        {
            Client.Logger.LogSubscriberClientUnprocessableException(exn, MessageId);
            Ack = true;
        }

        void IResultVisitor.VisitUnprocessableDueToReason(string reason)
        {
            Client.Logger.LogSubscriberClientUnprocessableReason(reason, MessageId);
            Ack = true;
        }
    }

    private static FixSizePool<ResultVisitor> ResultVisitorPool { get; } = new(ConcurrentWorkerCount);

    /// <summary>
    /// Evaluates the result and returns whether message should be acknowleged.
    /// </summary>
    /// <param name="result">Result to evaluate.</param>
    /// <param name="messageId">Related message id.</param>
    /// <returns>
    /// <see langword="true" /> if the messagge should be acknowleged, <see langword="false"/> otherwise.
    /// </returns>
    private bool VisitResult(Result result, string? messageId)
    {
        var visitor = ResultVisitorPool.TryRent(out var instance)
            ? instance.Update(messageId)
            : new(this, messageId);
        try
        {
            result.Accept(visitor);
            return visitor.Ack;
        }
        finally
        {
            ResultVisitorPool.Return(visitor);
        }
    }
}