using Larch.Api.Data;
using Larch.Api.Dtos;
using Larch.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Larch.Api.Controllers;

[Route("api/columns")]
public class ColumnsController : OwnedController
{
    public ColumnsController(AppDbContext db) : base(db) { }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, ColumnInput input)
    {
        var column = await MyColumns.FirstOrDefaultAsync(c => c.Id == id);
        if (column == null) return NotFound();

        column.Name = input.Name.Trim();
        column.IsDone = input.IsDone;
        await Db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var column = await MyColumns.FirstOrDefaultAsync(c => c.Id == id);
        if (column == null) return NotFound();

        if (await Db.Cards.AnyAsync(k => k.ColumnId == id))
            return Problem(detail: "Move or delete the cards in this column first.", statusCode: 409);

        // the columns to the right slide one place to the left
        var others = await Db.Columns.Where(c => c.BoardId == column.BoardId && c.Id != id)
            .OrderBy(c => c.Position).ToListAsync();
        for (var i = 0; i < others.Count; i++) others[i].Position = i;

        Db.Columns.Remove(column);
        await Db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id:int}/cards")]
    public async Task<ActionResult<CardDto>> AddCard(int id, CardInput input)
    {
        if (!await MyColumns.AnyAsync(c => c.Id == id)) return NotFound();

        var count = await Db.Cards.CountAsync(k => k.ColumnId == id);
        if (count >= 200) return Problem(detail: "This column is full (200 cards).", statusCode: 409);

        var card = new Card
        {
            ColumnId = id,
            Title = input.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim(),
            Priority = input.Priority,
            DueDate = input.DueDate,
            Position = count,          // goes to the bottom
            CreatedAtUtc = DateTime.UtcNow,
        };
        Db.Cards.Add(card);
        await Db.SaveChangesAsync();

        return new CardDto(card.Id, card.ColumnId, card.Title, card.Description, card.Priority, card.DueDate);
    }
}
