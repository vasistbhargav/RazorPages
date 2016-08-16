
using System;
using System.ComponentModel.DataAnnotations;

namespace RazorPages.Todo
{
    public class TodoItem
    {
        public TodoItem()
        {
            Id = Guid.NewGuid();
        }

        [Required()]
        public Guid Id { get; set; }

        [Required()]
        [StringLength(255, MinimumLength = 5)]
        public string Description { get; set; }
    }
}
