using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using TaskScheduler.Application.Auth.Commands.AuthLogin;
using TaskScheduler.Application.Auth.Commands.AuthRegister;

namespace TaskScheduler.Api.Controllers
{
    [ApiController]
    [Route("api/v1/auth")]
    public class AuthController : BaseApiController
    {
        private readonly IMediator _mediator;

        public AuthController(IMediator mediator)
        {
            _mediator = mediator;
        }
        [HttpPost("login")]
        [SwaggerOperation(Summary = "Login and receive JWT token.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login(LoginCommand cmd)
        {
            var result = await _mediator.Send(cmd);
            return Success(result, "Login successful.");
        }

        [HttpPost("register")]
        [SwaggerOperation(Summary = "Register a new user account.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register(RegisterCommand cmd)
        {
            var result = await _mediator.Send(cmd);
            return Success(result, "Registration successful.");
        }
    }
}