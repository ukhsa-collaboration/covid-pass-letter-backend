// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
namespace CovidLetter.Backend.QueueManager.Configuration;

using CovidLetter.Backend.Common.Options;

public class ExtendedFunctionOptions : ServiceBusEnqueueOptions
{
    public string InboundStagingQueue { get; set; } = string.Empty;
}
