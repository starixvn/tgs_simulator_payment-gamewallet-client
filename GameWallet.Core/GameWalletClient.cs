using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;

namespace Payment.GameWallet;

public class GameWalletClient(
    HttpClient httpClient,
    ILogger<GameWalletClient> logger)
{

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
        logger.LogInformation("GameWallet Withdraw Body:{body}", body);

        if (!response.IsSuccessStatusCode)
            throw new ApiFailException(body);

        var node = JsonNode.Parse(body) ?? throw new ApiFailException("Withdraw response can't read.");

        return node["statusCode"]!.GetValue<string>() != "002"
            ? throw new ApiFailException($"Error in Payment Provider with status {node["statusMessage"]!.GetValue<string>()}")
            : node;
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

        var balance = node["availableBalance"]?.GetValue<decimal>() ?? throw new ApiFailException("Query Merchant Balance response can't read availableBalance.");

        return balance;
    }

}
