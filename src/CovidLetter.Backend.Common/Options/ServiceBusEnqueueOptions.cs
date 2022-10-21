// ReSharper disable PropertyCanBeMadeInitOnly.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
namespace CovidLetter.Backend.Common.Options;

public abstract class ServiceBusEnqueueOptions
{
    public string InboundTopic { get; set; } = string.Empty;

    public string SuccessTopic { get; set; } = string.Empty;

    public string FailureTopic { get; set; } = string.Empty;
}
