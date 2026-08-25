using Auth0.AspNetCore.Authentication;
using MantIA.DAL.Bitacora;
using MantIA.WEB.Components;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;

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
            // Se decide el acceso ANTES de crear la sesion. Si no esta habilitado, el pipeline se
            // detiene aca y nunca llega a existir una cookie: dejarlo entrar y bloquearlo despues
            // en cada pagina seria confiar en que ninguna pantalla se olvide de preguntar.
            //
            // La decision NO vive aca. Vive en IServicioAcceso, que es tambien el que usa el
            // resolvedor de tenant. Antes habia dos copias de estas reglas y basta con que una se
            // desactualice para que deje entrar a quien no debe.
            OnTicketReceived = async context =>
            {
                var acceso = context.HttpContext.RequestServices
                    .GetRequiredService<MantIA.BLL.Acceso.IServicioAcceso>();

                var resultado = await acceso.ResolverAsync(
                    context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                    context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value,
                    context.HttpContext.RequestAborted);

                if (resultado.Autorizado) return;

                // El mensaje que ve la persona sale del servicio y no de aca: es el mismo que quedo
                // escrito en la bitacora, asi que soporte y usuario hablan del mismo hecho.
                context.Response.Redirect("/acceso-denegado?mensaje=" +
                    Uri.EscapeDataString(resultado.Mensaje ?? "No tenes acceso al sistema."));
                context.HandleResponse();
            }
        };
    }); 
// UseVector habilita el mapeo del tipo vector de pgvector. Tiene que estar tambien aca y no
// solo en la fabrica de diseno: sin el, la aplicacion genera la tabla bien pero falla al leer
// la columna de embeddings en tiempo de ejecucion.
builder.Services.AddDbContext<MantIA.DAL.Context.MantIADbContext>(options =>
    options
        .UseNpgsql(
            builder.Configuration.GetConnectionString("MantIADb"),
            npgsql => npgsql.UseVector())
        .UseSnakeCaseNamingConvention());

builder.Services.AddScoped<MantIA.DAL.Tenancy.ICurrentTenant, MantIA.DAL.Tenancy.CurrentTenant>();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddMudServices();
builder.Services.AddMemoryCache();

// Estado de la maqueta funcional. Alcance scoped = una copia por circuito de Blazor
// Server, para que dos usuarios conectados al mismo tiempo no compartan datos, vista
// ni idioma. Cuando cada modulo pase a base de datos, estos tres registros se
// reemplazan por los servicios reales sin tocar las pantallas.
builder.Services.AddScoped<MantIA.WEB.Demo.DatosDemo>();
builder.Services.AddScoped<MantIA.WEB.Demo.Sesion>();
builder.Services.AddScoped<MantIA.WEB.Demo.Idioma>();
builder.Services.AddScoped<MantIA.BLL.Authorization.IPermisoService, MantIA.BLL.Authorization.PermisoService>();
builder.Services.AddScoped<MantIA.BLL.Authorization.IUsuarioActual, MantIA.BLL.Authorization.UsuarioActual>();
builder.Services.AddScoped<MantIA.BLL.Authorization.IGestorPermisos, MantIA.BLL.Authorization.GestorPermisos>();

// Acceso. Es el unico lugar donde se decide quien entra, y lo usan tanto el evento de login de
// Auth0 como el resolvedor de tenant.
builder.Services.AddScoped<MantIA.BLL.Acceso.IServicioAcceso, MantIA.BLL.Acceso.ServicioAcceso>();
builder.Services.AddScoped<MantIA.BLL.Acceso.IServicioInvitaciones, MantIA.BLL.Acceso.ServicioInvitaciones>();
builder.Services.AddScoped<MantIA.BLL.Plataforma.IServicioAltaEmpresa, MantIA.BLL.Plataforma.ServicioAltaEmpresa>();

// Bitacora. Las llaves de sellado y cifrado NO van en appsettings.json: en desarrollo se cargan
// con "dotnet user-secrets set Auditoria:Llaves:v1 <base64>" y en produccion por variable de
// entorno. Se generan con: openssl rand -base64 32
builder.Services.Configure<MantIA.DAL.Seguridad.OpcionesAuditoria>(
    builder.Configuration.GetSection(MantIA.DAL.Seguridad.OpcionesAuditoria.Seccion));
builder.Services.AddSingleton<MantIA.DAL.Seguridad.IProtectorDatos, MantIA.DAL.Seguridad.ProtectorDatos>();
builder.Services.AddScoped<MantIA.BLL.Auditoria.IBitacora, MantIA.BLL.Auditoria.Bitacora>();
builder.Services.AddScoped<MantIA.BLL.Auditoria.IVerificadorCadena, MantIA.BLL.Auditoria.VerificadorCadena>();
builder.Services.AgregarBitacora(builder.Configuration);
builder.Services.AddScoped<MantIA.DAL.Numeracion.INumeradorDocumentos, MantIA.DAL.Numeracion.NumeradorDocumentos>();

// Trabajo de fondo: refleja en MongoDB lo que quedo en el respaldo local y cierra los eslabones
// que quedaron sin sellar. Corre cada 30 segundos y no hace nada si no hay nada pendiente.
builder.Services.AddHostedService<MantIA.BLL.Auditoria.MantenimientoBitacora>();

// Digitos verificadores de fila y de tabla. Usan un TERCER juego de llaves, distinto del de sellado
// y del de cifrado: "Auditoria:Verificacion:Llaves:v1". La aplicacion no arranca si dos juegos
// comparten una llave. Generar con: openssl rand -base64 32
builder.Services.AddScoped<MantIA.BLL.Auditoria.IVerificadorDigitos, MantIA.BLL.Auditoria.VerificadorDigitos>();
builder.Services.Configure<MantIA.DAL.Seguridad.OpcionesVerificacion>(
    builder.Configuration.GetSection(MantIA.DAL.Seguridad.OpcionesVerificacion.Seccion));
builder.Services.AddHostedService<MantIA.BLL.Auditoria.VerificacionIntegridad>();

// Siembra minima, SOLO en desarrollo. Doble cerrojo: el registro esta dentro de este if y ademas
// el propio servicio vuelve a comprobar el entorno. Sembrar usuarios administradores en produccion
// es la clase de accidente que no se descubre hasta que alguien entra con ellos.
if (builder.Environment.IsDevelopment())
    builder.Services.AddHostedService<MantIA.BLL.Siembra.SiembraInicial>();

// Documentos adjuntos a las maquinas. El almacen local guarda por hash de contenido; la raiz tiene
// que quedar FUERA de wwwroot, o los archivos serian descargables sin pasar por permisos.
builder.Services.Configure<MantIA.DAL.Documentos.OpcionesDocumentos>(
    builder.Configuration.GetSection(MantIA.DAL.Documentos.OpcionesDocumentos.Seccion));
builder.Services.AddSingleton<MantIA.DAL.Documentos.IAlmacenDocumentos, MantIA.DAL.Documentos.AlmacenDocumentosLocal>();
builder.Services.AddScoped<MantIA.BLL.Documentos.IServicioDocumentos, MantIA.BLL.Documentos.ServicioDocumentos>();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<MantIA.WEB.Services.TenantResolver>();

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

var app = builder.Build();

// Indices de la bitacora. Si Mongo no esta arriba se avisa y se sigue: dejar la aplicacion sin
// arrancar por un indice que se puede crear despues convierte una falla parcial en una total.
try
{
    await app.Services.PrepararBitacoraAsync();
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "No se pudieron preparar los indices de la bitacora. " +
        "La aplicacion arranca igual, pero revisar la conexion a MongoDB.");
}

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
