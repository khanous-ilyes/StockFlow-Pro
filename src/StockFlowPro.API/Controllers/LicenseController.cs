using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Threading.Tasks;

namespace StockFlowPro.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LicenseController : ControllerBase
    {
        private readonly Services.License.IMachineIdProvider _machineIdProvider;
        private readonly Services.License.ILicenseCryptographyService _cryptoService;

        public LicenseController(
            Services.License.IMachineIdProvider machineIdProvider,
            Services.License.ILicenseCryptographyService cryptoService)
        {
            _machineIdProvider = machineIdProvider;
            _cryptoService = cryptoService;
        }

        [HttpGet("machine-id")]
        public IActionResult GetMachineId()
        {
            return Ok(new { machineId = _machineIdProvider.GetMachineId() });
        }

        [HttpPost("activate")]
        public async Task<IActionResult> Activate([FromBody] ActivateRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.LicenseKey))
            {
                return BadRequest("License key is required.");
            }

            string currentMachineId = _machineIdProvider.GetMachineId();

            if (!_cryptoService.VerifyLicense(request.LicenseKey.Trim(), currentMachineId))
            {
                return BadRequest("Invalid license key for this machine.");
            }

            // Save the valid license key
            string licenseFilePath = Path.Combine(Directory.GetCurrentDirectory(), "license.key");
            await System.IO.File.WriteAllTextAsync(licenseFilePath, request.LicenseKey.Trim());

            return Ok(new { message = "License activated successfully!" });
        }
    }

    public class ActivateRequest
    {
        public string LicenseKey { get; set; } = string.Empty;
    }
}
