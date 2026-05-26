using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskScheduler.Domain.Entities;

namespace TaskScheduler.Domain.Tests.Builders
{
    public class UserBuilder
    {
        private string _username = "john";
        private string _email = "john@test.com";
        private string _passwordHash = "hash";

        public UserBuilder WithUsername(string username)
        {
            _username = username;
            return this;
        }

        public UserBuilder WithEmail(string email)
        {
            _email = email;
            return this;
        }

        public UserBuilder WithPasswordHash(string passwordHash)
        {
            _passwordHash = passwordHash;
            return this;
        }

        public User Build()
        {
            return new User(
                _username,
                _email,
                _passwordHash);
        }
    }
}