using System.ComponentModel.DataAnnotations;
using Larch.Api.Models;

namespace Larch.Api.Dtos;

public record RegisterRequest(
    [Required, StringLength(60, MinimumLength = 2)] string DisplayName,
    [Required, EmailAddress] string Email,
    [Required, StringLength(100, MinimumLength = 8)] string Password);

public record LoginRequest([Required] string Email, [Required] string Password);

public record MeDto(string Email, string DisplayName);

public record BoardInput([Required, StringLength(80, MinimumLength = 1)] string Name);

public record ColumnInput([Required, StringLength(40, MinimumLength = 1)] string Name, bool IsDone);

public record CardInput(
    [Required, StringLength(120, MinimumLength = 1)] string Title,
    [StringLength(2000)] string? Description,
    Priority Priority,
    DateOnly? DueDate);

public record MoveInput([Range(1, int.MaxValue)] int ColumnId, [Range(0, 10000)] int Index);

public record BoardSummaryDto(int Id, string Name, int CardCount, int OverdueCount);

public record CardDto(int Id, int ColumnId, string Title, string? Description, Priority Priority, DateOnly? DueDate);

public record ColumnDto(int Id, string Name, bool IsDone, List<CardDto> Cards);

public record BoardDto(int Id, string Name, List<ColumnDto> Columns);

public record ActivityDto(int Id, string Text, DateTime AtUtc);
