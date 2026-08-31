using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace SeasonEnded.Api.Notifications;

public sealed class WebPushSender(
    IHttpClientFactory httpClientFactory,
    IOptions<PushOptions> options,
    TimeProvider timeProvider) : IPushSender
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<PushSendResult> SendAsync(PushSubscription subscription, string payload, CancellationToken cancellationToken = default)
    {
        var http = httpClientFactory.CreateClient("WebPush");

        var encrypted = WebPushEncryptor.Encrypt(subscription, payload);
        var request = new HttpRequestMessage(HttpMethod.Post, subscription.Endpoint)
        {
            Content = new ByteArrayContent(encrypted.Payload)
        };
        request.Content.Headers.TryAddWithoutValidation("Content-Encoding", "aesgcm");
        request.Headers.TryAddWithoutValidation("Encryption", $"salt={Base64Url(encrypted.Salt)}");
        request.Headers.TryAddWithoutValidation("Crypto-Key", $"dh={Base64Url(encrypted.PublicKey)}");
        request.Headers.TryAddWithoutValidation("TTL", "2419200");

        var jwt = GenerateVapidJwt(subscription.Endpoint);
        request.Headers.TryAddWithoutValidation("Authorization", $"vapid t={jwt},k={options.Value.PublicKey}");

        try
        {
            var response = await http.SendAsync(request, cancellationToken);
            return new PushSendResult(response.IsSuccessStatusCode, (int)response.StatusCode);
        }
        catch (HttpRequestException)
        {
            return new PushSendResult(false, null);
        }
    }

    private string GenerateVapidJwt(string endpoint)
    {
        var now = timeProvider.GetUtcNow();
        var claims = new
        {
            aud = new Uri(endpoint).GetLeftPart(UriPartial.Authority),
            exp = now.AddHours(12).ToUnixTimeSeconds(),
            sub = options.Value.Subject
        };

        string ToBase64Url(byte[] data) =>
            Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        var header = ToBase64Url(Encoding.UTF8.GetBytes(
            JsonSerializer.Serialize(new { typ = "JWT", alg = "ES256" })));
        var payload = ToBase64Url(Encoding.UTF8.GetBytes(
            JsonSerializer.Serialize(claims, JsonOptions)));

        var signingInput = $"{header}.{payload}";
        using var ecdsa = ECDsa.Create();
        ecdsa.ImportFromPem(options.Value.PrivateKey);
        var signature = ecdsa.SignData(
            Encoding.UTF8.GetBytes(signingInput),
            HashAlgorithmName.SHA256);

        return $"{signingInput}.{ToBase64Url(signature)}";
    }

    private static string Base64Url(byte[] data) =>
        Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
