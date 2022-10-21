namespace CovidLetter.Backend.Common.Application;

using System.Text.Json;
using Azure.Core;
using Azure.Messaging.ServiceBus;
using CovidLetter.Backend.Common.Application.Constants;
using CovidLetter.Backend.Common.Options;
using CovidLetter.Backend.Common.Utilities;
using Microsoft.Extensions.Options;

public class QueuePoster : IQueuePoster
{
    private readonly FunctionOptions functionOptions;
    private readonly ServiceBusClient serviceBusClient;

    public QueuePoster(
        IOptions<FunctionOptions> functionOptions,
        ServiceBusClient serviceBusClient)
    {
        this.functionOptions = functionOptions.Value;
        this.serviceBusClient = serviceBusClient;
    }

    ServiceBusMessage IQueuePoster.MakeJsonMessage<T>(
        string id,
        T body,
        string version,
        Guid correlationId,
        DateTimeOffset scheduledEnqueueTime) =>
        MakeJsonMessage(id, body, version, correlationId, scheduledEnqueueTime);

    public async Task SendMessageAsync(ServiceBusMessage message, Func<ServiceBusEnqueueOptions, string> queueOrTopic)
    {
        await using var sender = this.serviceBusClient.CreateSender(queueOrTopic(this.functionOptions));
        await sender.SendMessageAsync(message);
    }

    public static ServiceBusMessage MakeJsonMessage<T>(
        string id,
        T body,
        string version,
        Guid correlationId = default!,
        DateTimeOffset scheduledEnqueueTime = default)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(body, JsonConfig.Default);
        return new ServiceBusMessage(bytes)
        {
            MessageId = id,
            CorrelationId = correlationId == default ? id : correlationId.ToString(),
            ContentType = ContentType.ApplicationJson.ToString(),
            ApplicationProperties =
            {
                [QueueMetadataKeys.Version] = version,
                [QueueMetadataKeys.Sha256Checksum] = Checksum.Sha256(bytes),
            },
            ScheduledEnqueueTime = scheduledEnqueueTime,
        };
    }
}
