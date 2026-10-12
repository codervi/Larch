using Larch.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Larch.Api.Data;

public static class SeedData
{
    public static async Task RunAsync(IServiceProvider services, bool development)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        if (!development || await db.Users.AnyAsync()) return;

        var password = sp.GetRequiredService<IConfiguration>()["SeedUsers:DemoPassword"];
        if (string.IsNullOrWhiteSpace(password)) return;

        var users = sp.GetRequiredService<UserManager<AppUser>>();
        var sam = new AppUser { UserName = "sam@larch.test", Email = "sam@larch.test", DisplayName = "Sam Carter", EmailConfirmed = true };
        var result = await users.CreateAsync(sam, password);
        if (!result.Succeeded) throw new InvalidOperationException("Demo user failed: " + result.Errors.First().Description);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var now = DateTime.UtcNow;

        Card C(string title, string? text, Priority p, int? dueInDays, int pos) => new()
        {
            Title = title, Description = text, Priority = p, Position = pos, CreatedAtUtc = now,
            DueDate = dueInDays.HasValue ? today.AddDays(dueInDays.Value) : null,
        };

        var board = new Board
        {
            Name = "Website relaunch",
            OwnerId = sam.Id,
            CreatedAtUtc = now,
            Columns =
            {
                new Column { Name = "To do", Position = 0, Cards =
                {
                    C("Write the new About page", "Short, with the team photo from last month.", Priority.Normal, 6, 0),
                    C("Check contrast on the buttons", null, Priority.Low, null, 1),
                    C("Set up redirects for old URLs", "List is in the shared sheet. Do this before launch day.", Priority.High, 2, 2),
                    C("Order business cards", null, Priority.Low, -3, 3),
                }},
                new Column { Name = "Doing", Position = 1, Cards =
                {
                    C("Build the contact form", "Needs spam protection and a thank-you page.", Priority.High, 1, 0),
                    C("Compress hero images", null, Priority.Normal, 3, 1),
                }},
                new Column { Name = "Done", Position = 2, IsDone = true, Cards =
                {
                    C("Pick the colour palette", null, Priority.Normal, -10, 0),
                    C("Buy the domain", null, Priority.Low, null, 1),
                    C("Draft the site map", "Six pages, one level deep.", Priority.Normal, -7, 2),
                }},
            },
        };
        db.Boards.Add(board);
        db.Boards.Add(new Board { Name = "Home move", OwnerId = sam.Id, CreatedAtUtc = now, Columns =
        {
            new Column { Name = "To do", Position = 0, Cards = { C("Book the van", null, Priority.High, 5, 0), C("Cancel the old internet contract", null, Priority.Normal, null, 1) } },
            new Column { Name = "Doing", Position = 1 },
            new Column { Name = "Done", Position = 2, IsDone = true },
        }});
        await db.SaveChangesAsync();

        db.Activities.Add(new Activity { BoardId = board.Id, AtUtc = now.AddHours(-5), Text = "Sam Carter moved \"Pick the colour palette\" from Doing to Done" });
        db.Activities.Add(new Activity { BoardId = board.Id, AtUtc = now.AddHours(-1), Text = "Sam Carter moved \"Build the contact form\" from To do to Doing" });
        await db.SaveChangesAsync();
    }
}
