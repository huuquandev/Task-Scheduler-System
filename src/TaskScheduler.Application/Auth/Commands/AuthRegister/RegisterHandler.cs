using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using TaskScheduler.Application.Interfaces;
using TaskScheduler.Domain.Entities;
namespace TaskScheduler.Application.Auth.Commands.AuthRegister
{
     public class RegisterHandler : IRequestHandler<RegisterCommand, Guid>
      {
          private readonly IUserRepository _userRepository;
          private readonly ITokenService _tokenService;

          public RegisterHandler(IUserRepository userRepository, ITokenService tokenService)
          {
              _userRepository = userRepository;
              _tokenService = tokenService;
          }

          public async Task<Guid> Handle(RegisterCommand request, CancellationToken cancellationToken)
          {
              if (await _userRepository.UsernameExistsAsync(request.Username))
                  throw new InvalidOperationException("Username already exists.");

              if (await _userRepository.EmailExistsAsync(request.Email))
                  throw new InvalidOperationException("Email already exists.");

              var user = new User
              {
                  Id = Guid.NewGuid(),
                  Username = request.Username,
                  Email = request.Email,
                  PasswordHash = _tokenService.HashPassword(request.Password),
                  CreatedAt = DateTime.UtcNow,
                  IsActive = true
              };

              await _userRepository.AddAsync(user);

              return user.Id;
          }
      }
}