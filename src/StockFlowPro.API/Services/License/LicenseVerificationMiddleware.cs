using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;

namespace StockFlowPro.API.Services.License
{
    public class LicenseVerificationMiddleware
    {
        private readonly RequestDelegate _next;

        public LicenseVerificationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(
            HttpContext context, 
            IConfiguration configuration,
            ILicenseCryptographyService cryptoService,
            IMachineIdProvider machineIdProvider)
        {
            var appMode = configuration["AppMode"] ?? "Offline";
            
            // If Online mode, skip license check completely.
            if (appMode.Equals("Online", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            var path = context.Request.Path.Value ?? string.Empty;

            // Only protect /api/ routes
            if (!path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            // Let activation and MachineID endpoints pass through
            if (path.StartsWith("/api/license", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            // Check License
            string licenseFilePath = Path.Combine(Directory.GetCurrentDirectory(), "license.key");
            if (!File.Exists(licenseFilePath))
            {
                context.Response.StatusCode = StatusCodes.Status402PaymentRequired;
                await context.Response.WriteAsJsonAsync(new { message = "License file missing. Please activate the application.", code = "LICENSE_MISSING" });
                return;
            }

            string licenseKey = await File.ReadAllTextAsync(licenseFilePath);
            string currentMachineId = machineIdProvider.GetMachineId();

            if (!cryptoService.VerifyLicense(licenseKey.Trim(), currentMachineId))
            {
                context.Response.StatusCode = StatusCodes.Status402PaymentRequired;
                await context.Response.WriteAsJsonAsync(new { message = "Invalid license key for this machine.", code = "LICENSE_INVALID" });
                return;
            }

            await _next(context);
        }
    }
}
