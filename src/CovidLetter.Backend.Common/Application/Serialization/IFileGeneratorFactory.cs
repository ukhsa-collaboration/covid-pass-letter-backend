namespace CovidLetter.Backend.Common.Application.Serialization;

public interface IFileGeneratorFactory<TRow>
    where TRow : class, new()
{
    FileGenerator<TRow> Create();
}
