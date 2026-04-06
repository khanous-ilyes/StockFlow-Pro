using System.IO;
using System.Threading.Tasks;

namespace StockFlowPro.Application.Interfaces
{
    public interface IFileStorageService
    {
        Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType);
        Task DeleteFileAsync(string fileUrl);
    }
}
