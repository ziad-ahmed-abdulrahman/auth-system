using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Auth.Core.Interfaces.Services
{
    public interface IEmailSender
    {
            Task SendEmailAsync(string to, string subject, string body, IList<IFormFile>? attachments = null);
    }
}
