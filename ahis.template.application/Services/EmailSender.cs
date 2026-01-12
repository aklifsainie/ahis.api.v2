using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;

namespace ahis.template.application.Services
{
    /// <summary>
    /// Responsible for sending emails using SMTP.
    /// Implements IEmailSender for ASP.NET Identity compatibility.
    /// </summary>
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailSender> _logger;

        public EmailSender(
            IConfiguration configuration,
            ILogger<EmailSender> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Sends an email asynchronously using SMTP.
        /// </summary>
        /// <param name="email">Recipient email address</param>
        /// <param name="subject">Email subject</param>
        /// <param name="htmlMessage">HTML email body</param>
        public async Task SendEmailAsync(
            string email,
            string subject,
            string htmlMessage)
        {
            // Read SMTP configuration
            var smtpSection = _configuration.GetSection("EmailSmtp");

            var host = smtpSection["Host"];
            var port = smtpSection.GetValue<int>("Port");
            var username = smtpSection["Username"];
            var password = smtpSection["Password"];
            var enableSsl = smtpSection.GetValue<bool>("EnableSsl", true);

            // Validate configuration early (fail fast)
            if (string.IsNullOrWhiteSpace(host) ||
                string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException(
                    "SMTP configuration is missing or invalid. Check EmailSmtp settings.");
            }

            try
            {
                // Create SMTP client (dispose properly)
                using var smtpClient = new SmtpClient(host, port)
                {
                    Credentials = new NetworkCredential(username, password),
                    EnableSsl = enableSsl,
                    Timeout = 10000 // 10 seconds timeout (best practice)
                };

                // Create email message
                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(username),
                    Subject = subject,
                    Body = htmlMessage,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(email);

                // Send email
                await smtpClient.SendMailAsync(mailMessage);

                _logger.LogInformation(
                    "Email sent successfully to {Email}", email);
            }
            catch (Exception ex)
            {
                // Log detailed error for production diagnostics
                _logger.LogError(
                    ex,
                    "Failed to send email to {Email}", email);

                throw; // Important: rethrow so caller knows it failed
            }
        }
    }
}
