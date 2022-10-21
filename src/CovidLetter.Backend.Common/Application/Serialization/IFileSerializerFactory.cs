namespace CovidLetter.Backend.Common.Application.Serialization;

public interface IFileSerializerFactory
{
    public FileSerializer<TRow> CreateFileSerializer<TRow>()
        where TRow : class, new();
}
