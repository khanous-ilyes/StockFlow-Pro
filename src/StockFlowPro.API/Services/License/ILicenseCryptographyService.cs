namespace StockFlowPro.API.Services.License
{
    public interface ILicenseCryptographyService
    {
        bool VerifyLicense(string licenseKey, string machineId);
    }
}
