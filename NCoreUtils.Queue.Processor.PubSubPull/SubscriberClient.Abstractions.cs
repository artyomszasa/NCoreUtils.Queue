namespace NCoreUtils.Queue;

internal partial class SubscriberClient
{
    private interface IResultVisitor
    {
        void VisitSucceeded();

        void VisitUnprocessableDueToReason(string reason);

        void VisitUnprocessableDueToException(Exception exn);

        void VisitFailedDueToReason(string reason);

        void VisitFailedDueToException(Exception exn);
    }

    private abstract class Result
    {
        private sealed class SucceededResult : Result
        {
            public override void Accept(IResultVisitor visitor)
                => visitor.VisitSucceeded();
        }

        private sealed class UnprocessableDueToReason(string reason) : Result
        {
            public string Reason { get; } = reason;

            public override void Accept(IResultVisitor visitor)
                => visitor.VisitUnprocessableDueToReason(Reason);
        }

        private sealed class UnprocessableDueToException(Exception exn) : Result
        {
            public Exception Exn { get; } = exn;

            public override void Accept(IResultVisitor visitor)
                => visitor.VisitUnprocessableDueToException(Exn);
        }

        private sealed class FailedDueToReason(string reason) : Result
        {
            public string Reason { get; } = reason;

            public override void Accept(IResultVisitor visitor)
                => visitor.VisitFailedDueToReason(Reason);
        }

        private sealed class FailedDueToException(Exception exn) : Result
        {
            public Exception Exn { get; } = exn;

            public override void Accept(IResultVisitor visitor)
                => visitor.VisitFailedDueToException(Exn);
        }

        public static Result Success { get; } = new SucceededResult();

        public static Result Unprocessable(string reason) => new UnprocessableDueToReason(reason);

        public static Result Unprocessable(Exception exn) => new UnprocessableDueToException(exn);

        public static Result Failure(string reason) => new FailedDueToReason(reason);

        public static Result Failure(Exception exn) => new FailedDueToException(exn);

        public abstract void Accept(IResultVisitor visitor);
    }
}