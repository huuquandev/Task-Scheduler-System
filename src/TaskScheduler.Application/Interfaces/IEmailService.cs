using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskScheduler.Application.Common.Models;
namespace TaskScheduler.Application.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(EmailMessage message);
    }
}