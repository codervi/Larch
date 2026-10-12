using Larch.Api.Dtos;
using Larch.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Larch.Api.Controllers;

[ApiController, Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly SignInManager<AppUser> _signIn;
    private readonly UserManager<AppUser> _users;

    public AuthController(SignInManager<AppUser> signIn, UserManager<AppUser> users)
    {
        _signIn = signIn;
        _users = users;
    }

    [HttpPost("register"), AllowAnonymous, EnableRateLimiting("auth")]
    public async Task<ActionResult<MeDto>> Register(RegisterRequest request)
    {
        var user = new AppUser
        {
            UserName = request.Email.Trim(),
            Email = request.Email.Trim(),
            DisplayName = request.DisplayName.Trim(),
        };

        var result = await _users.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return Problem(detail: result.Errors.First().Description, statusCode: StatusCodes.Status400BadRequest);

        await _signIn.SignInAsync(user, isPersistent: false);
        return new MeDto(user.Email!, user.DisplayName);
    }

    [HttpPost("login"), AllowAnonymous, EnableRateLimiting("auth")]
    public async Task<ActionResult<MeDto>> Login(LoginRequest request)
    {
        var result = await _signIn.PasswordSignInAsync(request.Email.Trim(), request.Password,
            isPersistent: false, lockoutOnFailure: true);

        if (!result.Succeeded)
            return Problem(
                detail: result.IsLockedOut ? "Too many attempts. Try again in a few minutes." : "Wrong email or password.",
                statusCode: StatusCodes.Status401Unauthorized);

        var user = (await _users.FindByEmailAsync(request.Email.Trim()))!;
        return new MeDto(user.Email!, user.DisplayName);
    }

    [HttpPost("logout"), Authorize]
    public async Task<IActionResult> Logout()
    {
        await _signIn.SignOutAsync();
        return NoContent();
    }

    [HttpGet("me"), Authorize]
    public async Task<ActionResult<MeDto>> Me()
    {
        var user = await _users.GetUserAsync(User);
        return user == null ? Unauthorized() : new MeDto(user.Email!, user.DisplayName);
    }
}
