namespace CovidLetter.Backend.QueueManager.Configuration;

public class QueueOptions
{
    public int MaxPeekMessages { get; set; } = 1000;

    public int MaxMoveMessages { get; set; } = 1000;
}
