namespace CovidLetter.Backend.QueueManager.Application;

using Azure.Messaging.ServiceBus;

public class ServiceBusClientReceiver : ServiceBusClient
{
    public ServiceBusClientReceiver(string connectionString, ServiceBusClientOptions options)
        : base(connectionString, options)
    {
    }

    public override ServiceBusSender CreateSender(string queueOrTopicName)
    {
        throw new NotImplementedException("ServiceBusClientReceiver cannot be used to send messages");
    }
}

public class ServiceBusClientSender : ServiceBusClient
{
    public ServiceBusClientSender(string connectionString, ServiceBusClientOptions options)
        : base(connectionString, options)
    {
    }

    public override ServiceBusReceiver CreateReceiver(string queueName)
    {
        throw new NotImplementedException("ServiceBusClientSender cannot be used to receive messages");
    }

    public override ServiceBusReceiver CreateReceiver(string queueName, ServiceBusReceiverOptions options)
    {
        throw new NotImplementedException("ServiceBusClientSender cannot be used to receive messages");
    }

    public override ServiceBusReceiver CreateReceiver(string topicName, string subscriptionName)
    {
        throw new NotImplementedException("ServiceBusClientSender cannot be used to receive messages");
    }

    public override ServiceBusReceiver CreateReceiver(string topicName, string subscriptionName, ServiceBusReceiverOptions options)
    {
        throw new NotImplementedException("ServiceBusClientSender cannot be used to receive messages");
    }
}
