// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
namespace CovidLetter.Backend.QueueManager.Application;

using Azure.Messaging.ServiceBus;

/// <summary>
/// This class can be used to format the path for different Service Bus entity types.
/// </summary>
internal static class EntityNameFormatter
{
    internal const string PathDelimiter = @"/";
    private const string SubscriptionsSubPath = "Subscriptions";
    private const string SubQueuePrefix = "$";
    internal const string DeadLetterQueueSuffix = "DeadLetterQueue";
    private const string DeadLetterQueueName = SubQueuePrefix + DeadLetterQueueSuffix;
    internal const string EnqueueFailureNotificationSuffix = "EnqueueFailureNotification";
    private const string Transfer = "Transfer";
    private const string TransferDeadLetterQueueName = SubQueuePrefix + Transfer + PathDelimiter + DeadLetterQueueName;

    /// <summary>
    /// Formats the entity path for a receiver or processor taking into account whether using a SubQueue.
    /// </summary>
    public static string? FormatEntityPath(string? entityPath, SubQueue subQueue)
    {
        return subQueue switch
        {
            SubQueue.None => entityPath,
            SubQueue.DeadLetter => FormatDeadLetterPath(entityPath),
            SubQueue.TransferDeadLetter => FormatTransferDeadLetterPath(entityPath),
            _ => null,
        };
    }

    /// <summary>
    /// Formats the dead letter path for either a queue, or a subscription.
    /// </summary>
    /// <param name="entityPath">The name of the queue, or path of the subscription.</param>
    /// <returns>The path as a string of the dead letter entity.</returns>
    public static string FormatDeadLetterPath(string? entityPath)
    {
        return FormatSubQueuePath(entityPath, DeadLetterQueueName);
    }

    /// <summary>
    /// Formats the subqueue path for either a queue, or a subscription.
    /// </summary>
    /// <param name="entityPath">The name of the queue, or path of the subscription.</param>
    /// <param name="subQueueName"></param>
    /// <returns>The path as a string of the subqueue entity.</returns>
    public static string FormatSubQueuePath(string? entityPath, string subQueueName)
    {
        return string.Concat(entityPath, PathDelimiter, subQueueName);
    }

    /// <summary>
    /// Formats the subscription path, based on the topic path and subscription name.
    /// </summary>
    /// <param name="topicPath">The name of the topic, including slashes.</param>
    /// <param name="subscriptionName">The name of the subscription.</param>
    public static string FormatSubscriptionPath(string topicPath, string subscriptionName)
    {
        return string.Concat(topicPath, PathDelimiter, SubscriptionsSubPath, PathDelimiter, subscriptionName);
    }

    /// <summary>
    /// Utility method that creates the name for the transfer dead letter receiver, specified by <paramref name="entityPath"/>
    /// </summary>
    public static string FormatTransferDeadLetterPath(string? entityPath)
    {
        return string.Concat(entityPath, PathDelimiter, TransferDeadLetterQueueName);
    }
}
