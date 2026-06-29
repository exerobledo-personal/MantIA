using Auth0.AspNetCore.Authentication;
using MantIA.WEB.Components;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddAuth0WebAppAuthentication(options =>
    {
        options.Domain = builder.Configuration["Auth0:Domain"]!;
        options.ClientId = builder.Configuration["Auth0:ClientId"]!;
        options.ClientSecret = builder.Configuration["Auth0:ClientSecret"]!;
        options.Scope = "openid profile email";
    });
builder.Services.AddDbContext<MantIA.DAL.Context.MantIADbContext>(options =>
    options
        .UseNpgsql(builder.Configuration.GetConnectionString("MantIADb"))
        .UseSnakeCaseNamingConvention());

builder.Services.AddScoped<MantIA.DAL.Tenancy.ICurrentTenant, MantIA.DAL.Tenancy.CurrentTenant>();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<MantIA.WEB.Services.TenantResolver>();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();


app.MapGet("/login", async (HttpContext ctx, string? returnUrl) =>
{
    var props = new Microsoft.AspNetCore.Authentication.AuthenticationProperties
    {
        RedirectUri = returnUrl ?? "/"
    };
    await ctx.ChallengeAsync("Auth0", props);
});

app.MapGet("/logout", async (HttpContext ctx) =>
{
    await ctx.SignOutAsync("Auth0", new Microsoft.AspNetCore.Authentication.AuthenticationProperties { RedirectUri = "/" });
    await ctx.SignOutAsync("Cookies");
});

app.Run();
