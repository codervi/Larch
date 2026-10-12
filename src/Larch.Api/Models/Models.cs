using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Larch.Api.Models;

public class AppUser : IdentityUser
{
    public string DisplayName { get; set; } = "";
}

public enum Priority { Low, Normal, High }

public class Board
{
    public int Id { get; set; }

    [Required, StringLength(80)]
    public string Name { get; set; } = "";

    public string OwnerId { get; set; } = "";
    public AppUser? Owner { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public List<Column> Columns { get; set; } = new();
}

public class Column
{
    public int Id { get; set; }
    public int BoardId { get; set; }
    public Board? Board { get; set; }

    [Required, StringLength(40)]
    public string Name { get; set; } = "";

    // columns are shown left to right by this number
    public int Position { get; set; }

    // cards in a "done" column are never shown as overdue
    public bool IsDone { get; set; }

    public List<Card> Cards { get; set; } = new();
}

public class Card
{
    public int Id { get; set; }
    public int ColumnId { get; set; }
    public Column? Column { get; set; }

    [Required, StringLength(120)]
    public string Title { get; set; } = "";

    [StringLength(2000)]
    public string? Description { get; set; }

    public Priority Priority { get; set; } = Priority.Normal;

    public DateOnly? DueDate { get; set; }

    // top to bottom inside its column: 0, 1, 2, ...
    public int Position { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}

// A short history line per board: "Sam moved 'Fix login' from Doing to Done".
public class Activity
{
    public int Id { get; set; }
    public int BoardId { get; set; }
    public Board? Board { get; set; }

    [StringLength(300)]
    public string Text { get; set; } = "";

    public DateTime AtUtc { get; set; }
}
