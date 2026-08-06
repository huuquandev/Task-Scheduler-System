using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TaskScheduler.Application.Common.Models
{
    public class SmsMessage
    {
        public string phoneNumber { get; init; } = default!;
        public string message { get; init; } = default!;
    }
}