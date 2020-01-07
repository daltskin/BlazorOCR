using System.ComponentModel;

namespace BlazingReceipts.Shared
{
    public static class Constants
    {
        public const string BLOB_CONTAINER_NAME = "receipts";
        public const string QUEUE_NAME = "receipts";
        public const string TABLE_NAME = "receipts";

        public enum OCRServiceOperationStatus
        {
            [Description("Succeeded")]
            Succeeded = 0,
            [Description("Failed")]
            Failed = 1,
            [Description("NotStarted")]
            NotStarted = 2,
            [Description("Running")]
            Running = 3
        }
    }
}
