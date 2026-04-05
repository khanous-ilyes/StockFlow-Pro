using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace StockFlowPro.API.Services.License
{
    public class LicensePayload
    {
        public string MachineId { get; set; } = string.Empty;
        public DateTime? ExpirationDate { get; set; }
    }

    public class LicenseCryptographyService : ILicenseCryptographyService
    {
        private readonly string _publicKeyBase64;

        public LicenseCryptographyService(IConfiguration configuration)
        {
            _publicKeyBase64 = configuration["LicenseSettings:PublicKey"] 
                ?? string.Empty;
        }

        public bool VerifyLicense(string licenseKey, string expectedMachineId)
        {
            if (string.IsNullOrEmpty(licenseKey) || string.IsNullOrEmpty(_publicKeyBase64))
                return false;

            try
            {
                // Format: PayloadBase64.SignatureBase64
                var parts = licenseKey.Split('.');
                if (parts.Length != 2) return false;

                string payloadBase64 = parts[0];
                string signatureBase64 = parts[1];

                byte[] payloadBytes = Convert.FromBase64String(payloadBase64);
                byte[] signatureBytes = Convert.FromBase64String(signatureBase64);

                using var rsa = RSA.Create();
                rsa.ImportRSAPublicKey(Convert.FromBase64String(_publicKeyBase64), out _);

                bool isSignatureValid = rsa.VerifyData(
                    payloadBytes, 
                    signatureBytes, 
                    HashAlgorithmName.SHA256, 
                    RSASignaturePadding.Pkcs1);

                if (!isSignatureValid) return false;

                string jsonPayload = Encoding.UTF8.GetString(payloadBytes);
                var payload = JsonSerializer.Deserialize<LicensePayload>(jsonPayload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (payload == null || payload.MachineId != expectedMachineId) 
                    return false;

                if (payload.ExpirationDate.HasValue && payload.ExpirationDate.Value < DateTime.UtcNow)
                    return false;

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
