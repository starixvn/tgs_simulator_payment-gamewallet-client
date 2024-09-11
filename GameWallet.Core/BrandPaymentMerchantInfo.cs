using System.Text.Json.Serialization;

namespace Payment.GameWallet;
public class BrandPaymentMerchantInfo
{
    [JsonPropertyName("merchantCode")]
    public string MerchantCode { get; set; } = default!;
    [JsonPropertyName("securityCode")]
    public string SecurityCode { get; set; } = default!;
}
