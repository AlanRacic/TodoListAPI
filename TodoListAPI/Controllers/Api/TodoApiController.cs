using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
            var user = await _userManager.GetUserAsync(User);

            var lists = await _context.TodoLists
                .Include(l => l.Tasks)
                .Where(l => l.UserId == user!.Id)
                .ToListAsync();

            return Ok(lists);
        }

        // POST /api/todolists
        [HttpPost("todolists")]
        public async Task<IActionResult> CreateList([FromBody] CreateListRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
                return BadRequest("Title is required.");

            var user = await _userManager.GetUserAsync(User);

            var list = new TodoList
            {
                Title = request.Title.Trim(),
                UserId = user!.Id
            };

            _context.TodoLists.Add(list);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetLists), new { id = list.TodoListId }, list);
        }

        // POST /api/todolists/{listId}/tasks
        [HttpPost("todolists/{listId:int}/tasks")]
        public async Task<IActionResult> AddTask(int listId, [FromBody] AddTaskRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
                return BadRequest("Task title is required.");

            var user = await _userManager.GetUserAsync(User);

            // List belongs to the logged-in user
            var list = await _context.TodoLists
                .FirstOrDefaultAsync(l => l.TodoListId == listId && l.UserId == user!.Id);

            if (list == null)
                return NotFound("List not found.");

            var task = new TodoTask
            {
                Title = request.Title.Trim(),
                TodoListId = listId
            };

            _context.TodoTasks.Add(task);
            await _context.SaveChangesAsync();

            return Ok(task);
        }

        // PUT /api/todotasks/{taskId}/done
        [HttpPut("todotasks/{taskId:int}/done")]
        public async Task<IActionResult> MarkDone(int taskId)
        {
            var user = await _userManager.GetUserAsync(User);

            // Task belongs to a list owned by the logged-in user
            var task = await _context.TodoTasks
                .Include(t => t.TodoList)
                .FirstOrDefaultAsync(t => t.TodoTaskId == taskId && t.TodoList.UserId == user!.Id);

            if (task == null)
                return NotFound("Task not found.");

            task.Status = true;
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }

    // Simple request DTOs
    public record CreateListRequest(string Title);
    public record AddTaskRequest(string Title);
}