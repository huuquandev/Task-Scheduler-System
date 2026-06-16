using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using TaskScheduler.Application.Interfaces;
using TaskScheduler.Application.Auth.DTOs;
namespace TaskScheduler.Application.Auth.Commands.AuthLogin
{
    public record LoginCommand(string Username, string Password) : IRequest<AuthResponse>, ISensitiveRequest;
}