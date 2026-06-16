using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;

namespace TaskScheduler.Application.Common.Telemetry
{
    public class TelemetryConfig
    {
        public const string ServiceName = "TaskScheduler";

        public static readonly ActivitySource ActivitySource = new(ServiceName);
    }
}