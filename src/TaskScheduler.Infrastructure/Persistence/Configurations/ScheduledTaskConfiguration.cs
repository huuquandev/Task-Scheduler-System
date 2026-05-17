using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskScheduler.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskScheduler.Domain.Entities;

namespace TaskScheduler.Infrastructure.Persistence.Configurations
{
    public class ScheduledTaskConfiguration: IEntityTypeConfiguration<ScheduledTask>
    {
        public void Configure(EntityTypeBuilder<ScheduledTask> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.Description)
                .HasMaxLength(256);

            builder.Property(x => x.Command)
                .IsRequired()
                .HasMaxLength(256);

            builder.Property(x => x.CronExpression)
                .HasConversion(
                    v => v.Value,
                    v => CronExpression.Create(v)
                )
                .HasMaxLength(100)
                .IsRequired();
        }
    }
}
