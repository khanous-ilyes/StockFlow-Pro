using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace StockFlowPro.LicenseGenerator
{
    public class LicensePayload
    {
        public string MachineId { get; set; } = string.Empty;
        public DateTime? ExpirationDate { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("========================================");
            Console.WriteLine("    StockFlow Pro License Generator     ");
            Console.WriteLine("========================================");
            Console.WriteLine();

            while (true)
            {
                Console.WriteLine("1. Generate New RSA Key Pair (Public/Private)");
                Console.WriteLine("2. Generate License Key for a Machine");
                Console.WriteLine("3. Exit");
                Console.Write("Select an option: ");
                
                var choice = Console.ReadLine();
                if (choice == "1") GenerateKeyPair();
                else if (choice == "2") GenerateLicense();
                else if (choice == "3") break;
                
                Console.WriteLine();
            }
        }

        static void GenerateKeyPair()
        {
            using var rsa = RSA.Create(2048);
            
            var privateKeyBytes = rsa.ExportRSAPrivateKey();
            var publicKeyBytes = rsa.ExportRSAPublicKey();

            var privateKeyBase64 = Convert.ToBase64String(privateKeyBytes);
            var publicKeyBase64 = Convert.ToBase64String(publicKeyBytes);

            Console.WriteLine("\n--- NEW RSA KEY PAIR ---");
            Console.WriteLine("\n[PRIVATE KEY] (Save this securely! Do NOT share):");
            Console.WriteLine(privateKeyBase64);
            Console.WriteLine("\n[PUBLIC KEY] (Put this in StockFlowPro.API appsettings.json):");
            Console.WriteLine(publicKeyBase64);
            Console.WriteLine("------------------------\n");
        }

        static void GenerateLicense()
        {
            Console.Write("\nEnter Machine ID provided by the customer: ");
            string machineId = Console.ReadLine()?.Trim() ?? "";

            if (string.IsNullOrEmpty(machineId))
            {
                Console.WriteLine("Machine ID is required.");
                return;
            }

            Console.Write("Enter expiration date (YYYY-MM-DD) or press Enter for no expiration: ");
            string expInput = Console.ReadLine()?.Trim() ?? "";
            
            DateTime? expirationDate = null;
            if (DateTime.TryParse(expInput, out var parsedDate))
            {
                expirationDate = parsedDate;
            }

            Console.WriteLine("\nEnter the Private Key Base64 string:");
            string privateKeyBase64 = Console.ReadLine()?.Trim() ?? "";

            if (string.IsNullOrEmpty(privateKeyBase64))
            {
                Console.WriteLine("Private Key is required.");
                return;
            }

            try
            {
                using var rsa = RSA.Create();
                rsa.ImportRSAPrivateKey(Convert.FromBase64String(privateKeyBase64), out _);

                var payload = new LicensePayload
                {
                    MachineId = machineId,
                    ExpirationDate = expirationDate
                };

                string jsonPayload = JsonSerializer.Serialize(payload);
                byte[] payloadBytes = Encoding.UTF8.GetBytes(jsonPayload);
                
                // Sign data
                byte[] signatureBytes = rsa.SignData(
                    payloadBytes,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);

                string payloadBase64 = Convert.ToBase64String(payloadBytes);
                string signatureBase64 = Convert.ToBase64String(signatureBytes);

                string finalLicenseKey = $"{payloadBase64}.{signatureBase64}";

                Console.WriteLine("\n--- GENERATED LICENSE KEY ---");
                Console.WriteLine("Send this string to the customer:\n");
                Console.WriteLine(finalLicenseKey);
                Console.WriteLine("-----------------------------\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nError generating license: {ex.Message}");
            }
        }
    }
}
