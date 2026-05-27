using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskScheduler.Domain.Common;

namespace TaskScheduler.Domain.Events
{
    public record TaskFailedEvent(Guid TaskId, string Reason) : DomainEvent;
}