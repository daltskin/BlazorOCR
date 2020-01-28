using System;

namespace BlazingReceipts.Shared
{

    public class OCRResult
    {
        public string status { get; set; }
        public DateTime createdDateTime { get; set; }
        public DateTime lastUpdatedDateTime { get; set; }
        public Analyzeresult analyzeResult { get; set; }
    }

    public class Analyzeresult
    {
        public string version { get; set; }
        public Readresult[] readResults { get; set; }
        public Documentresult[] documentResults { get; set; }
    }

    public class Readresult
    {
        public int page { get; set; }
        public float angle { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        public string unit { get; set; }
        public string language { get; set; }
    }

    public class Documentresult
    {
        public string docType { get; set; }
        public int[] pageRange { get; set; }
        public Fields fields { get; set; }
    }

    public class Fields
    {
        public Receipttype ReceiptType { get; set; }
        public Merchantname MerchantName { get; set; }
        public Transactiondate TransactionDate { get; set; }
        public Transactiontime TransactionTime { get; set; }
        public Items Items { get; set; }
        public Subtotal Subtotal { get; set; }
        public Tax Tax { get; set; }
        public Total Total { get; set; }
        public Tip Tip{ get; set; }
    }

    public class Receipttype
    {
        public string type { get; set; }
        public string valueString { get; set; }
        public float confidence { get; set; }
    }

    public class Merchantname
    {
        public string type { get; set; }
        public string valueString { get; set; }
        public string text { get; set; }
        public float[] boundingBox { get; set; }
        public int page { get; set; }
        public float confidence { get; set; }
    }

    public class Transactiondate
    {
        public string type { get; set; }
        public string valueDate { get; set; }
        public string text { get; set; }
        public float[] boundingBox { get; set; }
        public int page { get; set; }
        public float confidence { get; set; }
    }

    public class Transactiontime
    {
        public string type { get; set; }
        public string valueTime { get; set; }
        public string text { get; set; }
        public float[] boundingBox { get; set; }
        public int page { get; set; }
        public float confidence { get; set; }
    }

    public class Items
    {
        public string type { get; set; }
        public Valuearray[] valueArray { get; set; }
    }

    public class Valuearray
    {
        public string type { get; set; }
        public Valueobject valueObject { get; set; }
    }

    public class Valueobject
    {
        public Quantity Quantity { get; set; }
        public Name Name { get; set; }
        public Totalprice TotalPrice { get; set; }
    }

    public class Quantity
    {
        public string type { get; set; }
        public string text { get; set; }
        public float[] boundingBox { get; set; }
        public int page { get; set; }
        public float confidence { get; set; }
    }

    public class Name
    {
        public string type { get; set; }
        public string valueString { get; set; }
        public string text { get; set; }
        public float[] boundingBox { get; set; }
        public int page { get; set; }
        public float confidence { get; set; }
    }

    public class Totalprice
    {
        public string type { get; set; }
        public float valueNumber { get; set; }
        public string text { get; set; }
        public float[] boundingBox { get; set; }
        public int page { get; set; }
        public float confidence { get; set; }
    }

    public class Subtotal
    {
        public string type { get; set; }
        public float valueNumber { get; set; }
        public string text { get; set; }
        public float[] boundingBox { get; set; }
        public int page { get; set; }
        public float confidence { get; set; }
    }

    public class Tax
    {
        public string type { get; set; }
        public float valueNumber { get; set; }
        public string text { get; set; }
        public float[] boundingBox { get; set; }
        public int page { get; set; }
        public float confidence { get; set; }
    }

    public class Total
    {
        public string type { get; set; }
        public float valueNumber { get; set; }
        public string text { get; set; }
        public float[] boundingBox { get; set; }
        public int page { get; set; }
        public float confidence { get; set; }
    }

    public class Tip
    {
        public string type { get; set; }
        public float valueNumber { get; set; }
        public string text { get; set; }
        public float[] boundingBox { get; set; }
        public int page { get; set; }
        public float confidence { get; set; }
    }

    //public class OCRResult
    //{
    //    public string status { get; set; }
    //    //public Recognitionresult[] recognitionResults { get; set; }
    //    public Understandingresult[] understandingResults { get; set; }
    //}

    //public class Recognitionresult
    //{
    //    public int page { get; set; }
    //    public float clockwiseOrientation { get; set; }
    //    public int width { get; set; }
    //    public int height { get; set; }
    //    public string unit { get; set; }
    //    public Line[] lines { get; set; }
    //}

    //public class Line
    //{
    //    public int[] boundingBox { get; set; }
    //    public string text { get; set; }
    //    public Word[] words { get; set; }
    //}

    //public class Word
    //{
    //    public int[] boundingBox { get; set; }
    //    public string text { get; set; }
    //    public string confidence { get; set; }
    //}

    //public class Understandingresult
    //{
    //    public int[] pages { get; set; }
    //    public Fields fields { get; set; }
    //}

    //public class Fields
    //{
    //    public SubTotal Subtotal { get; set; }
    //    public Total Total { get; set; }
    //    public Tax Tax { get; set; }
    //    public Address MerchantAddress { get; set; }
    //    public Merchantname MerchantName { get; set; }
    //    public object MerchantPhoneNumber { get; set; }
    //    public Transactiondate TransactionDate { get; set; }
    //    public Transactiontime TransactionTime { get; set; }
    //}

    //public class Total
    //{
    //    public string valueType { get; set; }
    //    public float? value { get; set; }
    //    public string text { get; set; }
    //    public Element[] elements { get; set; }
    //}

    //public class SubTotal
    //{
    //    public string valueType { get; set; }
    //    public float value { get; set; }
    //    public string text { get; set; }
    //    public Element[] elements { get; set; }
    //}

    //public class Tax
    //{
    //    public string valueType { get; set; }
    //    public float value { get; set; }
    //    public string text { get; set; }
    //    public Element[] elements { get; set; }
    //}

    //public class Address
    //{
    //    public string valueType { get; set; }
    //    public string value { get; set; }
    //    public string text { get; set; }
    //    public Element[] elements { get; set; }
    //}
    //public class Element
    //{
    //    public string _ref { get; set; }
    //}

    //public class Merchantname
    //{
    //    public string valueType { get; set; }
    //    public string value { get; set; }
    //    public string text { get; set; }
    //    public Element1[] elements { get; set; }
    //}

    //public class Element1
    //{
    //    public string _ref { get; set; }
    //}

    //public class Transactiondate
    //{
    //    public string valueType { get; set; }
    //    public string value { get; set; }
    //    public string text { get; set; }
    //    public Element2[] elements { get; set; }
    //}

    //public class Element2
    //{
    //    public string _ref { get; set; }
    //}

    //public class Transactiontime
    //{
    //    public string valueType { get; set; }
    //    public string value { get; set; }
    //    public string text { get; set; }
    //    public Element3[] elements { get; set; }
    //}

    //public class Element3
    //{
    //    public string _ref { get; set; }
    //}


    public class OCRError
    {
        public Error error { get; set; }
    }

    public class Error
    {
        public string code { get; set; }
        public string message { get; set; }
    }

}
