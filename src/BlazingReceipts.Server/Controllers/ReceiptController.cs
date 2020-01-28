using BlazingReceipts.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace BlazingReceipts.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ReceiptController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ReceiptController> _logger;
        private Microsoft.WindowsAzure.Storage.CloudStorageAccount _queueStorageAccount { get; set; }
        private CloudQueue _receiptQueue { get; set; }
        private Microsoft.Azure.Cosmos.Table.CloudTable _receiptTable { get; set; }

        public ReceiptController(ILogger<ReceiptController> logger, IConfiguration configuration)
        {
            this._logger = logger;
            _configuration = configuration;
            _queueStorageAccount = Microsoft.WindowsAzure.Storage.CloudStorageAccount.Parse(_configuration["Azure:Storage:ConnectionString"]);
            _receiptQueue = _queueStorageAccount.CreateCloudQueueClient().GetQueueReference("receipts");

            var tableStorageAccount = Microsoft.Azure.Cosmos.Table.CloudStorageAccount.Parse(_configuration["Azure:Storage:ConnectionString"]);
            _receiptTable = tableStorageAccount.CreateCloudTableClient().GetTableReference(Constants.TABLE_NAME);
        }

        [HttpGet()]
        public async Task<IEnumerable<Receipt>> Get(int dayOffset)
        {
            string partitionFilter = DateTime.Now.AddDays(dayOffset).ToString("yyyyMMdd");

            List<Receipt> result = new List<Receipt>();
            var entities = new List<TableEntityAdapter<Receipt>>();
            var query = new TableQuery<TableEntityAdapter<Receipt>>()
                .Where(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionFilter)
                );

            TableContinuationToken token = null;

            var tableResults = await _receiptTable.ExecuteQuerySegmentedAsync(query, token);
            if (tableResults.Results.Any())
            {
                // Get a SAS container token for accessing all the blob files
                var container = _queueStorageAccount.CreateCloudBlobClient().GetContainerReference(Constants.BLOB_CONTAINER_NAME);
                var sasToken = container.GetSharedAccessSignature(new SharedAccessBlobPolicy()
                {
                    Permissions = SharedAccessBlobPermissions.Read,
                    SharedAccessExpiryTime = DateTimeOffset.Now.AddHours(1)
                });

                tableResults.Results.ForEach(e =>
                {
                    e.OriginalEntity.Url = $"{e.OriginalEntity.BlobUrl}{sasToken}";
                    result.Add(e.OriginalEntity);
                });
            }
            return result;
        }

        /// <summary>
        /// Save the uploaded file straight to blob
        /// Add a request to the Receipt Worker queue for OCR
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult<Receipt>> Post()
        {
            if (Request.Body != null)
            {
                var scanRequest = new Receipt();
                var container = _queueStorageAccount.CreateCloudBlobClient().GetContainerReference(Constants.BLOB_CONTAINER_NAME);
                if (!await container.ExistsAsync()) await container.CreateIfNotExistsAsync();

                CloudBlockBlob cloudBlockBlob = container.GetBlockBlobReference(scanRequest.FileName);
                scanRequest.BlobUrl = cloudBlockBlob.StorageUri.PrimaryUri.ToString();

                string blobAccessURL = cloudBlockBlob.GetSharedAccessSignature(new SharedAccessBlobPolicy()
                {
                    Permissions = SharedAccessBlobPermissions.Read,
                    SharedAccessExpiryTime = DateTimeOffset.Now.AddHours(1)
                });
                scanRequest.Url = $"{scanRequest.BlobUrl}{blobAccessURL}";

                await cloudBlockBlob.UploadFromStreamAsync(Request.Body);

                _receiptQueue = _queueStorageAccount.CreateCloudQueueClient().GetQueueReference(Constants.QUEUE_NAME);
                if (!await _receiptQueue.ExistsAsync()) await _receiptQueue.CreateIfNotExistsAsync();

                CloudQueueMessage message = new CloudQueueMessage(JsonSerializer.Serialize(scanRequest));
                await _receiptQueue.AddMessageAsync(message);
                return scanRequest;
            }
            return new BadRequestResult();
        }
    }
}
