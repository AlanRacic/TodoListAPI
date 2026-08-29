using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoListAPI.Data;
using TodoListAPI.Models;

namespace TodoListAPI.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ProfileController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Challenge();
            }

            var lists = await _context.TodoLists
                .Where(list => list.UserId == userId)
                .Include(list => list.Tasks)
                .AsNoTracking()
                .ToListAsync();

            return View(lists);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateList(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                return RedirectToAction(nameof(Index));
            }

            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Challenge();
            }

            var list = new TodoList
            {
                Title = title.Trim(),
                UserId = userId
            };

            _context.TodoLists.Add(list);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTask(int listId, string taskTitle)
        {
            if (string.IsNullOrWhiteSpace(taskTitle))
            {
                return RedirectToAction(nameof(Index));
            }

            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Challenge();
            }

            var listExists = await _context.TodoLists.AnyAsync(list =>
                    list.TodoListId == listId &&
                    list.UserId == userId);

            if (!listExists)
            {
                return NotFound();
            }

            var task = new TodoTask
            {
                Title = taskTitle.Trim(),
                TodoListId = listId
            };

            _context.TodoTasks.Add(task);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkDone(int taskId)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return Challenge();
            }

            var task = await _context.TodoTasks
                .Include(task => task.TodoList)
                .FirstOrDefaultAsync(task =>
                    task.TodoTaskId == taskId &&
                    task.TodoList.UserId == userId);

            if (task == null)
            {
                return NotFound();
            }

            task.Status = true;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}