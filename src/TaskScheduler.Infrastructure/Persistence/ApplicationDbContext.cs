using Microsoft.EntityFrameworkCore;
using TaskScheduler.Domain.Entities;
using TaskScheduler.Domain.ValueObjects;

namespace TaskScheduler.Infrastructure.Persistence
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<ScheduledTask> ScheduledTasks => Set<ScheduledTask>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ScheduledTask>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.Name).IsRequired();

                entity.Property(x => x.Description);    

                entity.Property(x => x.Command).IsRequired();

                entity.Property(x => x.CronExpression)
                    .HasConversion(
                        v => v.Value,                   
                        v => CronExpression.Create(v)   
                    )
                    .HasMaxLength(100)
                    .IsRequired();
            });
        }
    }
}