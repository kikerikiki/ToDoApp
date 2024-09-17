using System;
using System.Collections.Generic;

namespace ToDoApplication.Models;

public partial class Todo
{
    public int Id { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime? DueDate { get; set; }

    public bool IsCompleted { get; set; } = false;

    public DateTime? CreatedAt { get; set; }
}
