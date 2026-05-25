using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;

namespace TaskScheduler.Application.Auth.Commands.AuthRegister
{
    public record RegisterCommand(string Username, string Email, string Password, string ConfirmPassword) : IRequest<Guid>;
}