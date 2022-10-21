namespace CovidLetter.Backend.Common.Options
{
    public class StorageOptions
    {
        public string InputFileShare { get; set; } = null!;

        public string? InputDirectory { get; set; } = null!;

        public string OutputFileShare { get; set; } = null!;

        public string DelimitedFileOutputDirectory { get; set; } = null!;

        public string LongTermFileStoreOutputDirectory { get; set; } = null!;

        public string RejectsDirectory { get; set; } = null!;

        public SftpOptions OutputSftpOptions { get; set; } = null!;

        public string OutputSftpPath { get; set; } = string.Empty;

        public class SftpOptions
        {
            public string Host { get; set; } = default!;

            public int Port { get; set; } = default!;

            public string UserName { get; set; } = default!;

            public string Password { get; set; } = default!;

            public void Deconstruct(out string host, out int port, out string username, out string password)
            {
                host = this.Host;
                port = this.Port;
                username = this.UserName;
                password = this.Password;
            }
        }
    }
}
