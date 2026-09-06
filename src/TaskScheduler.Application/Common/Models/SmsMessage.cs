using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TaskScheduler.Application.Common.Models
{
    public class SmsMessage
    {
        public string PhoneNumber { get; init; } = default!;
        public string Message { get; init; } = default!;
    }
}