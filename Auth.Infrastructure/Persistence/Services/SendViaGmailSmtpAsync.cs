using Auth.Core.Interfaces.Services;
using Auth.Core.Shared;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using MimeKit;


namespace Auth.Infrastructure.Persistence.Services
{
    public class SendViaGmailSmtpAsync : IEmailSender
    {
        // if server support sending emails
        private readonly GmailSettings _gmailSettings;

        public SendViaGmailSmtpAsync(IOptions<GmailSettings> gmailSettings)
        {
            _gmailSettings = gmailSettings.Value;
        }

        public async Task SendEmailAsync(string to, string subject, string body, IList<IFormFile>? attachments = null)
        {
            MimeMessage? email = new MimeMessage
            {

                Subject = subject

            };

            email.To.Add(MailboxAddress.Parse(to));

            var builder = new BodyBuilder();

            if (attachments != null)
            {
                byte[] fileBytes;
                foreach (var file in attachments)
                {
                    if (file?.Length > 0)
                    {
                        using var ms = new MemoryStream();
                        file.CopyTo(ms);
                        fileBytes = ms.ToArray();

                        builder.Attachments.Add(fileName: file.FileName, fileBytes, MimeKit.ContentType.Parse(text: file.ContentType));
                    }
                }
            }

            builder.HtmlBody = body;

            email.Body = builder.ToMessageBody();
            email.From.Add(new MailboxAddress(_gmailSettings.DisplayName, _gmailSettings.Email!));

            using var stmp = new SmtpClient();
            await stmp.ConnectAsync(_gmailSettings.Host, port: _gmailSettings.Port, SecureSocketOptions.StartTls);

            await stmp.AuthenticateAsync(_gmailSettings.Email, _gmailSettings.Password);
            await stmp.SendAsync(email);

            await stmp.DisconnectAsync(true);


        }
    }
}
