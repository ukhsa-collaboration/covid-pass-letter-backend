namespace CovidLetter.Backend.Common.Application.Serialization;

using Microsoft.Extensions.DependencyInjection;

internal class FileSerializerFactory : IFileSerializerFactory
{
    private readonly IServiceProvider serviceProvider;

    public FileSerializerFactory(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    public FileSerializer<TRow> CreateFileSerializer<TRow>()
        where TRow : class, new() =>
        this.serviceProvider.GetRequiredService<FileSerializer<TRow>>();
}
