namespace CovidLetter.Backend.Common.Infrastructure.Barcodes;

using System.Net.Http.Json;
using System.Text.Json;
using CovidLetter.Backend.Common.Application.Barcodes;
using CovidLetter.Backend.Common.Application.Constants;
using CovidLetter.Backend.Common.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class BarcodePublicKeyProvider
    : IBarcodePublicKeyProvider
{
    private static readonly JsonSerializerOptions JsonConfig = new(JsonSerializerDefaults.Web);
    private readonly IHttpClientFactory httpClientFactory;
    private readonly IMemoryCache cache;
    private readonly BarcodeOptions barcodeOptions;
    private readonly ILogger<BarcodePublicKeyProvider> log;

    public BarcodePublicKeyProvider(
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        IOptions<BarcodeOptions> barcodeOptions,
        ILogger<BarcodePublicKeyProvider> log)
    {
        this.httpClientFactory = httpClientFactory;
        this.cache = cache;
        this.barcodeOptions = barcodeOptions.Value;
        this.log = log;
    }

    public async Task<IReadOnlyDictionary<string, string>> GetKeysAsync()
    {
        return await this.cache.GetOrCreateAsync("AD_PubKeys", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
            return await this.LookupKeysAsync();
        });
    }

    private async Task<IReadOnlyDictionary<string, string>> LookupKeysAsync()
    {
        using var client = this.httpClientFactory.CreateClient(StringConsts.BarcodePublicKeyClient);
        var endpointUrl = this.barcodeOptions.DevolvedAdministrationPublicKeysEndpointUrl;
        this.log.LogInformation("Getting public keys from {Url}", $"{client.BaseAddress}{endpointUrl}");

        var data = await client.GetFromJsonAsync<List<PubKey>>(endpointUrl, JsonConfig);
        if (data == null)
        {
            throw new InvalidOperationException("Could not retrieve keys");
        }

        return data.ToDictionary(d => d.Kid, d => d.PublicKey);
    }
}

public class PubKey
{
    public string Kid { get; set; } = null!;

    public string PublicKey { get; set; } = null!;

    public string Country { get; set; } = null!;
}
