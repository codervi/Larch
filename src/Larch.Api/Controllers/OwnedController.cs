using System.Security.Claims;
using Larch.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Larch.Api.Controllers;

// Base class for everything behind the login. All queries in the controllers below start
// from a board that belongs to the signed-in user, so somebody else's id simply gives 404.
[ApiController, Authorize]
public abstract class OwnedController : ControllerBase
{
    protected readonly AppDbContext Db;

    protected OwnedController(AppDbContext db) => Db = db;

    protected string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    protected IQueryable<Models.Board> MyBoards => Db.Boards.Where(b => b.OwnerId == UserId);
    protected IQueryable<Models.Column> MyColumns => Db.Columns.Where(c => c.Board!.OwnerId == UserId);
    protected IQueryable<Models.Card> MyCards => Db.Cards.Where(c => c.Column!.Board!.OwnerId == UserId);

    protected async Task<string> MyNameAsync() =>
        await Db.Users.Where(u => u.Id == UserId).Select(u => u.DisplayName).FirstAsync();

    protected void Log(int boardId, string text) =>
        Db.Activities.Add(new Models.Activity { BoardId = boardId, Text = text, AtUtc = DateTime.UtcNow });
}
