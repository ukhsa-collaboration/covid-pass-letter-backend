namespace CovidLetter.Backend.Common.Infrastructure.AzureFiles;

public interface IAzureNode
{
    string GetPathRelativeTo(string relativeTo);
}
