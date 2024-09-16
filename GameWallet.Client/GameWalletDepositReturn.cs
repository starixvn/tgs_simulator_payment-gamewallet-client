namespace Payment.GameWallet.Client;
public record GameWalletDepositReturn
{
    public string Transaction { get; set; } = default!;

    public decimal OrderAmount { get; set; }

    public decimal RealAmount { get; set; }

    public Guid FormId { get; set; }

    public string StatusCode { get; set; } = default!;

    public string StatusMessage { get; set; } = default!;

    public string Key { get; set; } = default!;

    public string? SenderBankName { get; set; } = default!;

    public string? SenderBankAccount { get; set; } = default!;

    public string? ReceiveAccountName { get; set; } = default!;

    public string? ReceiveBankAccount { get; set; } = default!;

    public string ExtendData { get; set; } = default!;
}
