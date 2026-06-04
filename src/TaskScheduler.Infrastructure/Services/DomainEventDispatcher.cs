using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using TaskScheduler.Application.Common.Interfaces;
using TaskScheduler.Domain.Common;

namespace TaskScheduler.Infrastructure.Services
{
    public class DomainEventDispatcher : IDomainEventDispatcher
    {
        private readonly IMediator _mediator;

        public DomainEventDispatcher(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task DispatchAsync(IDomainEvent domainEvent)
        {
            await _mediator.Publish(domainEvent);
        }
    }
}