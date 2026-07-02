using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using TaskScheduler.Application.Auth.DTOs;
using TaskScheduler.Application.Interfaces;

namespace TaskScheduler.Application.Auth.Commands.AuthLogin
{
    public class LoginHandler : IRequestHandler<LoginCommand, AuthResponse>
    {
        private readonly IUserRepository _userRepository;
          private readonly ITokenService _tokenService;

          public LoginHandler(IUserRepository userRepository, ITokenService tokenService)
          {
              _userRepository = userRepository;
              _tokenService = tokenService;
          }

          public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
          {
              var user = await _userRepository.GetByUsernameAsync(request.Username);
              if (user == null || !_tokenService.VerifyPassword(request.Password, user.PasswordHash))
                  throw new UnauthorizedAccessException("Invalid username or password.");

              if (!user.IsActive)
                  throw new InvalidOperationException("Account is disabled.");

              var token = _tokenService.GenerateJwtToken(user);

              return new AuthResponse
              {
                  Token = token,
                  Username = user.Username,
                  Email = user.Email
              };
          }
    }
}