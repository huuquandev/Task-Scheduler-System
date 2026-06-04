using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskScheduler.Domain.Common;
using TaskScheduler.Domain.Entities;
using TaskScheduler.Domain.ValueObjects;
using TaskScheduler.Application.Interfaces;
using TaskScheduler.Application.Common.EventNotifications;
namespace TaskScheduler.Infrastructure.Persistence
{
    public class ApplicationDbContext : DbContext, IUnitOfWork
    {
        private readonly IPublisher _publisher;
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IPublisher publisher): base(options)
        {
            _publisher = publisher;
        }

        public DbSet<ScheduledTask> ScheduledTasks => Set<ScheduledTask>();
        public DbSet<TaskExecutionLog> TaskExecutionLogs => Set<TaskExecutionLog>();
        public DbSet<User> Users => Set<User>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        }
        public override async Task<int>SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var result = await base.SaveChangesAsync(cancellationToken);

            await DispatchDomainEvents();

            return result;
        }
        private async Task DispatchDomainEvents()
        {
            var entities = ChangeTracker
                .Entries<BaseEntity>()
                .Where(x => x.Entity.DomainEvents.Any())
                .Select(x => x.Entity);

            var domainEvents = entities
                .SelectMany(x => x.DomainEvents)
                .ToList();

            entities.ToList().ForEach(x => x.ClearDomainEvents());

            foreach (var domainEvent in domainEvents)
            {
                var notification = CreateDomainEventNotification(domainEvent);

                await _publisher.Publish(notification);
            }
        }
        
        private static INotification CreateDomainEventNotification(IDomainEvent domainEvent)
        {
            var notificationType = typeof(DomainEventNotification<>).MakeGenericType(domainEvent.GetType());

            return (INotification)
                Activator.CreateInstance(
                    notificationType,
                    domainEvent)!;
        }
    }
}