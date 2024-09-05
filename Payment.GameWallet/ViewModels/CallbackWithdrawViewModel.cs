using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace Payment.GameWallet.ViewModels;

public class CallbackWithdrawViewModel
{
    [FromForm(Name = "InvoiceNo")]
    [JsonPropertyName("InvoiceNo")]
    public string InvoiceNo { get; set; } = default!;

    [FromForm(Name = "Currency")]
    [JsonPropertyName("Currency")]
    public string Currency { get; set; } = default!;

    [FromForm(Name = "Status")]
    [JsonPropertyName("Status")]
    public int Status { get; set; } = default!;

    [FromForm(Name = "StatusMessage")]
    [JsonPropertyName("StatusMessage")]
    public string StatusMessage { get; set; } = default!;

    [FromForm(Name = "Amount")]
    [JsonPropertyName("Amount")]
    public decimal Amount { get; set; } = default!;

    [FromForm(Name = "PaymentDate")]
    [JsonPropertyName("PaymentDate")]
    public string PaymentDate { get; set; } = default!;

    [FromForm(Name = "RefId")]
    [JsonPropertyName("RefId")]
    public string RefId { get; set; } = default!;

    [FromForm(Name = "Signature")]
    [JsonPropertyName("Signature")]
    public string Signature { get; set; } = default!;
}
