using System.Collections.Generic;
using Newtonsoft.Json;
using System.Net.Http;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using Microsoft.Extensions.Logging;

namespace RazorPages.Todo
{
    public class InMemoryTodoService : ITodoService
    {
        private List<TodoItem> _todos;

        public InMemoryTodoService()
        {
            _todos = new List<TodoItem>();
        }

        public Task<IEnumerable<TodoItem>> GetTodoItems()
        {
            return Task.FromResult(_todos as IEnumerable<TodoItem>);
        }

        public Task AddTodoItem(TodoItem item)
        {
            _todos.Add(item);
            return Task.CompletedTask;
        }

        public Task RemoveTodoItem(TodoItem item)
        {
            _todos.Remove(_todos.Single(x=> item.Id == x.Id));
            return Task.CompletedTask;
        }
    }
}