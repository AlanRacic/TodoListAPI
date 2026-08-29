using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoListAPI.Contracts;
using TodoListAPI.Data;
using TodoListAPI.Models;

namespace TodoListAPI.Controllers.Api
{
    [ApiController]
    [Route("api")]
    [Authorize]
    public class TodoApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public TodoApiController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET /api/todolists
        [HttpGet("todolists")]
        public async Task<IActionResult> GetLists()
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Unauthorized();
            }

            var lists = await _context.TodoLists
                .AsNoTracking()
                .Where(list => list.UserId == userId)
                .Select(list => new TodoListResponse(
                    list.TodoListId,
                    list.Title,
                    list.Tasks
                        .Select(task => new TodoTaskResponse(
                            task.TodoTaskId,
                            task.Title,
                            task.Status))
                        .ToList()))
                .ToListAsync();

            return Ok(lists);
        }

        // POST /api/todolists
        [HttpPost("todolists")]
        public async Task<IActionResult> CreateList(
            [FromBody] CreateListRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return BadRequest("Title is required.");
            }

            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Unauthorized();
            }

            var list = new TodoList
            {
                Title = request.Title.Trim(),
                UserId = userId
            };

            _context.TodoLists.Add(list);
            await _context.SaveChangesAsync();

            var response = new TodoListResponse(
                list.TodoListId,
                list.Title,
                Array.Empty<TodoTaskResponse>());

            return CreatedAtAction(
                nameof(GetLists),
                response);
        }

        // POST /api/todolists/{listId}/tasks
        [HttpPost("todolists/{listId:int}/tasks")]
        public async Task<IActionResult> AddTask(int listId, [FromBody] AddTaskRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return BadRequest("Task title is required.");
            }

            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Unauthorized();
            }

            var listExists = await _context.TodoLists.AnyAsync(list =>
                    list.TodoListId == listId &&
                    list.UserId == userId);

            if (!listExists)
            {
                return NotFound("List not found.");
            }

            var task = new TodoTask
            {
                Title = request.Title.Trim(),
                TodoListId = listId
            };

            _context.TodoTasks.Add(task);
            await _context.SaveChangesAsync();

            var response = new TodoTaskResponse(
                task.TodoTaskId,
                task.Title,
                task.Status);

            return Ok(response);
        }

        // PUT /api/todotasks/{taskId}/done
        [HttpPut("todotasks/{taskId:int}/done")]
        public async Task<IActionResult> MarkDone(int taskId)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Unauthorized();
            }

            var task = await _context.TodoTasks
                .Include(task => task.TodoList)
                .FirstOrDefaultAsync(task =>
                    task.TodoTaskId == taskId &&
                    task.TodoList.UserId == userId);

            if (task == null)
            {
                return NotFound("Task not found.");
            }

            task.Status = true;
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}