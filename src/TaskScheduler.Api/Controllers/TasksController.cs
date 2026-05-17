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
using TaskScheduler.Application.Tasks.Queries.GetTaskExecutionLogs;
using TaskScheduler.Application.Tasks.Queries.GetTasksDashBoard;

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

        [HttpPost("create")]
        [SwaggerOperation(Summary = "Creates a new scheduled task.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Create(CreateTaskCommand cmd)
        {
            var result = await _mediator.Send(cmd);
            return Success(result,"Task Created successfully");        
    }

        [HttpGet("getall")]
        [SwaggerOperation(Summary = "Retrieves all scheduled tasks.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Get()
        {
            var result = await _mediator.Send(new GetTasksQuery());
            return Success(result, "Success");
        }

        [HttpPatch("update")]
        [SwaggerOperation(Summary = "Update scheduled task.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Update(UpdateTaskCommand cmd)
        {
            var result = await _mediator.Send(cmd);
            return Success(result,"Task Updated successfully");        
        }

        [HttpGet("getbyid")]
        [SwaggerOperation(Summary = "Retrieves details scheduled tasks.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _mediator.Send(new GetTaskByIdQuery(id));
            return Success(result, "Success");
        }

        [HttpDelete("delete")]
        [SwaggerOperation(Summary = "Delete scheduled task.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Delete(DeleteTaskCommand cmd)
        {
            var result = await _mediator.Send(cmd);
            return Success(result,"Task Deleted successfully");        
        }

        [HttpPatch("pause")]
        [SwaggerOperation(Summary = "Pause scheduled task.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Pause(PauseTaskCommand cmd)
        {
            var result = await _mediator.Send(cmd);
            return Success(result,"Task Paused successfully");        
        }

        [HttpPatch("resume")]
        [SwaggerOperation(Summary = "Resume scheduled task.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Resume(ResumeTaskCommand cmd)
        {
            var result = await _mediator.Send(cmd);
            return Success(result,"Task has continue successfully");        
        }

        [HttpPatch("trigger")]
        [SwaggerOperation(Summary = "Trigger scheduled task.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Trigger(TriggerTaskCommand cmd)
        {
            var result = await _mediator.Send(cmd);
            return Success(result,"complete Task successfully.");        
        }

        [HttpGet("getExecutionlogs")]
        [SwaggerOperation(Summary = "Retrieves execution logs for a scheduled task.")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetExecutionLogs(Guid id)
        {
            var result = await _mediator.Send(new GetTaskExecutionLogsQuery(id));
            return Success(result, "Success");
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