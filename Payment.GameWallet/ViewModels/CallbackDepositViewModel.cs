using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace Payment.GameWallet.ViewModels;

public class CallbackDepositViewModel
{
    [FromForm(Name = "contract")]
    [JsonPropertyName("contract")]
    public long Contract { get; set; } = default!;

    [FromForm(Name = "apiKey")]
    [JsonPropertyName("apiKey")]
    public string ApiKey { get; set; } = default!;

    [FromForm(Name = "transaction")]
    [JsonPropertyName("transaction")]
    public string Transaction { get; set; } = default!;

    [FromForm(Name = "status")]
    [JsonPropertyName("status")]
    public int Status { get; set; } = default!;

    [FromForm(Name = "status_message")]
    [JsonPropertyName("status_message")]
    public string StatusMessage { get; set; } = default!;

    [FromForm(Name = "ItemID")]
    [JsonPropertyName("ItemID")]
    public string ItemId { get; set; } = default!;

    [FromForm(Name = "ItemDescription")]
    [JsonPropertyName("ItemDescription")]
    public string ItemDescription { get; set; } = default!;

    [FromForm(Name = "Amount")]
    [JsonPropertyName("Amount")]
    public decimal Amount { get; set; } = default!;

    [FromForm(Name = "Currency")]
    [JsonPropertyName("Currency")]
    public string Currency { get; set; } = default!;


    [FromForm(Name = "ClientName")]
    [JsonPropertyName("ClientName")]
    public string ClientName { get; set; } = default!;

    [FromForm(Name = "bank_name")]
    [JsonPropertyName("bank_name")]
    public string? BankName { get; set; } = default!;

    [FromForm(Name = "BusinessAcct")]
    [JsonPropertyName("BusinessAcct")]
    public string? BusinessAcct { get; set; } = default!;

    [FromForm(Name = "src_bank_account")]
    [JsonPropertyName("src_bank_account")]
    public string? BankAccountNumber { get; set; } = default!;

    [FromForm(Name = "bank_reference")]
    [JsonPropertyName("bank_reference")]
    public string? BankRef { get; set; } = default!;

    [FromForm(Name = "signature2")]
    [JsonPropertyName("signature2")]
    public string Signature { get; set; } = default!;

    [FromForm(Name = "created_at")]
    [JsonPropertyName("created_at")]
    public string CreatedAt { get; set; } = default!;

    [FromForm(Name = "updated_at")]
    [JsonPropertyName("updated_at")]
    public string UpdatedAt { get; set; } = default!;

    [FromForm(Name = "sender_name")]
    [JsonPropertyName("sender_name")]
    public string? SenderName { get; set; } = default!;
}
