using Microsoft.Extensions.Logging;
using System.Text.Json.Nodes;
using Payment.GameWallet;
using Payment.GameWallet.Client;
using System.Net;

namespace BigPayz.DepositHandler;
internal class BankTransfer: IDepositHandler
{
    public (Dictionary<string, string> Body, string Api, string[] KeysHash) MakeDepositRequest(DepositOrder order, GameWalletDepositSettings settings)
    {
        var dict = new Dictionary<string, string>
        {
            ["MerchantCode"] = settings.MerchantCode,
            ["ReturnURL"] = "",
            ["FailedReturnURL"] = "",
            ["HTTPPostURL"] = order.CallbackUrl.AbsoluteUri,
            ["Amount"] = order.Amount.ToString(),
            ["Currency"] = settings.Currency,
            ["ItemID"] = order.FormId.ToString(),
            ["ItemDescription"] = order.ItemDescription,
            ["PlayerId"] = order.PlayerId,
        };

        return (dict, "Payin/DepositV2", ["MerchantCode", "ItemID", "Currency", "Amount"]);
    }

    public GameWalletClientDepositResponse MakeResponse(DepositOrder order, HttpStatusCode statusCode, JsonNode body)
    {
        return statusCode != HttpStatusCode.OK
            ? throw new ApiFailException(body["message"]!.GetValue<string>())
            : body["status"]!.GetValue<int>() != 0
            ? throw new ApiFailException($"Error in Payment Provider with status {body["status"]!.GetValue<int>()}")
            : new GameWalletClientDepositResponse(
            Amount: body["Amount"]!.GetValue<decimal>(),
            FormId: order.FormId,
            RedirectUrl: new Uri(body["redirect_to"]!.GetValue<string>()),
            Token: body["token"]!.GetValue<string>(),
            Currency: body["Currency"]!.GetValue<string>(),
            Transaction: body["transaction"]!.ToString());
    }
}
