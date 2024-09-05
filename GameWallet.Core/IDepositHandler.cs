using System.Net;
using System.Text.Json.Nodes;
using Payment.GameWallet.Client;

namespace Payment.GameWallet;

public interface IDepositHandler
{
    (Dictionary<string, string> Body, string Api, string[] KeysHash) MakeDepositRequest(DepositOrder order, GameWalletDepositSettings settings);
    GameWalletClientDepositResponse MakeResponse(DepositOrder order, HttpStatusCode statusCode, JsonNode body);

}
