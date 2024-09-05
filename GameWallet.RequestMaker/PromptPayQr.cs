using System.Net;
using System.Text.Json.Nodes;
using Payment.GameWallet;
using Payment.GameWallet.Client;

namespace BigPayz.DepositHandler;
internal class PromptPayQr : IDepositHandler
{
    public (Dictionary<string, string> Body, string Api, string[] KeysHash) MakeDepositRequest(DepositOrder order, GameWalletDepositSettings settings)
    {
        var dict = new Dictionary<string, string>
        {
            ["merchant_code"] = settings.MerchantCode,
            ["client_url"] = "",
            ["HTTPPostURL"] = new Uri(order.CallbackUrl, "promptpay").AbsoluteUri,
            ["amount"] = order.Amount.ToString(),
            ["currency_code"] = settings.Currency,
            ["ref_id"] = order.FormId.ToString(),
            ["player_ip"] = "127.0.0.1",
            ["player_username"] = order.PlayerId,
        };

        return (dict, "Payin/DepositV2", ["merchant_code", "ref_id",
            "player_username", "Amount", "player_ip", "currency_code", "amount", "client_url"]);
    }

    public GameWalletClientDepositResponse MakeResponse(DepositOrder order, HttpStatusCode statusCode, JsonNode body)
    {
        return statusCode != HttpStatusCode.OK || body["status"]!.GetValue<int>() != 0
            ? throw new ApiFailException(body["error_message"]!.GetValue<string>())
            : new GameWalletClientDepositResponse(
            Amount: body["amount"]!.GetValue<decimal>(),
            FormId: order.FormId,
            RedirectUrl: new Uri(body["redirect_to"]!.GetValue<string>()),
            Token: body["token"]!.GetValue<string>(),
            Currency: body["currency_code"]!.GetValue<string>(),
            Transaction: body["invoice_number"]!.ToString());
    }
}
