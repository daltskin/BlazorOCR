using System;
using System.Collections.Generic;

namespace BlazingReceipts.Shared
{
    public class RequestIDComparer: IEqualityComparer<Receipt>
    {
        public bool Equals(Receipt x, Receipt y)
        {
            if (string.Equals(x.Id, y.Id, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            return false;
        }

        public int GetHashCode(Receipt obj)
        {
            return obj.Id.GetHashCode();
        }
    }
}
