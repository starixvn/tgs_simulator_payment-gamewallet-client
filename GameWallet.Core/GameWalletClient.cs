using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tgs.Logging;

namespace Payment.GameWallet;

public class GameWalletClient(
    HttpClient httpClient,
    IMemoryCache memoryCache,
    IOptions<GameWalletOptions> optionsAccessor,
    ILogger<GameWalletClient> logger,
    ITgsLog tgsLog)
{
    private readonly GameWalletOptions _options = optionsAccessor.Value;

    public async ValueTask<JsonNode> WithdrawAsync(Dictionary<string, string> content, CancellationToken cancellationToken = default)
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
        LogApi(Guid.Parse(content["Reference"]),
            "/api/MerchantTransfer/withdraw",
            JsonSerializer.Serialize(content),

            body, response);

        if (!response.IsSuccessStatusCode)
            throw new ApiFailException(body);

        var node = JsonNode.Parse(body) ?? throw new ApiFailException("Withdraw response can't read.");

        return node["statusCode"]!.GetValue<string>() != "002"
            ? throw new ApiFailException($"Error in Payment Provider with status {node["statusMessage"]!.GetValue<string>()}")
            : node;
    }


    public async ValueTask<decimal> QueryBalanceAsync(Dictionary<string, string> content, CancellationToken cancellationToken = default)
    {
        return await QueryBalanceFromCacheAsync(content, cancellationToken);
    }

    private async Task<decimal> QueryBalanceFromApiAsync(Dictionary<string, string> content, CancellationToken cancellationToken)
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

    private void LogApi(Guid formId, string path, string request, string response, HttpResponseMessage rawResponse)
    {
        var ip = Environment.MachineName;

        var apiCall = new ApiLogEvent
        {
            ExecutionTimeMs = 0,
            Ip = ip,
            Method = "POST",
            Path = path,
            Prefix = _options.DepositUrl.ToString(),
            RecordedOn = DateTimeOffset.Now,
            RequestContent = request,
            RequestHeaders = string.Empty,
            ResponseCode = ((short)rawResponse.StatusCode),
            ResponseContent = response,
            ResponseHeaders = JsonSerializer.Serialize(rawResponse.Headers),
            ServerName = Environment.MachineName,
            TrackingId = formId
        };

        tgsLog.LogApi(apiCall);
    }

    private async Task<decimal> QueryBalanceFromCacheAsync(Dictionary<string, string> content, CancellationToken cancellationToken)
    {
        var cacheKey = $"GAME-WALLET-BALANCE-{content["Key"]}";
        return (await memoryCache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.SetAbsoluteExpiration(TimeSpan.FromSeconds(30));
            return await QueryBalanceFromApiAsync(content, cancellationToken);
        }));
    }
}
