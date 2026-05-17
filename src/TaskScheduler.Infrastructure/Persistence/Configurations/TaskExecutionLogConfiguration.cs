using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskScheduler.Domain.Entities;

namespace TaskScheduler.Infrastructure.Persistence.Configurations
{
    public class TaskExecutionLogConfiguration: IEntityTypeConfiguration<TaskExecutionLog>
    {
        public void Configure(EntityTypeBuilder<TaskExecutionLog> builder)
        {
            builder.HasKey(x => x.Id);

            builder.HasOne(x => x.ScheduledTask)
                .WithMany()
                .HasForeignKey(x => x.TaskId);

            builder.Property(x => x.ErrorMessage)
                .HasMaxLength(1000);
        }
    }
}