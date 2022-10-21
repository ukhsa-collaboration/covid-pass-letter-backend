// ReSharper disable NotAccessedPositionalProperty.Global
namespace CovidLetter.Backend.QueueManager.Application;

using System.Collections.Immutable;

public record ReprocessedMessages(
    int Count,
    int MaxMessages,
    int IncompleteMessages,
    string ReceiverFullyQualifiedNamespace,
    string? SenderFullyQualifiedNamespace,
    string SubQueueEntityPath,
    string EntityPath,
    IReadOnlyList<MessageSummary> Messages)
{
    public ReprocessedMessages(
        int maxMessages,
        string receiverFullyQualifiedNamespace,
        string subQueueEntityPath,
        string entityPath)
        : this(
            default,
            maxMessages,
            default,
            receiverFullyQualifiedNamespace,
            default,
            subQueueEntityPath,
            entityPath,
            ImmutableList<MessageSummary>.Empty)
    {
    }
}
