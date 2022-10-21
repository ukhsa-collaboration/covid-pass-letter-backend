namespace CovidLetter.Backend.QueueManager.Application;

using Azure.Messaging.ServiceBus;

public class ReceiveMessageRequest : ServiceBusEntityRequest
{
    public int? MaxMessages { get; set; } = null;

    public long? FromSequenceNumber { get; set; } = null;

    public SubQueue SubQueue { get; set; } = SubQueue.None;

    public Func<ServiceBusReceivedMessage, bool> Filter { get; set; } =
        f => f.ScheduledEnqueueTime == DateTimeOffset.MinValue;
}
