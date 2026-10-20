namespace Atendia.Application;

public interface IHandoffEmailSender
{
    bool IsConfigured { get; }

    Task SendAsync(
        string recipient,
        string businessName,
        string botName,
        string conversationId,
        string customerMessage,
        CancellationToken cancellationToken);
}

public sealed class HandoffEmailDeliveryException : Exception
{
    public HandoffEmailDeliveryException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
