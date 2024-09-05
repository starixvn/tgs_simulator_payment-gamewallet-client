using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace Payment.GameWallet.ViewModels
{
    public class CallBackDepositPromptPayViewModel
    {
        [FromForm(Name = "InvoiceNo")]
        [JsonPropertyName("InvoiceNo")]
        public string InvoiceNo { get; set; } = default!;

        [FromForm(Name = "ReceiverBank")]
        [JsonPropertyName("ReceiverBank")]
        public string ReceiverBank { get; set; } = default!;

        [FromForm(Name = "ReceiverAccount")]
        [JsonPropertyName("ReceiverAccount")]
        public string ReceiverAccount { get; set; } = default!;

        [FromForm(Name = "Currency")]
        [JsonPropertyName("Currency")]
        public string Currency { get; set; } = default!;

        [FromForm(Name = "Amount")]
        [JsonPropertyName("Amount")]
        public decimal Amount { get; set; } = default!;

        [FromForm(Name = "OriginalAmount")]
        [JsonPropertyName("OriginalAmount")]
        public decimal OriginalAmount { get; set; } = default!;

        [FromForm(Name = "PaymentDate")]
        [JsonPropertyName("PaymentDate")]
        public string PaymentDate { get; set; } = default!;

        [FromForm(Name = "MerchantCode")]
        [JsonPropertyName("MerchantCode")]
        public string MerchantCode { get; set; } = default!;

        [FromForm(Name = "RefId")]
        [JsonPropertyName("RefId")]
        public string RefId { get; set; } = default!;

        [FromForm(Name = "Status")]
        [JsonPropertyName("Status")]
        public int Status { get; set; } = default!;

        [FromForm(Name = "Signature")]
        [JsonPropertyName("Signature")]
        public string Signature { get; set; } = default!;

        [FromForm(Name = "StatusMessage")]
        [JsonPropertyName("StatusMessage")]
        public string StatusMessage { get; set; } = default!;

        [FromForm(Name = "DeclineReason")]
        [JsonPropertyName("DeclineReason")]
        public string DeclineReason { get; set; } = default!;
    }
}
