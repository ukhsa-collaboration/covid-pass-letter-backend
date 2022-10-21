namespace CovidLetter.Backend.QueueManager.Application;

using AutoMapper;
using Azure;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using CovidLetter.Backend.Common.Options;
using CovidLetter.Backend.QueueManager.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class ServiceBusHelper
{
    private readonly FunctionOptions functionOptions;
    private readonly ServiceBusClientReceiver serviceBusClientReceiver;
    private readonly ServiceBusClientSender serviceBusClientSender;
    private readonly ServiceBusAdministrationClient serviceBusAdministrationClient;
    private readonly ILogger<ServiceBusHelper> logger;
    private readonly IOptions<QueueOptions> queueOptions;
    private readonly IMapper mapper;
    private readonly ExtendedFunctionOptions extendedFunctionOptions;

    public ServiceBusHelper(
        IOptions<FunctionOptions> functionOptions,
        ServiceBusClientReceiver serviceBusClientReceiver,
        ServiceBusClientSender serviceBusClientSender,
        ServiceBusAdministrationClient serviceBusAdministrationClient,
        ILogger<ServiceBusHelper> logger,
        IOptions<QueueOptions> queueOptions,
        IMapper mapper,
        IOptions<ExtendedFunctionOptions> extendedFunctionOptions)
    {
        this.functionOptions = functionOptions.Value;
        this.serviceBusClientReceiver = serviceBusClientReceiver;
        this.serviceBusClientSender = serviceBusClientSender;
        this.serviceBusAdministrationClient = serviceBusAdministrationClient;
        this.logger = logger;
        this.queueOptions = queueOptions;
        this.mapper = mapper;
        this.extendedFunctionOptions = extendedFunctionOptions.Value;
    }

    public async Task<GetMessagesResult> GetMessagesAsync(
        Func<ServiceBusEnqueueOptions, string> topicSelector,
        Func<FunctionOptions, string> subscriptionSelector,
        CancellationToken cancellationToken)
    {
        var messageRequest = new ReceiveMessageRequest
        {
            TopicSelector = topicSelector,
            SubscriptionSelector = subscriptionSelector,
            CancellationToken = cancellationToken,
        };

        return await this.GetMessagesAsync(messageRequest);
    }

    public async Task<GetMessagesResult> GetMessagesAsync(
        Func<ExtendedFunctionOptions, string> queueSelector,
        CancellationToken cancellationToken)
    {
        var messageRequest = new ReceiveMessageRequest
        {
            QueueSelector = queueSelector,
            CancellationToken = cancellationToken,
        };

        return await this.GetMessagesAsync(messageRequest);
    }

    public async Task<ReprocessedMessages> ReprocessDeadLettersAsync(
        Func<ServiceBusEnqueueOptions, string> topicSelector,
        Func<FunctionOptions, string> subscriptionSelector,
        CancellationToken cancellationToken)
    {
        var reprocessedMessages = await this.ReprocessSubQueueMessagesAsync(
            new ReceiveMessageRequest
            {
                TopicSelector = topicSelector,
                SubscriptionSelector = subscriptionSelector,
                SubQueue = SubQueue.DeadLetter,
                CancellationToken = cancellationToken,
            });

        return reprocessedMessages;
    }

    public async Task<SubscriptionOrQueueMessageCount?> GetSubscriptionMessageCountAsync(
        ServiceBusEntityRequest request)
    {
        var topic = request.TopicSelector(this.functionOptions);
        Response<TopicRuntimeProperties>? topicProperties;
        try
        {
            topicProperties = await this.serviceBusAdministrationClient.GetTopicRuntimePropertiesAsync(
                topic,
                request.CancellationToken);
        }
        catch (ServiceBusException ex)
        {
            this.logger.LogError(ex, "Unable to get topic properties for {Topic}", topic);
            topicProperties = null;
        }

        if (topicProperties == null)
        {
            return null;
        }

        var subscription = request.SubscriptionSelector(this.functionOptions);
        Response<SubscriptionRuntimeProperties>? subscriptionProperties;
        try
        {
            subscriptionProperties = await this.serviceBusAdministrationClient.GetSubscriptionRuntimePropertiesAsync(
                topic,
                subscription,
                request.CancellationToken);
        }
        catch (ServiceBusException ex)
        {
            this.logger.LogError(
                ex,
                "Unable to get subscription properties for {Subscription} on {Topic}",
                subscription,
                topic);
            subscriptionProperties = null;
        }

        if (subscriptionProperties == null)
        {
            return null;
        }

        return new SubscriptionOrQueueMessageCount(
            await this.GetServiceBusAdministrationClientFullyQualifiedNamespace(),
            subscriptionProperties.Value.TopicName,
            subscriptionProperties.Value.SubscriptionName,
            subscriptionProperties.Value.ActiveMessageCount,
            topicProperties.Value.ScheduledMessageCount,
            subscriptionProperties.Value.DeadLetterMessageCount);
    }

    public async Task<SubscriptionOrQueueMessageCount?> GetQueueMessageCountAsync(ServiceBusEntityRequest request)
    {
        var queue = request.QueueSelector(this.extendedFunctionOptions);
        Response<QueueRuntimeProperties>? queueProperties;
        try
        {
            queueProperties = await this.serviceBusAdministrationClient.GetQueueRuntimePropertiesAsync(
                queue,
                request.CancellationToken);
        }
        catch (ServiceBusException ex)
        {
            this.logger.LogError(ex, "Unable to get queue properties for {Queue}", queue);
            queueProperties = null;
        }

        if (queueProperties == null)
        {
            return null;
        }

        return new SubscriptionOrQueueMessageCount(
            await this.GetServiceBusAdministrationClientFullyQualifiedNamespace(),
            queueProperties.Value.Name,
            queueProperties.Value.ActiveMessageCount,
            queueProperties.Value.ScheduledMessageCount,
            queueProperties.Value.DeadLetterMessageCount);
    }

    private async Task<GetMessagesResult> GetMessagesAsync(ReceiveMessageRequest messageRequest)
    {
        // active messages
        var active = await this.PeekMessagesAsync(messageRequest);
        if (active == null)
        {
            return new GetMessagesResult(null!, null!, null!);
        }

        // dead letter messages
        messageRequest.SubQueue = SubQueue.DeadLetter;
        var deadLetter = await this.PeekMessagesAsync(messageRequest);

        // scheduled messages
        messageRequest.Filter = f => f.ScheduledEnqueueTime > DateTimeOffset.MinValue;
        if (active.EntityPath.Contains(EntityNameFormatter.FormatSubscriptionPath(string.Empty, string.Empty)))
        {
            // for subscriptions, scheduled messages are on the parent topic so may not
            // necessarily reach said subscription, depending on correlation configuration
            messageRequest.QueueSelector = messageRequest.TopicSelector;
            messageRequest.SubQueue = SubQueue.None;
        }

        var scheduled = await this.PeekMessagesAsync(messageRequest);
        return new GetMessagesResult(active, scheduled!, deadLetter!);
    }

    private async Task<string> GetServiceBusAdministrationClientFullyQualifiedNamespace()
    {
        var properties = await this.serviceBusAdministrationClient.GetNamespacePropertiesAsync();
        return $"{properties.Value.Name}.servicebus.windows.net";
    }

    private ServiceBusReceiver CreateReceiver(ReceiveMessageRequest request)
    {
        var queue = request.QueueSelector(this.extendedFunctionOptions);
        var options = new ServiceBusReceiverOptions
        {
            SubQueue = request.SubQueue,
        };

        return string.IsNullOrWhiteSpace(queue)
            ? this.serviceBusClientReceiver.CreateReceiver(
                request.TopicSelector(this.functionOptions),
                request.SubscriptionSelector(this.functionOptions),
                options)
            : this.serviceBusClientReceiver.CreateReceiver(queue, options);
    }

    private async Task<PeekedMessages?> PeekMessagesAsync(ReceiveMessageRequest request)
    {
        await using var receiver = this.CreateReceiver(request);
        var maxMessages = request.MaxMessages ?? this.queueOptions.Value.MaxPeekMessages;

        IReadOnlyList<ServiceBusReceivedMessage> peekedMessages;
        try
        {
            // keep peeking messages until no more are received, as each batch may have a unique partition key
            peekedMessages = await receiver.PeekMessagesAsync(
                maxMessages,
                request.FromSequenceNumber,
                request.CancellationToken);
        }
        catch (ServiceBusException ex)
        {
            this.logger.LogError(
                ex,
                "Unable to peek message(s) for {EntityPath} on {Namespace}",
                receiver.EntityPath,
                receiver.FullyQualifiedNamespace);
            return null;
        }

        var messages = new List<ServiceBusReceivedMessage>(peekedMessages);
        while (peekedMessages.Count > 0 && messages.Count <= maxMessages)
        {
            peekedMessages = await receiver.PeekMessagesAsync(
                maxMessages,
                peekedMessages.Select(m => m.SequenceNumber).Last() + 1,
                request.CancellationToken);
            messages.AddRange(peekedMessages);
        }

        messages = messages.Where(request.Filter).ToList();
        this.logger.LogInformation(
            "{MessageCount} message(s) received on {EntityPath} on {Namespace}",
            messages.Count,
            receiver.EntityPath,
            receiver.FullyQualifiedNamespace);
        return new PeekedMessages(
            messages.Count,
            maxMessages,
            receiver.FullyQualifiedNamespace,
            receiver.EntityPath,
            this.mapper.Map<IReadOnlyList<MessageSummary>>(messages));
    }

    private async Task<ReprocessedMessages> ReprocessSubQueueMessagesAsync(ReceiveMessageRequest request)
    {
        if (request.SubQueue == SubQueue.None)
        {
            throw new ArgumentException("Invalid sub queue defined for reprocessing");
        }

        // get the sub-queue entity path to move the messages from
        await using var receiver = this.CreateReceiver(request);
        var subQueueEntityPath = receiver.EntityPath;
        var maxMessages = request.MaxMessages ?? this.queueOptions.Value.MaxMoveMessages;
        var messages = await receiver.ReceiveMessagesAsync(
            maxMessages,
            TimeSpan.FromSeconds(10),
            request.CancellationToken);

        // get the parent entity path corresponding to the topic to move the sub-queue messages to
        var entityPath = request.TopicSelector(this.functionOptions);
        this.logger.LogInformation(
            "{MessageCount} message(s) received on {EntityPath} on {Namespace}",
            messages.Count,
            subQueueEntityPath,
            receiver.FullyQualifiedNamespace);
        if (!messages.Any())
        {
            return new ReprocessedMessages(
                maxMessages,
                receiver.FullyQualifiedNamespace,
                subQueueEntityPath,
                entityPath);
        }

        var partitionKey = DateTime.UtcNow.ToString("O");
        var queuedMessages = new Queue<ServiceBusMessage>(messages.Select(m =>
            new ServiceBusMessage(m) { PartitionKey = partitionKey }));
        var messageCount = queuedMessages.Count;

        // batch and send all messages from the queue
        await using var sender = this.serviceBusClientSender.CreateSender(entityPath);
        while (queuedMessages.Count > 0)
        {
            using var messageBatch = await sender.CreateMessageBatchAsync(request.CancellationToken);
            var firstMessage = queuedMessages.Peek();
            var messageIndex = messageCount - queuedMessages.Count;

            if (messageBatch.TryAddMessage(firstMessage))
            {
                queuedMessages.Dequeue();
                this.logger.LogInformation(
                    "Enqueued message {MessageIndex} ({MessageId}) into batch for {EntityPath} on {Namespace}",
                    messageIndex,
                    firstMessage.MessageId,
                    entityPath,
                    sender.FullyQualifiedNamespace);
            }
            else
            {
                // if the first message can't fit, then it is too large for the batch
                throw new ApplicationException($"Message {messageIndex} is too large and cannot be sent.");
            }

            while (queuedMessages.Count > 0 && messageBatch.TryAddMessage(queuedMessages.Peek()))
            {
                queuedMessages.Dequeue();
                this.logger.LogInformation(
                    "Enqueued message {MessageIndex} into batch for {EntityPath} on {Namespace}",
                    messageIndex,
                    entityPath,
                    sender.FullyQualifiedNamespace);
            }

            this.logger.LogInformation(
                "Sending batch of {Count} message(s) from {SubQueueEntityPath} to {EntityPath} on {Namespace}",
                messageBatch.Count,
                subQueueEntityPath,
                entityPath,
                sender.FullyQualifiedNamespace);
            await sender.SendMessagesAsync(messageBatch, request.CancellationToken);
        }

        var i = 0;
        var incompleteMessages = 0;
        foreach (var receivedMessage in messages)
        {
            this.logger.LogInformation(
                "Completing message {MessageIndex} ({MessageId}) on {SubQueueEntityPath}",
                i,
                receivedMessage.MessageId,
                subQueueEntityPath);

            try
            {
                await receiver.CompleteMessageAsync(receivedMessage, request.CancellationToken);
            }
            catch (ServiceBusException ex)
            {
                // "The lock supplied is invalid. Either the lock expired, or the message has already been removed from the queue."
                this.logger.LogWarning(
                    ex,
                    "Unable to complete message {MessageIndex} ({MessageId}). It may exist in both {EntityPath} and {SubQueueEntityPath}",
                    i,
                    receivedMessage.MessageId,
                    entityPath,
                    subQueueEntityPath);
                incompleteMessages++;
            }

            i++;
        }

        return new ReprocessedMessages(
            messages.Count,
            maxMessages,
            incompleteMessages,
            receiver.FullyQualifiedNamespace,
            sender.FullyQualifiedNamespace,
            subQueueEntityPath!,
            entityPath,
            this.mapper.Map<IReadOnlyList<MessageSummary>>(messages));
    }
}
