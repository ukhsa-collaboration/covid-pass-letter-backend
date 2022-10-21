namespace CovidLetter.Backend.QueueManager.Configuration;

public static class ConnectionStrings
{
    public static readonly string ServiceBusReceiver = nameof(ServiceBusReceiver);

    public static readonly string ServiceBusSender = nameof(ServiceBusSender);
}
