namespace CovidLetter.Backend.Common.Infrastructure.SftpFiles;

using CovidLetter.Backend.Common.Options;
using CovidLetter.Backend.Common.Utilities;
using Microsoft.Extensions.Options;
using Renci.SshNet;

public class SftpFileSystem
{
    private readonly StorageOptions storageOptions;

    public SftpFileSystem(
        IOptions<StorageOptions> options)
    {
        this.storageOptions = options.Value;
    }

    public void Upload(string filePath, byte[] content)
    {
        using var sftpClient = this.MakeSftpClient();

        var sftpPath = this.storageOptions.OutputSftpPath;
        foreach (var directory in CloudPath.GetDirectories(filePath))
        {
            sftpPath += $"/{directory}";
            sftpClient.CreateDirectory(sftpPath);
        }

        using var stream = new MemoryStream(content);
        sftpClient.UploadFile(stream, $"{this.storageOptions.OutputSftpPath}/{filePath}");
    }

    public SftpClient MakeSftpClient()
    {
        var (host, port, username, password) = this.storageOptions.OutputSftpOptions;

        SftpClient? result = null;
        try
        {
            result = new SftpClient(host, port == 0 ? 22 : port, username, password);
            result.Connect();
            return result;
        }
        catch
        {
            result?.Dispose();
            throw;
        }
    }
}
