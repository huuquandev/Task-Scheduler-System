using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using TaskScheduler.Application.Tasks.Queries.GetTaskExecutionLogs;
using TaskScheduler.Application.Tasks.Queries.GetDetailsTaskExecutionLog;
using Microsoft.AspNetCore.Authorization;
namespace TaskScheduler.Api.Controllers
{
    [ApiController]
    [Route("api/v1/tasks")]
    [Authorize]
    public class ExecutionLogsController : BaseApiController
    {
        private readonly IMediator _mediator;

        public ExecutionLogsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("{id}/logs")]
        [SwaggerOperation(Summary = "Retrieves execution logs for a scheduled task.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetExecutionLogs(Guid id)
        {
            var result = await _mediator.Send(new GetTaskExecutionLogsQuery(id));
            return Success(result, "Success");
        }

        [HttpGet("{id}/log/{logId}")]
        [SwaggerOperation(Summary = "Retrieves a specific execution log for a scheduled task.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDetailsExecutionLog(Guid id, Guid logId)
        {
            var result = await _mediator.Send(new GetDetailsTaskExecutionLogQuery(id, logId));
            return Success(result, "Success");
        }
    }
}