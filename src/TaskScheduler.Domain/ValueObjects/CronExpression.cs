using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cronos;

namespace TaskScheduler.Domain.ValueObjects
{
    public sealed class CronExpression : IEquatable<CronExpression>
    {
        public string Value { get; private set; } = default;
        private CronExpression()
        {
        }
        private CronExpression(string value)
        {
            Value = value;
        }
        public static CronExpression Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Cron expression cannot be empty.");
            }

            try
            {
                Cronos.CronExpression.Parse(value);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Invalid cron expression: {value}", ex);
            }

            return new CronExpression(value);
        }
        public override bool Equals(object? obj)
        {
            return Equals(obj as CronExpression);
        }

        public bool Equals(CronExpression? other)
        {
            if (other is null)
            {
                return false;
            }

            return Value == other.Value;
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override string ToString()
        {
            return Value;
        }
        public DateTime? GetNextOccurrence(DateTime from)
        {
            var cron = Cronos.CronExpression.Parse(Value);

            return cron.GetNextOccurrence(from, TimeZoneInfo.Utc);
        }
    }
}