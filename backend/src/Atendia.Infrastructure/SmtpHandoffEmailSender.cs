using System.Net.Sockets;
using Atendia.Application;
using MailKit.Security;
using MimeKit;

namespace Atendia.Infrastructure;

public sealed class HandoffEmailSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = "Atendia";
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool UseStartTls { get; set; } = true;
}

public sealed class SmtpHandoffEmailSender : IHandoffEmailSender
{
    private readonly HandoffEmailSettings _settings;

    public SmtpHandoffEmailSender(HandoffEmailSettings settings)
    {
        _settings = settings;
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_settings.Host) &&
        _settings.Port is > 0 and <= 65535 &&
        !string.IsNullOrWhiteSpace(_settings.FromAddress) &&
        (string.IsNullOrWhiteSpace(_settings.UserName) || !string.IsNullOrWhiteSpace(_settings.Password));

    public async Task SendAsync(
        string recipient,
        string businessName,
        string botName,
        string conversationId,
        string customerMessage,
        CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            throw new HandoffEmailDeliveryException("La configuración de correo SMTP está incompleta.");
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromAddress));
            message.To.Add(MailboxAddress.Parse(recipient));
            message.Subject = $"Atendia: nueva solicitud de atención humana";
            message.Body = new TextPart("plain")
            {
                Text = $"""
                    Se recibió una solicitud de atención humana.

                    Negocio: {businessName}
                    Bot: {botName}
                    Conversación: {conversationId}

                    Mensaje del cliente:
                    {customerMessage}
                    """
            };

            using var client = new MailKit.Net.Smtp.SmtpClient();
            client.Timeout = 15000;
            var socketOptions = _settings.UseStartTls ? SecureSocketOptions.Auto : SecureSocketOptions.None;
            await client.ConnectAsync(_settings.Host, _settings.Port, socketOptions, cancellationToken);
            if (!string.IsNullOrWhiteSpace(_settings.UserName))
            {
                await client.AuthenticateAsync(_settings.UserName, _settings.Password, cancellationToken);
            }

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
        }
        catch (Exception exception) when (
            exception is MailKit.Net.Smtp.SmtpCommandException or
            MailKit.Net.Smtp.SmtpProtocolException or
            MailKit.ServiceNotConnectedException or
            MailKit.ServiceNotAuthenticatedException or
            MailKit.Security.AuthenticationException or
            SocketException or
            IOException or
            System.Security.Authentication.AuthenticationException or
            MimeKit.ParseException or
            ArgumentException or
            FormatException)
        {
            throw new HandoffEmailDeliveryException("No se pudo enviar la notificación de handoff por email.", exception);
        }
        catch (Exception exception) when (
            (exception is TaskCanceledException or TimeoutException) &&
            !cancellationToken.IsCancellationRequested)
        {
            throw new HandoffEmailDeliveryException("El envío de la notificación de handoff agotó el tiempo de espera.", exception);
        }
    }
}
