#nullable enable

using System.Configuration;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace SecureChat.Client.Services.Api;

/// <summary>
/// Creates HTTP handlers that validate server certificates signed by the SecureChat CA.
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
        if (certificate is null)
        {
            return false;
        }

        if ((errors & SslPolicyErrors.RemoteCertificateNameMismatch) != 0)
        {
            return false;
        }

        if (errors == SslPolicyErrors.None)
        {
            return true;
        }

        var caCertificate = LoadCaCertificate();
        if (caCertificate is null)
        {
            return false;
        }

        using (caCertificate)
        using (var customChain = new X509Chain())
        {
            customChain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
            customChain.ChainPolicy.CustomTrustStore.Add(caCertificate);
            customChain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            customChain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;
            return customChain.Build(certificate);
        }
    }

    private static X509Certificate2? LoadCaCertificate()
    {
        var configuredPath = ConfigurationManager.AppSettings["ServerCaCertificateFile"];
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            return null;
        }

        var path = Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, configuredPath));

        return File.Exists(path) ? new X509Certificate2(path) : null;
    }
}
