using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using TaskScheduler.Application.Tasks.Queries.GetTasksDashBoard;

namespace TaskScheduler.Api.Controllers
{
    public class DashboardController : BaseApiController
    {
        private readonly IMediator _mediator;

        public DashboardController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("gettaskdashboard")]
        [SwaggerOperation(Summary = "Retrieves task dashboard information for a scheduled task.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTaskDashboard()
        {
            var result = await _mediator.Send(new GetTasksDashboardQuery());
            return Success(result, "Success");
        }
    }
}