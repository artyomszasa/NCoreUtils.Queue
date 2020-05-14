namespace NCoreUtils.Queue.Data
{
    public static class EntryState
    {
        public const string Pending = "pending";

        public const string Done = "done";

        public const string Processing = "processing";

        public const string Cancelled = "cancelled";

        public const string Failed = "failed";
    }
}