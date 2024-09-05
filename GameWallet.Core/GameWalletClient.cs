using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Payment.GameWallet;

public class GameWalletClient(
    HttpClient httpClient,
    IOptions<GameWalletOptions> optionsAccessor,
    ILogger<GameWalletClient> logger)
{
    private readonly GameWalletOptions _options = optionsAccessor.Value;

    public async ValueTask<JsonNode> WithdrawAsync(Dictionary<string,string> content, CancellationToken cancellationToken = default)
    {   
        var request = new HttpRequestMessage(HttpMethod.Post, "api/MerchantTransfer/withdraw")
        {
            Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, MediaTypeNames.Application.Json)
        };
        request.Headers.Add("Accept", MediaTypeNames.Application.Json);

        var response = await httpClient
            .SendAsync(request, cancellationToken)
            .ConfigureAwait(false);

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            throw new ApiFailException(body);

        var node = JsonNode.Parse(body) ?? throw new ApiFailException("Withdraw response can't read.");

        if (node["status_code"]!.GetValue<string>() != "001")
            throw new ApiFailException($"Error in Payment Provider with status {node["statusMessage"]!.GetValue<string>()}");

        logger.LogInformation("GameWalletClient Withdraw Body:{body}", body);

        return node;
    }


    public async ValueTask<decimal> QueryBalanceAsync(Dictionary<string, string> content, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "api/merchant/AvailableBalance")
        {
            Content = new StringContent(JsonSerializer.Serialize(content), Encoding.UTF8, MediaTypeNames.Application.Json)
        };

        var response = await httpClient
            .SendAsync(request, cancellationToken)
            .ConfigureAwait(false);

        _ = response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("GameWallet Query Merchant Balance Body:{body}", body);

        var node = JsonNode.Parse(body) ?? throw new ApiFailException("Query Merchant Balance response can't read.");

        if (node["error_code"]!.GetValue<int>() != 0)
            throw new ApiFailException(node["error_message"]!.GetValue<string>());

        var balance = node["availableBalance"]!.GetValue<decimal>();

        return balance;
    }

}
