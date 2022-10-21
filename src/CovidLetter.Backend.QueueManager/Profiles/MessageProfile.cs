namespace CovidLetter.Backend.QueueManager.Profiles;

using AutoMapper;
using Azure.Messaging.ServiceBus;
using CovidLetter.Backend.QueueManager.Application;

public class MessageProfile : Profile
{
    public MessageProfile()
    {
        this.CreateMap<ServiceBusReceivedMessage, MessageSummary>()
            .ForMember(
                dest => dest.ScheduledEnqueueTime,
                opt => opt.Condition(src => src.ScheduledEnqueueTime > DateTimeOffset.MinValue));
    }
}
