using System;
using System.Numerics;

namespace dotnetClaimAuthorization
{
    public class TodoItemTask
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public DateTime Date { get; set; }
        public Guid UserId { get; set; }

        public Boolean Completed { get; set; }
    }
}