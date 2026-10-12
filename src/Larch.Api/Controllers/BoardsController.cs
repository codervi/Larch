using Larch.Api.Data;
using Larch.Api.Dtos;
using Larch.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Larch.Api.Controllers;

[Route("api/boards")]
public class BoardsController : OwnedController
{
    public BoardsController(AppDbContext db) : base(db) { }

    [HttpGet]
    public async Task<List<BoardSummaryDto>> List()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // counts are worked out in SQL, no cards are loaded
        return await MyBoards.AsNoTracking()
            .OrderBy(b => b.Name)
            .Select(b => new BoardSummaryDto(
                b.Id, b.Name,
                b.Columns.SelectMany(c => c.Cards).Count(),
                b.Columns.Where(c => !c.IsDone).SelectMany(c => c.Cards).Count(k => k.DueDate < today)))
            .ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<BoardSummaryDto>> Create(BoardInput input)
    {
        if (await MyBoards.CountAsync() >= 20)
            return Problem(detail: "You can have up to 20 boards.", statusCode: 409);

        var board = new Board
        {
            Name = input.Name.Trim(),
            OwnerId = UserId,
            CreatedAtUtc = DateTime.UtcNow,
            Columns =
            {
                new Column { Name = "To do", Position = 0 },
                new Column { Name = "Doing", Position = 1 },
                new Column { Name = "Done", Position = 2, IsDone = true },
            },
        };
        Db.Boards.Add(board);
        await Db.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = board.Id }, new BoardSummaryDto(board.Id, board.Name, 0, 0));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<BoardDto>> Get(int id)
    {
        var board = await MyBoards.AsNoTracking()
            .Where(b => b.Id == id)
            .Select(b => new BoardDto(
                b.Id, b.Name,
                b.Columns.OrderBy(c => c.Position).Select(c => new ColumnDto(
                    c.Id, c.Name, c.IsDone,
                    c.Cards.OrderBy(k => k.Position)
                        .Select(k => new CardDto(k.Id, k.ColumnId, k.Title, k.Description, k.Priority, k.DueDate))
                        .ToList()))
                    .ToList()))
            .FirstOrDefaultAsync();

        return board == null ? NotFound() : board;
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Rename(int id, BoardInput input)
    {
        var updated = await MyBoards.Where(b => b.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.Name, input.Name.Trim()));
        return updated == 0 ? NotFound() : NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        // columns, cards and history go with it (cascade delete in the database)
        var deleted = await MyBoards.Where(b => b.Id == id).ExecuteDeleteAsync();
        return deleted == 0 ? NotFound() : NoContent();
    }

    [HttpGet("{id:int}/activity")]
    public async Task<ActionResult<List<ActivityDto>>> Activity(int id)
    {
        if (!await MyBoards.AnyAsync(b => b.Id == id)) return NotFound();

        return await Db.Activities.AsNoTracking()
            .Where(a => a.BoardId == id)
            .OrderByDescending(a => a.AtUtc).ThenByDescending(a => a.Id)
            .Take(20)
            .Select(a => new ActivityDto(a.Id, a.Text, a.AtUtc))
            .ToListAsync();
    }

    [HttpPost("{id:int}/columns")]
    public async Task<ActionResult<ColumnDto>> AddColumn(int id, ColumnInput input)
    {
        if (!await MyBoards.AnyAsync(b => b.Id == id)) return NotFound();

        var count = await Db.Columns.CountAsync(c => c.BoardId == id);
        if (count >= 8) return Problem(detail: "A board can have up to 8 columns.", statusCode: 409);

        var column = new Column { BoardId = id, Name = input.Name.Trim(), IsDone = input.IsDone, Position = count };
        Db.Columns.Add(column);
        await Db.SaveChangesAsync();

        return new ColumnDto(column.Id, column.Name, column.IsDone, new());
    }
}
