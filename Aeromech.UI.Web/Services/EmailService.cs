using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace AeroMech.UI.Web.Services
{
    /// <summary>
    /// How the outgoing mail server is reached, read from the "Email" configuration section
    /// (Email__Host, Email__Port and so on when set through the environment). Left empty, the
    /// system simply cannot send mail and says so, rather than failing somewhere less legible.
    /// </summary>
    public class EmailSettings
    {
        public string Host { get; set; } = string.Empty;

        public int Port { get; set; } = 587;

        /// <summary>Whether the connection is upgraded with STARTTLS (587) or opened over TLS (465).</summary>
        public bool UseStartTls { get; set; } = true;

        public string UserName { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public string FromAddress { get; set; } = string.Empty;

        public string FromName { get; set; } = "AeroMech";

        public bool IsConfigured => !string.IsNullOrWhiteSpace(Host) && !string.IsNullOrWhiteSpace(FromAddress);
    }

    /// <summary>
    /// Sends the few emails the system has any business sending - today, the password reset link.
    /// One message at a time over SMTP: at this scale a queue would be machinery with nothing to do.
    /// </summary>
    public class EmailService
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _settings = configuration.GetSection("Email").Get<EmailSettings>() ?? new EmailSettings();
            _logger = logger;
        }

        public bool IsConfigured => _settings.IsConfigured;

        public async Task SendAsync(string toAddress, string subject, string htmlBody)
        {
            if (!_settings.IsConfigured)
            {
                throw new InvalidOperationException("Email is not configured. Set the Email section (Host, Port, UserName, Password, FromAddress) in configuration.");
            }

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromAddress));
            message.To.Add(MailboxAddress.Parse(toAddress));
            message.Subject = subject;
            message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

            using var client = new SmtpClient();

            await client.ConnectAsync(
                _settings.Host,
                _settings.Port,
                _settings.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.SslOnConnect);

            if (!string.IsNullOrWhiteSpace(_settings.UserName))
            {
                await client.AuthenticateAsync(_settings.UserName, _settings.Password);
            }

            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Email sent to {ToAddress}: {Subject}", toAddress, subject);
        }
    }
}
