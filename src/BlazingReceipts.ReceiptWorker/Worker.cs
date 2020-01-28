using BlazingReceipts.ReceiptWorker.Hubs;
using BlazingReceipts.Shared;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Recognizers.Text;
using Microsoft.Recognizers.Text.DateTime;
using Microsoft.Recognizers.Text.Number;
using Microsoft.WindowsAzure.Storage.Queue;
using System;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Constants = BlazingReceipts.Shared.Constants;

namespace BlazingReceipts.ReceiptWorker
{
    public class Worker : BackgroundService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHubContext<OCRStatusHub> _hubContext;
        private Microsoft.WindowsAzure.Storage.CloudStorageAccount _storageAccount { get; set; }
        private CloudQueue _receiptQueue { get; set; }
        private Microsoft.Azure.Cosmos.Table.CloudTable _receiptTable { get; set; }
        private int _pollInterval { get; set; }

        public Worker(IHttpClientFactory _clientFactory, ILogger<Worker> logger, IConfiguration configuration, IHubContext<OCRStatusHub> hubContext)
        {
            _logger = logger;
            _configuration = configuration;
            _hubContext = hubContext;
            _httpClient = _clientFactory.CreateClient("cs");
            _httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _configuration["FormRecognizerKey"]);
            _pollInterval = Convert.ToInt32(_configuration["OCRPollInterval"]);
        }

        public override async Task StartAsync(CancellationToken stoppingToken)
        {
            _storageAccount = Microsoft.WindowsAzure.Storage.CloudStorageAccount.Parse(_configuration["Azure:Storage:ConnectionString"]);
            _receiptQueue = _storageAccount.CreateCloudQueueClient().GetQueueReference(Constants.QUEUE_NAME);
            if (!await _receiptQueue.ExistsAsync()) await _receiptQueue.CreateIfNotExistsAsync();

            var cosmosAccount = Microsoft.Azure.Cosmos.Table.CloudStorageAccount.Parse(_configuration["Azure:Storage:ConnectionString"]);
            _receiptTable = cosmosAccount.CreateCloudTableClient().GetTableReference(Constants.TABLE_NAME);

            if (!await _receiptTable.ExistsAsync()) await _receiptTable.CreateIfNotExistsAsync();
            await base.StartAsync(stoppingToken);
        }

        /// <summary>
        /// Here we are only processing one message at a time, this sample follows a happy path scenario
        /// there is no status code/error handling or dead lettering of messages
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Receipt worker running at: {time}", DateTimeOffset.Now);
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var message = await _receiptQueue.GetMessageAsync(null, null, null, stoppingToken);
                    if (message != null)
                    {
                        await ProcessMessage(message, stoppingToken);
                        await _receiptQueue.DeleteMessageAsync(message.Id, message.PopReceipt, null, null, stoppingToken);
                    }
                }
                catch (Exception exp)
                {
                    _logger.LogError(exp.Message);
                }
            }
        }

        /// <summary>
        /// Submit the receipt understanding job to the Forms Recognizer Cognitive Service
        /// The service will return the endpoint (operation-location) to monitor for the result
        /// </summary>
        /// <param name="message"></param>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        private async Task ProcessMessage(CloudQueueMessage message, CancellationToken stoppingToken)
        {
            Receipt receiptRequest = JsonSerializer.Deserialize<Receipt>(message.AsString);
            if (receiptRequest != null)
            {
                var ocrRequest = new OCRRequest() { Source = receiptRequest.Url };
                using var content = new StringContent(JsonSerializer.Serialize(ocrRequest), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_configuration["FormRecognizerEndpoint"]}analyze", content);

                if (!response.IsSuccessStatusCode)
                {
                    var serviceError = JsonSerializer.Deserialize<OCRError>(await response.Content.ReadAsStringAsync());
                    _logger.LogError($"OCR Request status code: {response.StatusCode} : Error Code: {serviceError.error.code}, Error Message: {serviceError.error.message}");
                }
                else
                {
                    var requestUrl = ((string[])response.Headers.FirstOrDefault(n => n.Key.Equals("Operation-Location")).Value)[0];
                    receiptRequest.OCRRequestUrl = requestUrl;
                    await PollForCompletion(receiptRequest, stoppingToken);
                }
            }
        }

        /// <summary>
        /// Wait for the Forms Recognizer Cognitive Service to complete
        /// Polly is used on the httpclient to handle service throttling/back-off
        /// SignalR used for pushing out updates to all users
        /// </summary>
        /// <param name="receipt"></param>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        private async Task PollForCompletion(Receipt receipt, CancellationToken stoppingToken)
        {
            // Don't poll too aggressively otherwise we'll get throttled
            await Task.Delay(_pollInterval);

            var ocrResult = await ReceiptAnalysisResult(receipt);
            if (ocrResult != null)
            {
                // TODO: Probably want to filter these based on user!
                await _hubContext.Clients.All.SendAsync("ReceiveMessage", ocrResult, stoppingToken);

                if (!ocrResult.OCRComplete)
                {
                    await PollForCompletion(receipt, stoppingToken);
                }
                else
                {
                    await SaveReceiptResult(receipt);
                }
            }
        }

        /// <summary>
        /// Check the Forms Recognizer response - once complete map it to a POCO
        /// </summary>
        /// <param name="receipt"></param>
        /// <returns></returns>
        private async Task<Receipt> ReceiptAnalysisResult(Receipt receipt)
        {
            _logger.LogInformation($"Getting receipt analysis result: {receipt.Id}");
            using HttpResponseMessage response = await _httpClient.GetAsync(receipt.OCRRequestUrl);

            var ocrResponse = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                var serviceError = JsonSerializer.Deserialize<OCRError>(ocrResponse);
                _logger.LogError($"OCR Response status code: {response.IsSuccessStatusCode} : Error Code: {serviceError.error.code}, Error Message: {serviceError.error.message}");
                receipt.Status = serviceError.error.message;
                return receipt;
            }

            try
            {
                var ocrResult = JsonSerializer.Deserialize<OCRResult>(ocrResponse);
                receipt.Status = ocrResult.status;
                _logger.LogInformation($"OCR response status: {receipt.Status}");

                if (receipt.Status.Equals(Constants.OCRServiceOperationStatus.Succeeded.ToString(), StringComparison.InvariantCultureIgnoreCase) ||
                    receipt.Status.Equals(Constants.OCRServiceOperationStatus.Failed.ToString(), StringComparison.InvariantCultureIgnoreCase))
                {
                    receipt.OCRComplete = true;
                }

                if (ocrResult.analyzeResult?.documentResults?.Length > 0)
                {
                    // Assuming only 1 receipt per scan
                    var ocrReceipt = ocrResult.analyzeResult?.documentResults.FirstOrDefault();

                    receipt.RequestComplete = DateTime.Now;
                    receipt.Merchant = ocrReceipt.fields.MerchantName?.text;
                    receipt.Total = Convert.ToDouble(ocrReceipt.fields.Total?.valueNumber);
                    receipt.SubTotal = Convert.ToDouble(ocrReceipt.fields.Subtotal?.valueNumber);
                    receipt.Tax = Convert.ToDouble(ocrReceipt.fields.Tax?.valueNumber);
                    receipt.Tip = Convert.ToDouble(ocrReceipt.fields.Tip?.valueNumber);
                    //receipt.Address = ocrReceipt.fields.MerchantAddress?.text;

                    if (ocrReceipt.fields.TransactionTime != null &&
                        DateTime.TryParse(ocrReceipt.fields.TransactionTime.text, out DateTime time))
                    {
                        receipt.Time = time;
                    }

                    if (ocrReceipt.fields.TransactionDate != null)
                    {
                        // Dates could come in a variety of different formats and styles
                        var recognizerDate = DateTimeRecognizer.RecognizeDateTime(ocrReceipt.fields.TransactionDate.text, Culture.English);
                        if (recognizerDate.Any())
                        {
                            var recognizerValues = (List<Dictionary<string, string>>)recognizerDate.First().Resolution["values"];
                            if (recognizerValues.Any())
                            {
                                if (recognizerValues[0].TryGetValue("value", out string v))
                                {
                                    DateTime.TryParse(v, out DateTime outputDate);
                                    receipt.Date = outputDate;
                                }
                            }
                        }
                    }

                    // Extract line items
                    if (ocrReceipt.fields.Items?.valueArray?.Length > 0)
                    {
                        receipt.Items = new LineItem[ocrReceipt.fields.Items.valueArray.Length];
                        for (int i = 0; i < ocrReceipt.fields.Items.valueArray.Length; i++)
                        {
                            LineItem lineItem = new LineItem
                            {
                                Item = ocrReceipt.fields.Items?.valueArray[i].valueObject?.Name?.text,
                            };

                            if (ocrReceipt.fields.Items?.valueArray[i].valueObject?.Quantity?.text != null)
                            {
                                var quantityRecognizer = NumberRecognizer.RecognizeNumber(ocrReceipt.fields.Items.valueArray[i].valueObject.Quantity.text, Culture.English);
                                if (quantityRecognizer.Any())
                                {
                                    var recognizerValue = quantityRecognizer.FirstOrDefault().Resolution["value"];
                                    if (recognizerValue != null)
                                    {
                                        double.TryParse(recognizerValue.ToString(), out double outputQuantity);
                                        lineItem.Quantity = outputQuantity;
                                    }
                                }
                            }

                            // Total price could include currency symbol
                            if (ocrReceipt.fields.Items?.valueArray[i].valueObject?.TotalPrice?.text != null)
                            {
                                var numberRecognizer = NumberRecognizer.RecognizeNumber(ocrReceipt.fields.Items.valueArray[i].valueObject.TotalPrice.text, Culture.English);
                                if (numberRecognizer.Any())
                                {
                                    var recognizerValue = numberRecognizer.FirstOrDefault().Resolution["value"];
                                    if (recognizerValue != null)
                                    {
                                        double.TryParse(recognizerValue.ToString(), out double outputTotal);
                                        lineItem.TotalPrice = outputTotal;
                                    }
                                }
                            }
                            receipt.Items[i] = lineItem;
                        }
                    }
                    _logger.LogInformation($"Successful OCR receipt extraction Id: {receipt.Id} Total: {receipt.Total}");
                }
            }
            catch (Exception exp)
            {
                _logger.LogError($"Error deserialising the OCR response: {exp.Message}");
                receipt.Status = exp.Message;
            }
            return receipt;
        }

        /// <summary>
        /// Stuff the result in table storage
        /// </summary>
        /// <param name="receipt"></param>
        /// <returns></returns>
        private async Task SaveReceiptResult(Receipt receipt)
        {
            if (receipt != null)
            {
                receipt.Url = null; // remove as this has the SAS token which will expire
                var entity = new TableEntityAdapter<Receipt>(receipt, receipt.RequestReceived.ToString("yyyyMMdd"), receipt.Id);
                var insert = Microsoft.Azure.Cosmos.Table.TableOperation.InsertOrMerge(entity);

                try
                {
                    _logger.LogInformation($"Saving receipt to table storage: Id: {receipt.Id} Total: {receipt.Total}");
                    var res = await _receiptTable.ExecuteAsync(insert);
                }
                catch (Exception exp)
                {
                    _logger.LogError($"Error saving to table storage: {exp.Message}");
                }
            }
        }
    }
}
