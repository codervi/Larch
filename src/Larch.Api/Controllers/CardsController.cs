using Larch.Api.Data;
using Larch.Api.Dtos;
using Larch.Api.Models;
using Larch.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Larch.Api.Controllers;

[Route("api/cards")]
public class CardsController : OwnedController
{
    public CardsController(AppDbContext db) : base(db) { }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<CardDto>> Update(int id, CardInput input)
    {
        var card = await MyCards.FirstOrDefaultAsync(k => k.Id == id);
        if (card == null) return NotFound();

        card.Title = input.Title.Trim();
        card.Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim();
        card.Priority = input.Priority;
        card.DueDate = input.DueDate;
        await Db.SaveChangesAsync();

        return new CardDto(card.Id, card.ColumnId, card.Title, card.Description, card.Priority, card.DueDate);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var card = await MyCards.FirstOrDefaultAsync(k => k.Id == id);
        if (card == null) return NotFound();

        var below = await Db.Cards.Where(k => k.ColumnId == card.ColumnId && k.Position > card.Position).ToListAsync();
        foreach (var k in below) k.Position--;

        Db.Cards.Remove(card);
        await Db.SaveChangesAsync();
        return NoContent();
    }

    // Drag and drop ends here: "put this card in that column at that index".
    // The browser has already redrawn the board; this makes it permanent.
    [HttpPost("{id:int}/move")]
    public async Task<IActionResult> Move(int id, MoveInput input)
    {
        var card = await MyCards.Include(k => k.Column).FirstOrDefaultAsync(k => k.Id == id);
        if (card == null) return NotFound();

        // the target column has to be on the same board (and so also belong to this user)
        var boardId = card.Column!.BoardId;
        var target = await Db.Columns.FirstOrDefaultAsync(c => c.Id == input.ColumnId && c.BoardId == boardId);
        if (target == null) return NotFound();

        var sourceId = card.ColumnId;
        var sourceName = card.Column.Name;

        var inTarget = await Db.Cards.Where(k => k.ColumnId == target.Id).OrderBy(k => k.Position).ToListAsync();
        var order = CardOrdering.Place(inTarget.Select(k => k.Id), card.Id, input.Index);

        var byId = inTarget.ToDictionary(k => k.Id);
        byId[card.Id] = card;
        card.ColumnId = target.Id;
        for (var i = 0; i < order.Count; i++) byId[order[i]].Position = i;

        if (sourceId != target.Id)
        {
            // close the gap the card left behind
            var left = await Db.Cards.Where(k => k.ColumnId == sourceId && k.Id != card.Id)
                .OrderBy(k => k.Position).ToListAsync();
            for (var i = 0; i < left.Count; i++) left[i].Position = i;

            Log(boardId, $"{await MyNameAsync()} moved \"{card.Title}\" from {sourceName} to {target.Name}");
        }

        await Db.SaveChangesAsync();
        return NoContent();
    }
}
