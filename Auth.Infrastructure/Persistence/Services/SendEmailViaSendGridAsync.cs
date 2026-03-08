using Auth.Core.Interfaces.Services;
using Auth.Core.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;


namespace Auth.Infrastructure.Persistence.Services
{
    public class SendEmailViaSendGridAdsync : IEmailSender
    {
        // if server does not support sending emails

        private readonly SendGridSettings _sendGridSettings;

        public SendEmailViaSendGridAdsync(IOptions<SendGridSettings> sendGridSettings)
        {
            _sendGridSettings = sendGridSettings.Value;
        }

        public async Task SendEmailAsync(string to, string subject, string body, IList<IFormFile>? attachments = null)
        {

            var client = new SendGridClient(_sendGridSettings.Password);

            var fromEmail = new EmailAddress(_sendGridSettings.Email, _sendGridSettings.DisplayName);
            var toEmail = new EmailAddress(to);


            var msg = MailHelper.CreateSingleEmail(fromEmail, toEmail, subject, plainTextContent: string.Empty, htmlContent: body);


            if (attachments != null)
            {
                foreach (var file in attachments)
                {
                    if (file.Length > 0)
                    {
                        using var ms = new MemoryStream();
                        await file.CopyToAsync(ms);
                        var fileBytes = ms.ToArray();
                        var base64Content = Convert.ToBase64String(fileBytes);

                        msg.AddAttachment(file.FileName, base64Content);
                    }
                }
            }


            var response = await client.SendEmailAsync(msg);


            if (!response.IsSuccessStatusCode)
            {

                var error = await response.Body.ReadAsStringAsync();
                throw new Exception($"SendGrid Error: {error}");
            }
        }

    }
}
