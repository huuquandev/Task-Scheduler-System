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
namespace TaskScheduler.Api.Controllers
{
    [ApiController]
    [Route("api/v1/tasks")]
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
        public async Task<IActionResult> Create(CreateTaskCommand cmd)
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
        public async Task<IActionResult> Update(Guid id, UpdateTaskCommand cmd)
        {
            cmd = cmd with { Id = id };
            var result = await _mediator.Send(cmd);
            return Success(result,"Task Updated successfully");        
        }

        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "Retrieves details scheduled tasks.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _mediator.Send(new GetTaskByIdQuery(id));
            return Success(result, "Success");
        }

        [HttpDelete("{id}")]
        [SwaggerOperation(Summary = "Delete scheduled task.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Delete(Guid id, DeleteTaskCommand cmd)
        {
            cmd = cmd with { Id = id };
            var result = await _mediator.Send(cmd);
            return Success(result,"Task Deleted successfully");        
        }

        [HttpPost("{id}/pause")]
        [SwaggerOperation(Summary = "Pause scheduled task.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Pause(Guid id, PauseTaskCommand cmd)
        {
            cmd = cmd with { Id = id };
            var result = await _mediator.Send(cmd);
            return Success(result,"Task Paused successfully");        
        }

        [HttpPost("{id}/resume")]
        [SwaggerOperation(Summary = "Resume scheduled task.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Resume(Guid id, ResumeTaskCommand cmd)
        {
            cmd = cmd with { Id = id };
            var result = await _mediator.Send(cmd);
            return Success(result,"Task has continue successfully");        
        }

        [HttpPost("{id}/trigger")]
        [SwaggerOperation(Summary = "Trigger scheduled task.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Trigger(Guid id, TriggerTaskCommand cmd)
        {
            cmd = cmd with { Id = id };
            var result = await _mediator.Send(cmd);
            return Success(result,"complete Task successfully.");        
        }
        
        [HttpGet("{page}/{pageSize}")]
        [SwaggerOperation(Summary = "Retrieve paged scheduled tasks.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPaged(int page, int pageSize)
        {
            var result = await _mediator.Send(new GetTasksPagedQuery(page, pageSize));
            return Success(result, "Success");
        }
        
        [HttpGet("{id}/activate")]
        [SwaggerOperation(Summary = "Activate a scheduled task.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Activate(Guid id, ActiveTaskCommand cmd)
        {
            cmd = cmd with { Id = id };
            var result = await _mediator.Send(cmd);
            return Success(result,"Task has activated successfully");        
        }
    }
}