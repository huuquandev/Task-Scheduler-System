using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TaskScheduler.Application.Common.Models;

namespace TaskScheduler.Api.Controllers
{
    [ApiController]
    public class BaseApiController : ControllerBase
    {
        protected IActionResult Success<T>(
            T data,
            string message = "Success")
        {
            return Ok(new ApiResponse<T>
            {
                Code = 0,
                Message = message,
                Data = data
            });
        }

        protected IActionResult Fail(
            string message,
            int code = 1)
        {
            return BadRequest(new ApiResponse<object>
            {
                Code = code,
                Message = message,
                Data = null
            });
        }
    }
}