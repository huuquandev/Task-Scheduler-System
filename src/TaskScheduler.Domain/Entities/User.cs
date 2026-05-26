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

        public User()
        {
        }

        public User(string username, string email, string passwordHash)
        {
            if(string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("Username cannot be empty.");

            if(string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email cannot be empty.");

            if(string.IsNullOrWhiteSpace(passwordHash))
                throw new ArgumentException("Password cannot be empty.");

            Id = Guid.NewGuid();
            Username = username;
            Email = email;
            PasswordHash = passwordHash;

            IsActive = true;

            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Activate()
        {
            IsActive = true;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Deactivate()
        {
            IsActive = false;
            UpdatedAt = DateTime.UtcNow;
        }

        public void ChangePassword(string passwordHash)
        {
            if(string.IsNullOrWhiteSpace(passwordHash))
                throw new ArgumentException("Password cannot be empty." );

            PasswordHash = passwordHash;

            UpdatedAt = DateTime.UtcNow;
        }
    }
}