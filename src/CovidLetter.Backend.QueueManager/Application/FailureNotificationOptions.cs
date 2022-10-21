namespace CovidLetter.Backend.QueueManager.Application;

public class FailureNotificationOptions
{
    public ICollection<string> RecipientWhitelist { get; set; } = new List<string>();
}
