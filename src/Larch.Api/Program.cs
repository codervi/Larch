using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Larch.Api.Data;
using Larch.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<AppUser, IdentityRole>(o =>
{
    o.Password.RequiredLength = 8;
    o.User.RequireUniqueEmail = true;
    o.Lockout.MaxFailedAccessAttempts = 5;
    o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
})
.AddEntityFrameworkStores<AppDbContext>();

builder.Services.ConfigureApplicationCookie(o =>
{
    o.Cookie.Name = "larch.auth";
    o.Cookie.HttpOnly = true;
    o.Cookie.SameSite = SameSiteMode.Strict;
    o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    o.ExpireTimeSpan = TimeSpan.FromHours(8);
    o.SlidingExpiration = true;
    // API: answer with a status code instead of redirecting to a login page
    o.Events.OnRedirectToLogin = ctx => { ctx.Response.StatusCode = 401; return Task.CompletedTask; };
    o.Events.OnRedirectToAccessDenied = ctx => { ctx.Response.StatusCode = 403; return Task.CompletedTask; };
});

builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy("auth", http => RateLimitPartition.GetFixedWindowLimiter(
        http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));
});

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseExceptionHandler();
if (!app.Environment.IsDevelopment()) app.UseHsts();
app.UseHttpsRedirection();

app.Use(async (ctx, next) =>
{
    var h = ctx.Response.Headers;
    h["X-Content-Type-Options"] = "nosniff";
    h["X-Frame-Options"] = "DENY";
    h["Referrer-Policy"] = "no-referrer";
    if (!app.Environment.IsDevelopment())
        h["Content-Security-Policy"] = "default-src 'self'; img-src 'self' data:; base-uri 'none'; frame-ancestors 'none'";
    if (ctx.Request.Path.StartsWithSegments("/api")) h["Cache-Control"] = "no-store";
    await next();
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();

// CSRF guard on top of the SameSite=Strict cookie: anything that changes data must send
// the X-Requested-With header. A form on another site cannot add it.
app.Use(async (ctx, next) =>
{
    var m = ctx.Request.Method;
    var changesData = !(HttpMethods.IsGet(m) || HttpMethods.IsHead(m) || HttpMethods.IsOptions(m));
    if (changesData && ctx.Request.Path.StartsWithSegments("/api") && !ctx.Request.Headers.ContainsKey("X-Requested-With"))
    {
        ctx.Response.StatusCode = 400;
        await ctx.Response.WriteAsJsonAsync(new ProblemDetails { Status = 400, Title = "Request rejected", Detail = "Missing X-Requested-With header." });
        return;
    }
    await next();
});

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

await SeedData.RunAsync(app.Services, app.Environment.IsDevelopment());

app.Run();
