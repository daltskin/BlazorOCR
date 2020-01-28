using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace BlazingReceipts.Shared
{
    public interface IReceiptService
    {
        Task<IEnumerable<Receipt>> GetReceiptsAsync(int dayOffset);
        Task<Receipt> PostReceiptAsync(Stream file);
    }
}