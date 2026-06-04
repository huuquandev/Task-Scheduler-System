using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TaskScheduler.Application.Common.Models
{
    public class EmailMessage
    {
        public string To { get; init; } = default!;
        public string Subject { get; init; } = default!;
        public string Body { get; init; } = default!;
    }
}