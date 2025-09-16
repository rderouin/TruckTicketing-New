namespace SE.Enterprise.Contracts.Constants;

public static class MessageConstants
{
    public const string CorrelationId = "CorrelationId";

    public const string SourceId = nameof(SourceId);

    public const string SessionId = nameof(SessionId);

    public const string SequenceNumber = nameof(SequenceNumber);

    public static class EntityUpdate
    {
        public const string MessageId = "MessageId";

        public const string MessageType = "MessageType";
    }

    public static class Changes
    {
        public const string Recovery = nameof(Recovery);
    }
}
