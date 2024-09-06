using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace Payment.GameWallet.ViewModels;

public class VerifyTransactionViewModel
{
    [FromForm(Name = "Merchant")]
    [JsonPropertyName("Merchant")]
    public string Merchant { get; set; } = default!;

    [FromForm(Name = "Reference")]
    [JsonPropertyName("Reference")]
    public string Reference { get; set; } = default!;

    [FromForm(Name = "Amount")]
    [JsonPropertyName("Amount")]
    public string Amount { get; set; } = default!;

    [FromForm(Name = "Sign")]
    [JsonPropertyName("Sign")]
    public string Sign { get; set; } = default!;
}
