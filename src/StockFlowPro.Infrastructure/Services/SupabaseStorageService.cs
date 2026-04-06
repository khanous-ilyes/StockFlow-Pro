using Amazon.S3;
using Amazon.S3.Transfer;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using StockFlowPro.Application.Interfaces;
using System.IO;
using System.Threading.Tasks;
using System;

namespace StockFlowPro.Infrastructure.Services
{
    public class SupabaseStorageService : IFileStorageService
    {
        private readonly IAmazonS3 _s3Client;
        private readonly string _bucketName;
        private readonly string _storageUrl;

        public SupabaseStorageService(IConfiguration configuration)
        {
            var config = configuration.GetSection("Supabase");
            var serviceUrl = config["S3Endpoint"];
            var accessKey = config["AccessKey"];
            var secretKey = config["SecretKey"];
            _bucketName = config["BucketName"];
            _storageUrl = config["StorageUrl"];

            var awsConfig = new AmazonS3Config
            {
                ServiceURL = serviceUrl,
                ForcePathStyle = true 
            };

            _s3Client = new AmazonS3Client(accessKey, secretKey, awsConfig);
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
        {
            var uniqueFileName = $"{Guid.NewGuid()}-{fileName}";
            var fileTransferUtility = new TransferUtility(_s3Client);
            
            var uploadRequest = new TransferUtilityUploadRequest
            {
                InputStream = fileStream,
                Key = uniqueFileName,
                BucketName = _bucketName,
                ContentType = contentType,
                CannedACL = S3CannedACL.PublicRead // Making file publicly readable
            };

            await fileTransferUtility.UploadAsync(uploadRequest);

            // Construct the public URL
            // Supabase formatted storage URL mapping
            return $"{_storageUrl}/object/public/{_bucketName}/{uniqueFileName}";
        }

        public async Task DeleteFileAsync(string fileUrl)
        {
            if (string.IsNullOrEmpty(fileUrl)) return;
            
            var uri = new Uri(fileUrl);
            var key = Path.GetFileName(uri.LocalPath);

            var deleteRequest = new DeleteObjectRequest
            {
                BucketName = _bucketName,
                Key = key
            };
            
            await _s3Client.DeleteObjectAsync(deleteRequest);
        }
    }
}
