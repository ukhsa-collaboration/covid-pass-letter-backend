namespace CovidLetter.Backend.Common.Utilities;

using System.Text;
using System.Text.Json;
using CovidLetter.Backend.Common.Application.Constants;

public record QueuedMessage(
    string Id,
    string Body,
    string CorrelationId,
    IDictionary<string, object> UserProperties)
{
    public T DeserializeWithoutValidation<T>(string expectedVersion)
        where T : class
    {
        this.ValidateVersion(expectedVersion);
        this.ValidateChecksum();

        return this.Deserialize<T>();
    }

    public T DeserializeAndValidate<T>(string expectedVersion)
        where T : class
    {
        var result = this.DeserializeWithoutValidation<T>(expectedVersion);
        ValidationHelpers.Validate(result);
        return result;
    }

    private void ValidateVersion(string expectedVersion)
    {
        if (!this.UserProperties.TryGetStringValue(QueueMetadataKeys.Version, out var version))
        {
            throw new ArgumentException($"{this.Id} has no version");
        }

        if (!string.Equals(version, expectedVersion, StringComparison.Ordinal))
        {
            throw new ArgumentException($"{this.Id} has unknown version: {version}");
        }
    }

    private void ValidateChecksum()
    {
        if (!this.UserProperties.TryGetStringValue(QueueMetadataKeys.Sha256Checksum, out var checksum))
        {
            throw new InvalidOperationException($"{this.Id} has no checksum");
        }

        if (Checksum.Sha256(Encoding.UTF8.GetBytes(this.Body)) != checksum)
        {
            throw new InvalidOperationException($"{this.Id} checksum does not match computed checksum");
        }
    }

    private T Deserialize<T>()
        where T : class
    {
        if (string.IsNullOrWhiteSpace(this.Body))
        {
            throw new InvalidOperationException($"{this.Id} is missing request body");
        }

        try
        {
            return JsonSerializer.Deserialize<T>(this.Body, JsonConfig.Default) ??
                   throw new InvalidOperationException($"Deserialization of {this.Id} resulted in null");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Unable to deserialize {this.Id}", ex);
        }
    }
}
