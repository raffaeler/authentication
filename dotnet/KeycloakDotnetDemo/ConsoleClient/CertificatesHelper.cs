using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleClient;

public static class CertificatesHelper
{
    public const string ClientAuthenticationOid = "1.3.6.1.5.5.7.3.2";

    public static X509Certificate2 FindByClientAuth(string issuerFilter)
    {
        using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
        store.Open(OpenFlags.ReadOnly);

        var now = DateTime.Now;
        var results = store.Certificates
            .Where(x => now <= x.NotAfter && now >= x.NotBefore)
            .Where(x => x.FilterByEnhancedKeyUsageOid(ClientAuthenticationOid))
            .ToList();

        var cert = results.FirstOrDefault(c => c.Issuer.Contains(issuerFilter));

        return cert;
    }

    private static bool FilterByEnhancedKeyUsageOid(this X509Certificate2 certificate,
        string oidValue)
    {
        return certificate.Extensions.OfType<X509EnhancedKeyUsageExtension>()
            .SelectMany(c => c.EnhancedKeyUsages.OfType<Oid>())
            .Any(e => e.Value == oidValue);
    }
}
