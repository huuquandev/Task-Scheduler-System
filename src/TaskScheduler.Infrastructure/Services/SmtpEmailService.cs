using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using TaskScheduler.Application.Common.Models;
using TaskScheduler.Application.Interfaces;

namespace TaskScheduler.Infrastructure.Services
{
    public class SmtpEmailService : IEmailService
    {
        private readonly IConfiguration _config;
        public SmtpEmailService(IConfiguration config) => _config = config;

        public async Task SendEmailAsync(EmailMessage message)
        {
            var host = _config["Smtp:Host"]!;
            var port = int.Parse(_config["Smtp:Port"] ?? "587");
            var user = _config["Smtp:User"]!;
            var pass = _config["Smtp:Password"]!;

            using var client = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(user, pass),
                EnableSsl = true
            };
            await client.SendMailAsync(new MailMessage(user, message.To, message.Subject, message.Body));
        }
    }
}