using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RazorPages.Todo
{
    public interface ITodoService
    {
        Task<IEnumerable<TodoItem>> GetTodoItems();
        Task AddTodoItem(TodoItem item);
        Task RemoveTodoItem(TodoItem item);
    }
}