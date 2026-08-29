using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TodoListAPI.Models
{
    public class TodoList
    {
        [Key]
        public int TodoListId { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        public string UserId { get; set; } = string.Empty;

        [ForeignKey(nameof(UserId))]
        public IdentityUser User { get; set; } = null!;

        public ICollection<TodoTask> Tasks { get; set; }
            = new List<TodoTask>();
    }
}