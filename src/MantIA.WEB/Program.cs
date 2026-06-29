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
        options.OpenIdConnectEvents = new Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectEvents
        {
            OnTicketReceived = async context =>
            {
                var sub = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var email = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

                var db = context.HttpContext.RequestServices
                    .GetRequiredService<MantIA.DAL.Context.MantIADbContext>();

                var usuario = await db.Usuarios
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(u => u.Auth0UserId == sub);

                bool autorizado = false;
                if (usuario is not null && email is not null)
                {
                    var empresa = await db.Empresas.FirstOrDefaultAsync(e => e.Id == usuario.EmpresaId);
                    if (empresa is not null)
                    {
                        var dominioEmail = email.Contains('@') ? email[(email.LastIndexOf('@') + 1)..] : null;
                        autorizado = string.Equals(dominioEmail, empresa.Dominio, StringComparison.OrdinalIgnoreCase);
                    }
                }

                if (!autorizado)
                {
                    // Cancela la creacion de la sesion y redirige a acceso denegado.
                    context.Response.Redirect("/acceso-denegado?mensaje=" +
                        Uri.EscapeDataString("Tu correo no pertenece al dominio corporativo de tu empresa."));
                    context.HandleResponse(); // detiene el pipeline de login
                }
            }
        };
    }); 
builder.Services.AddDbContext<MantIA.DAL.Context.MantIADbContext>(options =>
    options
        .UseNpgsql(builder.Configuration.GetConnectionString("MantIADb"))
        .UseSnakeCaseNamingConvention());

builder.Services.AddScoped<MantIA.DAL.Tenancy.ICurrentTenant, MantIA.DAL.Tenancy.CurrentTenant>();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<MantIA.BLL.Authorization.IPermisoService, MantIA.BLL.Authorization.PermisoService>();
builder.Services.AddScoped<MantIA.BLL.Authorization.IUsuarioActual, MantIA.BLL.Authorization.UsuarioActual>();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<MantIA.WEB.Services.TenantResolver>();

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

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
