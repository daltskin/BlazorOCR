using System.Text.Json.Serialization;

namespace BlazingReceipts.Shared
{
    public class OCRRequest
    {
        [JsonPropertyName("source")]
        public string Source { get; set; }
    }
}
