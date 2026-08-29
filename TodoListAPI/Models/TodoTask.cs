using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TodoListAPI.Models
{
    public class TodoTask
    {
        [Key]
        public int TodoTaskId { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        public bool Status { get; set; }

        public int TodoListId { get; set; }

        [ForeignKey(nameof(TodoListId))]
        public TodoList TodoList { get; set; } = null!;
    }
}