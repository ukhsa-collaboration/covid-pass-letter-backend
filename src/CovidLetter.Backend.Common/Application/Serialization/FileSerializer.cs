namespace CovidLetter.Backend.Common.Application.Serialization;

public class FileSerializer<TRow>
    where TRow : class, new()
{
    private readonly FileGenerator<TRow> generator;

    public FileSerializer(IFileGeneratorFactory<TRow> fileGeneratorFactory)
    {
        this.generator = fileGeneratorFactory.Create();
    }

    public string GenerateOutput(IReadOnlyCollection<TRow> letters)
    {
        return this.generator.Generate(letters);
    }
}
