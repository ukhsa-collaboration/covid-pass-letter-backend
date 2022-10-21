namespace CovidLetter.Backend.QueueManager.Application;

using CovidLetter.Backend.Common.Options;
using CovidLetter.Backend.QueueManager.Configuration;

public class ServiceBusEntityRequest
{
    public Func<ServiceBusEnqueueOptions, string> TopicSelector { get; set; } = _ => string.Empty;

    public Func<FunctionOptions, string> SubscriptionSelector { get; set; } = _ => string.Empty;

    public Func<ExtendedFunctionOptions, string> QueueSelector { get; set; } = _ => string.Empty;

    public CancellationToken CancellationToken { get; set; }
}
