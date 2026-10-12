using Larch.Api.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Larch.Api.Data;

public class AppDbContext : IdentityDbContext<AppUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Board> Boards => Set<Board>();
    public DbSet<Column> Columns => Set<Column>();
    public DbSet<Card> Cards => Set<Card>();
    public DbSet<Activity> Activities => Set<Activity>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<Board>(e =>
        {
            e.HasIndex(x => x.OwnerId);
            e.HasOne(x => x.Owner).WithMany().HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Column>(e =>
        {
            // The order is rewritten in one go when a card moves, so there is deliberately no
            // UNIQUE here: during the rewrite two rows can briefly share a number.
            e.HasIndex(x => new { x.BoardId, x.Position });
            e.HasOne(x => x.Board).WithMany(x => x.Columns).HasForeignKey(x => x.BoardId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Card>(e =>
        {
            // "all cards of this column, in order" is the query the board page runs
            e.HasIndex(x => new { x.ColumnId, x.Position });
            e.Property(x => x.Priority).HasConversion<string>().HasMaxLength(10);
            e.Property(x => x.DueDate).HasColumnType("date");
            e.HasOne(x => x.Column).WithMany(x => x.Cards).HasForeignKey(x => x.ColumnId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Activity>(e =>
        {
            e.HasIndex(x => new { x.BoardId, x.AtUtc });
            e.HasOne(x => x.Board).WithMany().HasForeignKey(x => x.BoardId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
