using System;
using System.Collections.Generic;

namespace BlazingReceipts.Shared
{
    public class Receipt
    {
        public Receipt()
        {
            Id = Guid.NewGuid().ToString();
            Status = "Received";
            RequestReceived = DateTime.Now;
        }

        public Receipt(string guid, DateTime requestedDate, string status)
        {
            Id = guid;
            RequestReceived = requestedDate;
            Status = status;
            Error = null;
        }

        public string Id { get; set; }
        public DateTime RequestReceived { get; set; }
        public DateTime RequestComplete { get; set; }
        public string OCRRequestUrl { get; set; }
        public string FileName => $"{Id}.jpg";
        public string BlobUrl { get; set; }
        public string Url { get; set; }
        public DateTime? Date { get; set; }
        public DateTime? Time { get; set; }
        public double SubTotal { get; set; }
        public double Total { get; set; }
        public double Tip { get; set; }
        public double Tax { get; set; }
        public string Merchant { get; set; }
        public LineItem[] Items { get; set; }
        public string Address { get; set; }
        public string Status { get; set; }
        public bool OCRComplete { get; set; }

        public string Error { get; set; }
    }

    public class LineItem
    {
        public double Quantity { get; set; }
        public string Item { get; set; }
        public double Price { get; set; }
        public double TotalPrice { get; set; }
    }
}
