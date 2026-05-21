using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskScheduler.Domain.Common;

namespace TaskScheduler.Domain.Entities
{
    public class User : BaseEntity
    {
        public Guid Id { get; set; } 
        public string Username { get; set; } = null!; 
        public string Email { get; set; } = null!; 
        public string PasswordHash { get; set; } = null!; 
        public DateTime? CreatedAt { get; set; } 
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; } = true;
    }
}