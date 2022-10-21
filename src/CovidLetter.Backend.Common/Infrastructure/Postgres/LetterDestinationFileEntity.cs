namespace CovidLetter.Backend.Common.Infrastructure.Postgres;

public class LetterDestinationFileEntity
{
    public Guid LetterId { get; set; }

    public Guid? DestinationFileId { get; set; }

    public int FileType { get; set; }
}
