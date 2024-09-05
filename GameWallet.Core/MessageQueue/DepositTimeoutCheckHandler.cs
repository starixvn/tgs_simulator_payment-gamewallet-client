using Microsoft.Extensions.Logging;
using Tgs.Payment.Clients;
using Tgs.Payment.Protos;
using Valhalla.MessageQueue;

namespace Payment.GameWallet.MessageQueue;

internal class DepositTimeoutCheckHandler(
    IPaymentServiceClient paymentServiceClient,
    IPaymentClient paymentClient,
    ILogger<DepositTimeoutCheckHandler> logger)
    : IMessageHandler<ScheduleRequest>
{
    private readonly ILogger<DepositTimeoutCheckHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async ValueTask HandleAsync(
        string subject,
        ScheduleRequest data,
        IEnumerable<MessageHeaderValue>? headerValues,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Deposit Schedule Handler Receive request Form Id:{request.FormId.ToGuid()}, "
                + "Schedule Time:{request.ScheduleTime.ToDateTime():yyyy/MM/dd HH:mm:ss}",
            data.FormId.ToGuid(),
            data.ScheduleTime.ToDateTime());

        var formId = data.FormId.ToGuid();

        try
        {
            var depositForm = await paymentServiceClient
                .GetDepositFormAsync(formId, cancellationToken)
                .ConfigureAwait(false);

            if (depositForm is null)
            {
                _logger.LogWarning("Deposit Form {request.FormId.ToGuid()} not found.", data.FormId.ToGuid());
                return;
            }

            var brandId = depositForm.BrandId;

            _logger.LogInformation(
                "GetDepositFormAsync Form Id:{depositForm.Id}, Brand Id:{brandId}, Status:{depositForm.Status}",
                depositForm.Id,
                brandId,
                depositForm.Status);

            if (depositForm.Status >= PaymentFlowState.Reject)
                return;

            var fsm = PaymentStateMachineFactory.CreateDepositStateMachine(depositForm.Status);

            if (fsm.CanFire(PaymentTrigger.Reject))
            {
                await fsm.FireAsync(PaymentTrigger.Reject).ConfigureAwait(false);

                await paymentClient
                    .RejectDepositAsync(formId, brandId, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Process deposit timeout error!");

            await paymentClient.ScheduleAsync(
                "tgs.payment.bigpayz.deposit.schedule.reject",
                formId,
                DateTime.UtcNow.AddMinutes(10D),
                cancellationToken).ConfigureAwait(false);
        }
    }
}
