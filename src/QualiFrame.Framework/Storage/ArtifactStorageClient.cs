using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Azure.Storage.Blobs;
using QualiFrame.Framework.Configuration;
using QualiFrame.Framework.Utilities.Logging;

namespace QualiFrame.Framework.Storage
{
    /// <summary>
    /// Opt-in upload of report/screenshot artifacts to S3 or Azure Blob.
    /// Never reads secret values from the manifest; uses configuration/environment only.
    /// </summary>
    public static class ArtifactStorageClient
    {
        public static bool IsS3Configured()
        {
            return !string.IsNullOrWhiteSpace(TestConfiguration.Instance.S3Bucket);
        }

        public static bool IsAzureBlobConfigured()
        {
            var config = TestConfiguration.Instance;
            return !string.IsNullOrWhiteSpace(config.AzureBlobConnectionString)
                   && !string.IsNullOrWhiteSpace(config.AzureBlobContainer);
        }

        public static async Task<string> UploadToS3Async(string filePath, string? objectKey = null, CancellationToken cancellationToken = default)
        {
            if (!IsS3Configured())
            {
                throw new InvalidOperationException("S3 is not configured. Set Storage:S3:Bucket.");
            }

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Artifact file was not found.", filePath);
            }

            var key = objectKey ?? Path.GetFileName(filePath);
            var region = RegionEndpoint.GetBySystemName(TestConfiguration.Instance.S3Region);
            using var client = new AmazonS3Client(region);
            await client.PutObjectAsync(new PutObjectRequest
            {
                BucketName = TestConfiguration.Instance.S3Bucket,
                Key = key,
                FilePath = filePath
            }, cancellationToken);

            Logger.Information("Uploaded artifact to s3://{Bucket}/{Key}", TestConfiguration.Instance.S3Bucket, key);
            return key;
        }

        public static async Task<string> UploadToAzureBlobAsync(string filePath, string? blobName = null, CancellationToken cancellationToken = default)
        {
            if (!IsAzureBlobConfigured())
            {
                throw new InvalidOperationException("Azure Blob is not configured. Set Storage:AzureBlob:ConnectionString and Container.");
            }

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Artifact file was not found.", filePath);
            }

            var name = blobName ?? Path.GetFileName(filePath);
            var container = new BlobContainerClient(TestConfiguration.Instance.AzureBlobConnectionString, TestConfiguration.Instance.AzureBlobContainer);
            await container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
            var blob = container.GetBlobClient(name);
            await blob.UploadAsync(filePath, overwrite: true, cancellationToken);
            Logger.Information("Uploaded artifact to Azure Blob {Blob}", name);
            return blob.Uri.ToString();
        }
    }
}
