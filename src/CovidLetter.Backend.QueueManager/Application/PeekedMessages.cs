namespace CovidLetter.Backend.QueueManager.Application;

public record PeekedMessages(
    int Count,
    int MaxMessages,
    string FullyQualifiedNamespace,
    string EntityPath,
    IReadOnlyList<MessageSummary> Messages);
