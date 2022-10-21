namespace CovidLetter.Backend.Common.Application;

using Azure.Messaging.ServiceBus;
using CovidLetter.Backend.Common.Options;

public interface IQueuePoster
{
    ServiceBusMessage MakeJsonMessage<T>(string id, T body, string version, Guid correlationId = default!, DateTimeOffset scheduledEnqueueTime = default);

    Task SendMessageAsync(ServiceBusMessage message, Func<ServiceBusEnqueueOptions, string> queueOrTopic);
}
