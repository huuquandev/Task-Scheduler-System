using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TaskScheduler.Application.Common.Models
{
    public class SmsMessage
    {
        string phoneNumber { get; init; } = default!;
        string message { get; init; } = default!;
    }
}