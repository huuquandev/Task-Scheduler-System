using System;
using System.Collections.Generic;
using System.Linq;
using MediatR;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using TaskScheduler.Application.Tasks.Commands.CreateTask;
using TaskScheduler.Application.Tasks.Queries.GetTasks;
using TaskScheduler.Application.Tasks.Commands.UpdateTask;
using TaskScheduler.Application.Tasks.Queries.GetTaskById;
using TaskScheduler.Application.Tasks.Commands.DeleteTask;
using TaskScheduler.Application.Common.Models;
using TaskScheduler.Application.Tasks.Commands.PauseTask;
using TaskScheduler.Application.Tasks.Commands.ResumeTask;
using TaskScheduler.Application.Tasks.Commands.TriggerTask;
using TaskScheduler.Application.Tasks.Commands.ActiveTask;
using TaskScheduler.Application.Tasks.Queries.GetTasksPaged;
using Microsoft.AspNetCore.Authorization;
using TaskScheduler.Domain.Enums;
using Microsoft.AspNetCore.RateLimiting;
namespace TaskScheduler.Api.Controllers
{
    [ApiController]
    [Route("api/v1/tasks")]
    [Authorize]
    public class TasksController : BaseApiController
    {
        private readonly IMediator _mediator;

        public TasksController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        [SwaggerOperation(Summary = "Creates a new scheduled task.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Create([FromBody] CreateTaskCommand cmd)
        {
            var result = await _mediator.Send(cmd);
            return Success(result,"Task Created successfully");        
        }

        [HttpGet]
        [SwaggerOperation(Summary = "Retrieves all scheduled tasks.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Get()
        {
            var result = await _mediator.Send(new GetTasksQuery());
            return Success(result, "Success");
        }

        [HttpPut("{id}")]
        [SwaggerOperation(Summary = "Update scheduled task.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateTaskCommand cmd)
        {
            cmd = cmd with { Id = id };
            var result = await _mediator.Send(cmd);
            return Success(result,"Task Updated successfully");        
        }

        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "Retrieves details scheduled tasks.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById([FromRoute] Guid id)
        {
            var result = await _mediator.Send(new GetTaskByIdQuery(id));
            return Success(result, "Success");
        }

        [HttpDelete("{id}")]
        [SwaggerOperation(Summary = "Delete scheduled task.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Delete([FromRoute] Guid id)
        {
            var result = await _mediator.Send(new DeleteTaskCommand(id));
            return Success(result,"Task Deleted successfully");        
        }

        [HttpPost("{id}/pause")]
        [SwaggerOperation(Summary = "Pause scheduled task.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Pause([FromRoute] Guid id)
        {
            var result = await _mediator.Send(new PauseTaskCommand(id));
            return Success(result,"Task Paused successfully");        
        }

        [HttpPost("{id}/resume")]
        [SwaggerOperation(Summary = "Resume scheduled task.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Resume([FromRoute] Guid id)
        {
            var result = await _mediator.Send(new ResumeTaskCommand(id));
            return Success(result,"Task has resumed successfully");        
        }

        [HttpPost("{id}/trigger")]
        [EnableRateLimiting("trigger-policy")]
        [SwaggerOperation(Summary = "Trigger scheduled task.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Trigger([FromRoute] Guid id)
        {
            var result = await _mediator.Send(new TriggerTaskCommand(id));
            return Success(result,"Task triggered successfully.");        
        }
        
        [HttpGet("paged")]
        [SwaggerOperation(Summary = "Retrieve paged scheduled tasks.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] ScheduledTaskStatus? status = null)
        {
            var result = await _mediator.Send(new GetTasksPagedQuery(page, pageSize, status));
            return Success(result, "Success");
        }
        
        [HttpPost("{id}/activate")]
        [SwaggerOperation(Summary = "Activate a scheduled task.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Activate([FromRoute] Guid id)
        {
            var result = await _mediator.Send(new ActiveTaskCommand(id));
            return Success(result,"Task has activated successfully");        
        }
    }
}