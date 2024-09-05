using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace Payment.GameWallet.ViewModels;

public class CallbackWithdrawViewModel
{
    [FromForm(Name = "statementDate")]
    [JsonPropertyName("statementDate")]
    public string StatemnetDate { get; set; } = default!;

    [FromForm(Name = "statusCode")]
    [JsonPropertyName("statusCode")]
    public string StatusCode { get; set; } = default!;

    [FromForm(Name = "actualAmount")]
    [JsonPropertyName("actualAmount")]
    public string ActualAmount { get; set; } = default!;

    [FromForm(Name = "statusMessage")]
    [JsonPropertyName("statusMessage")]
    public string StatusMessage { get; set; } = default!;

    [FromForm(Name = "transactionId")]
    [JsonPropertyName("transactionId")]
    public string TransactionId { get; set; } = default!;

    [FromForm(Name = "datetime")]
    [JsonPropertyName("datetime")]
    public string SubmitDatetime { get; set; } = default!;

    [FromForm(Name = "key")]
    [JsonPropertyName("key")]
    public string Key { get; set; } = default!;

    [FromForm(Name = "merchant")]
    [JsonPropertyName("merchant")]
    public string Merchant { get; set; } = default!;

    [FromForm(Name = "currency")]
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = default!;

    [FromForm(Name = "bank")]
    [JsonPropertyName("bank")]
    public string Bank { get; set; } = default!;

    [FromForm(Name = "customer")]
    [JsonPropertyName("customer")]
    public string Customer { get; set; } = default!;

    [FromForm(Name = "reference")]
    [JsonPropertyName("reference")]
    public string Reference { get; set; } = default!;

    [FromForm(Name = "amount")]
    [JsonPropertyName("amount")]
    public string Amount { get; set; } = default!;
}
