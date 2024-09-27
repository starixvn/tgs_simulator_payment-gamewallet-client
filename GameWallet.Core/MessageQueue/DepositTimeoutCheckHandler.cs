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
    private const double TIME_DEPOSIT_RECHECK = 10D;
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
                _logger.LogWarning("Deposit Form {FormId} not found.", data.FormId.ToGuid());
                return;
            }

            var brandId = depositForm.BrandId;

            _logger.LogInformation(
                "GetDepositFormAsync Form Id:{DepositFormId}, Brand Id:{BrandId}, Status:{DepositFormStatus}",
                depositForm.Id,
                brandId,
                depositForm.Status);

            if (depositForm.Status >= PaymentFlowState.Reject)
            {
                return;
            }    

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
                "tgs.payment.gamewallet.deposit.schedule.reject",
                formId,
                DateTimeOffset.UtcNow.DateTime.AddMinutes(TIME_DEPOSIT_RECHECK),
                cancellationToken).ConfigureAwait(false);
        }
    }
}
