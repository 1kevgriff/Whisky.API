public interface ISendEmailService
{
    Task QueueEmail(OutgoingEmailMessage outgoingEmailMessage);
    Task ProcessQueue(CancellationToken cancellationToken);
    Task StopProcessing();
}