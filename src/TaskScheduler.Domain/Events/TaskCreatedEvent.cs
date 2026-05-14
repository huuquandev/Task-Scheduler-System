using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskScheduler.Domain.Common;

namespace TaskScheduler.Domain.Events
{
    public record TaskCreatedEvent(Guid TaskId, string Name) : IDomainEvent;
    
}