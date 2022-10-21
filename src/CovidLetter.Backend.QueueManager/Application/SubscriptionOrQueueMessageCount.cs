// ReSharper disable NotAccessedPositionalProperty.Global
namespace CovidLetter.Backend.QueueManager.Application;

public record SubscriptionOrQueueMessageCount(
    string FullyQualifiedNamespace,
    string TopicName,
    string SubscriptionName,
    long ActiveMessageCount,
    long ScheduledMessageCount,
    long DeadLetterMessageCount)
{
    public SubscriptionOrQueueMessageCount(
        string fullyQualifiedNamespace,
        string topicName,
        long activeMessageCount,
        long scheduledMessageCount,
        long deadLetterMessageCount)
        : this(fullyQualifiedNamespace, topicName, null!, activeMessageCount, scheduledMessageCount, deadLetterMessageCount)
    {
    }
}
