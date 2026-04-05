using System;
using System.Management;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;

namespace StockFlowPro.API.Services.License
{
    public class WindowsMachineIdProvider : IMachineIdProvider
    {
        public string GetMachineId()
        {
            if (!OperatingSystem.IsWindows())
            {
                return HashString(Environment.MachineName);
            }

            return GetWindowsMachineId();
        }

        [SupportedOSPlatform("windows")]
        private string GetWindowsMachineId()
        {
            try
            {
                string cpuId = GetWmiPropertyValue("Win32_Processor", "ProcessorId");
                string biosId = GetWmiPropertyValue("Win32_BIOS", "SerialNumber");
                string boardId = GetWmiPropertyValue("Win32_BaseBoard", "SerialNumber");

                string rawId = $"{cpuId}|{biosId}|{boardId}";
                return HashString(rawId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving Machine ID: {ex.Message}");
                return HashString(Environment.MachineName + Environment.UserName);
            }
        }

        [SupportedOSPlatform("windows")]
        private string GetWmiPropertyValue(string wmiClass, string property)
        {
            string result = "";
            try
            {
                using var searcher = new ManagementObjectSearcher($"SELECT {property} FROM {wmiClass}");
                foreach (ManagementObject obj in searcher.Get())
                {
                    result = obj[property]?.ToString()?.Trim() ?? "";
                    if (!string.IsNullOrEmpty(result)) break;
                }
            }
            catch
            {
                // Ignored
            }
            return result;
        }

        private string HashString(string input)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = sha256.ComputeHash(bytes);
            var base64 = Convert.ToBase64String(hash);
            
            var clean = base64.Replace("+", "").Replace("/", "").Replace("=", "").ToUpper();
            if (clean.Length > 16) clean = clean.Substring(0, 16);
            
            return $"MACH-{clean.Substring(0, 4)}-{clean.Substring(4, 4)}-{clean.Substring(8, 4)}-{clean.Substring(12, 4)}";
        }
    }
}
