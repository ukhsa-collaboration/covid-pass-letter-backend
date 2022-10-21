namespace CovidLetter.Backend.QueueManager.Application;

public class MessageSummary
{
    public string MessageId { get; set; } = default!;

    public string CorrelationId { get; set; } = default!;

    public DateTimeOffset EnqueuedTime { get; set; } = DateTimeOffset.MinValue;

    public DateTimeOffset? ScheduledEnqueueTime { get; set; }
}
