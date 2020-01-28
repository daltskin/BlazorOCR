using BlazingReceipts.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace BlazingReceipts.Client.Services
{
    public class ReceiptService : IReceiptService
    {
        private readonly HttpClient _httpClient;

        public ReceiptService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<IEnumerable<Receipt>> GetReceiptsAsync(int dayOffset)
        {
            IEnumerable<Receipt> receipts = null;
            var response = await _httpClient.GetAsync($"Receipt?dayOffset={dayOffset}");
            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                receipts = JsonSerializer.Deserialize<IEnumerable<Receipt>>(responseJson);
            }
            return receipts;
        }

        public async Task<Receipt> PostReceiptAsync(Stream file)
        {
            Receipt receiptRequest = new Receipt();
            using (HttpContent fileStreamContent = new StreamContent(file))
            {
                var response = await _httpClient.PostAsync("Receipt", fileStreamContent);
                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    receiptRequest = JsonSerializer.Deserialize<Receipt>(responseJson);
                }
                else
                {
                    receiptRequest.Error = $"Error: {await response.Content.ReadAsStringAsync()}";
                }
            }
            return receiptRequest;
        }
    }
}
