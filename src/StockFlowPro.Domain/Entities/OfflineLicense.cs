using System;

namespace StockFlowPro.Domain.Entities
{
    public class OfflineLicense
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string CustomerName { get; set; } = string.Empty;
        public string MachineId { get; set; } = string.Empty;
        public string LicenseKey { get; set; } = string.Empty;
        public DateTime? ExpiryDate { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
