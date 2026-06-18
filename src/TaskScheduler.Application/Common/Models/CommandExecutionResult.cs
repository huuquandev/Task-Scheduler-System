using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TaskScheduler.Application.Common.Models
{
    public class CommandExecutionResult
    {
        public bool Success { get; set; }

        public int ExitCode { get; set; }

        public string StandardOutput { get; set; } = string.Empty;

        public string StandardError { get; set; } = string.Empty;

        public TimeSpan Duration { get; set; }
    }
}