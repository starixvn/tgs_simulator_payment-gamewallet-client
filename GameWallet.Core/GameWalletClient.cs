using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Payment.GameWallet.Client;

namespace Payment.GameWallet;

public class GameWalletClient(
    HttpClient httpClient,
    GameWalletHasher hasher,
    TimeProvider timeProvider,
    IDepositHandlerFactory depositHandlerFactory,
    ILogger<GameWalletClient> logger)
{
    public async ValueTask<GameWalletClientDepositResponse> DepositAsync(GameWalletDepositSettings settings, DepositOrder order, CancellationToken cancellationToken = default)
    {
        var depositHandler = depositHandlerFactory.GetDepositHandler(settings.ChannelType);
        var (dict, api, keysHash) = depositHandler.MakeDepositRequest(order, settings);

        var valuesToHash = dict.Where(x => keysHash.Contains(x.Key)).Select(x => x.Value).ToList();
        var hash = hasher.ComputeHash(settings.ApiKey, valuesToHash);
        dict.Add("hash", hash);

        var request = new HttpRequestMessage(HttpMethod.Post, api)
        {
            Content = new StringContent(JsonSerializer.Serialize(dict), Encoding.UTF8, MediaTypeNames.Application.Json)
        };
        request.Headers.Add("Authorization", $"Basic {settings.ApiKey}");

        var response = await httpClient
            .SendAsync(request, cancellationToken)
            .ConfigureAwait(false);

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var node = JsonNode.Parse(body) ?? throw new ApiFailException("Deposit response can't read.");

        logger.LogInformation("BigPayzClient Deposit Body:{body}", body);

        return depositHandler.MakeResponse(order, response.StatusCode, body);
    }

    /*
    public async ValueTask<string> QueryDepositFormStatusAsync(string merchantNo, string appSecert, string privateKey, Guid formId, CancellationToken cancellationToken = default)
    {
        var dict = new Dictionary<string, string?>
        {
            ["merchantNo"] = merchantNo,
            ["tradeNo"] = "",
            ["orderNo"] = formId.ToString("N"),
            ["time"] = timeProvider.GetUtcNow().ToUnixTimeSeconds().ToString(),
            ["appSecret"] = appSecert,
        };

        var sign = hasher.ComputeHash(privateKey, dict.Where(x => x.Key != "appSecret"));
        dict["sign"] = sign;

        var request = new HttpRequestMessage(HttpMethod.Post, "order/status")
        {
            Content = new FormUrlEncodedContent(dict)
        };

        var response = await httpClient
            .SendAsync(request, cancellationToken)
            .ConfigureAwait(false);

        _ = response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("U2PayClient Query Deposit Form Body:{body}", body);

        var node = JsonNode.Parse(body) ?? throw new ApiFailException("Query Deposit Form response can't read.");

        if (node["code"]!.GetValue<int>() != 0)
            throw new ApiFailException(node["text"]!.GetValue<string>());

        return node["status"]!.GetValue<string>();
    }
    */

    public async ValueTask<GameWalletClientWithdrawResponse> WithdrawAsync(string merchantNo, string privateKey, WithdrawOrder order, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();

        var dict = new Dictionary<string, string>
        {
            ["merchantCode"] = merchantNo,
            ["ref_id"] = order.FormId.ToString(),
            ["player_username"] = order.ReceiverName,
            ["player_ip"] = order.PlayerIp,
            ["currency_code"] = order.CurrencyCode,
            ["amount"] = order.Amount.ToString(),
            ["bank_code"] = order.BankCode,
            ["beneficiary_account"] = order.BankAccount,
            ["beneficiary_name"] = order.BankName,
            ["ifsc"] = order.BankCode,
            ["notifyUrl"] = order.CallbackUrl.AbsoluteUri,
        };

        var data = new string[] { dict["merchantCode"], dict["ref_id"], dict["player_username"], dict["player_ip"],
            dict["currency_code"], dict["amount"], dict["bank_code"], dict["beneficiary_account"], dict["beneficiary_name"] };

        var sign = hasher.ComputeHash(privateKey, data);
        dict["hash"] = sign;

        var request = new HttpRequestMessage(HttpMethod.Post, "Payout/Withdrawal")
        {
            Content = new StringContent(JsonSerializer.Serialize(dict), Encoding.UTF8, MediaTypeNames.Application.Json)
        };
        request.Headers.Add("Authorization", $"Basic {privateKey}");

        var response = await httpClient
            .SendAsync(request, cancellationToken)
            .ConfigureAwait(false);

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var node = JsonNode.Parse(body) ?? throw new ApiFailException("Withdraw response can't read.");

        if (response.StatusCode != System.Net.HttpStatusCode.OK)
            throw new ApiFailException(node["error_message"]!.GetValue<string>());

        if (node["error_code"]!.GetValue<int>() != 0)
            throw new ApiFailException($"Error in Payment Provider with status {node["error_message"]!.GetValue<int>()}");

        logger.LogInformation("BigPayzClient Withdraw Body:{body}", body);

        return new GameWalletClientWithdrawResponse(
            FormId: order.FormId,
            ThirdPartyOrderNo: node["invoice_number"]!.GetValue<string>(),
            MerchantBalance: node["merchant_balance"]!.GetValue<decimal>());
    }


    public async ValueTask<decimal> QueryBalanceAsync(string merchantCode, string currencyCode, string privateKey, CancellationToken cancellationToken = default)
    {
        var dict = new Dictionary<string, string?>
        {
            ["merchant_code"] = merchantCode,
        };

        var sign = hasher.ComputeHash(privateKey, [dict["merchant_code"]!]);
        dict["sign"] = sign;

        var request = new HttpRequestMessage(HttpMethod.Post, "Payout/GetBalance")
        {
            Content = new StringContent(JsonSerializer.Serialize(dict), Encoding.UTF8, MediaTypeNames.Application.Json)
        };
        request.Headers.Add("Authorization", $"Basic {privateKey}");

        var response = await httpClient
            .SendAsync(request, cancellationToken)
            .ConfigureAwait(false);

        _ = response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("BigPayz Query Merchant Balance Body:{body}", body);

        var node = JsonNode.Parse(body) ?? throw new ApiFailException("Query Merchant Balance response can't read.");

        if (node["error_code"]!.GetValue<int>() != 0)
            throw new ApiFailException(node["error_message"]!.GetValue<string>());

        var balances = node["info"]!.GetValue<(string CurrencyCode, decimal RunningBalance)[]>();

        var balance = balances.Where(x => string.Equals(x.CurrencyCode, currencyCode)).Select(x => x.RunningBalance).FirstOrDefault();

        return balance;
    }

}
