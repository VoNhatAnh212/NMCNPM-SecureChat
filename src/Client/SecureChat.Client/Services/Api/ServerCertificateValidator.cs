#nullable enable

using System.Configuration;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace SecureChat.Client.Services.Api;

/// <summary>
/// Creates HTTP handlers that validate trusted server certificates or a pinned development certificate.
/// </summary>
public static class ServerCertificateValidator
{
    /// <summary>Creates a handler with SecureChat certificate validation.</summary>
    public static HttpClientHandler CreateHttpClientHandler()
    {
        return new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = Validate
        };
    }

    private static bool Validate(HttpRequestMessage request, X509Certificate2? certificate, X509Chain? chain, SslPolicyErrors errors)
    {
        if (errors == SslPolicyErrors.None)
        {
            return true;
        }

        if (certificate is null || request.RequestUri?.Host is not "localhost" and not "127.0.0.1" and not "::1")
        {
            return false;
        }

        var expectedHash = ReadExpectedCertificateHash();
        if (string.IsNullOrWhiteSpace(expectedHash))
        {
            return false;
        }

        var actualHash = Convert.ToHexString(SHA256.HashData(certificate.RawData));
        return string.Equals(actualHash, NormalizeHash(expectedHash), StringComparison.OrdinalIgnoreCase);
    }

    private static string ReadExpectedCertificateHash()
    {
        var configuredHash = ConfigurationManager.AppSettings["ServerCertificateSha256"];
        if (!string.IsNullOrWhiteSpace(configuredHash))
        {
            return configuredHash;
        }

        var configuredPath = ConfigurationManager.AppSettings["ServerCertificateSha256File"];
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            return string.Empty;
        }

        var path = Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, configuredPath));

        return File.Exists(path) ? File.ReadAllText(path) : string.Empty;
    }

    private static string NormalizeHash(string hash)
    {
        return hash.Replace(":", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Trim();
    }
}
