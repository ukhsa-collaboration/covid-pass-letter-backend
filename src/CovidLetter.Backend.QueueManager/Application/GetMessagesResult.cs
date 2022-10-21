namespace CovidLetter.Backend.QueueManager.Application;

public record GetMessagesResult(PeekedMessages Active, PeekedMessages Scheduled, PeekedMessages DeadLetter);
