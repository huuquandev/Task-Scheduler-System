using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskScheduler.Domain.Entities;

namespace TaskScheduler.Domain.Tests.Builders
{
    public class ScheduledTaskBuilder
    {
        private string _name = "Backup Database";
        private string _description = "Daily backup";
        private string _cron = "0 0 * * *";
        private string _command = "backup.exe";
        private int _maxRetries = 3;

        public ScheduledTaskBuilder WithName(string name)
        {
            _name = name;
            return this;
        }

        public ScheduledTaskBuilder WithDescription(string description)
        {
            _description = description;
            return this;
        }

        public ScheduledTaskBuilder WithCron(string cron)
        {
            _cron = cron;
            return this;
        }

        public ScheduledTaskBuilder WithCommand(string command)
        {
            _command = command;
            return this;
        }

        public ScheduledTaskBuilder WithMaxRetries(int retries)
        {
            _maxRetries = retries;
            return this;
        }

        public ScheduledTask Build()
        {
            return new ScheduledTask(
                _name,
                _description,
                _cron,
                _command,
                _maxRetries);
        }
    }
}