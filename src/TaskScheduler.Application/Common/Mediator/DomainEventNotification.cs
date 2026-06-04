using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using TaskScheduler.Domain.Common;

namespace TaskScheduler.Application.Common.EventNotifications
{
    public class DomainEventNotification<TDomainEvent> : INotification where TDomainEvent : IDomainEvent
    {
        public TDomainEvent DomainEvent { get; }
        public DomainEventNotification(TDomainEvent domainEvent)
        {
             DomainEvent = domainEvent;
        }
    }
}