using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using MantIA.DAL.Bitacora;
using MantIA.DAL.Tenancy;


var builder = WebApplication.CreateBuilder(args);
// Ver el comentario equivalente en MantIA.WEB: UseVector habilita el mapeo del tipo vector.
builder.Services.AddDbContext<MantIA.DAL.Context.MantIADbContext>(options =>
    options
        .UseNpgsql(
            builder.Configuration.GetConnectionString("MantIADb"),
            npgsql => npgsql.UseVector())
        .UseSnakeCaseNamingConvention());

builder.Services.AddScoped<ICurrentTenant, CurrentTenant>();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<MantIA.BLL.Authorization.IPermisoService, MantIA.BLL.Authorization.PermisoService>();
builder.Services.AddScoped<MantIA.BLL.Authorization.IUsuarioActual, MantIA.BLL.Authorization.UsuarioActual>();
builder.Services.AddScoped<MantIA.BLL.Authorization.IGestorPermisos, MantIA.BLL.Authorization.GestorPermisos>();

// Bitacora. Ver el comentario en MantIA.WEB: las llaves no van en appsettings.json.
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

// Documentos adjuntos a las maquinas. El almacen local guarda por hash de contenido; la raiz tiene
// que quedar FUERA de wwwroot, o los archivos serian descargables sin pasar por permisos.
builder.Services.Configure<MantIA.DAL.Documentos.OpcionesDocumentos>(
    builder.Configuration.GetSection(MantIA.DAL.Documentos.OpcionesDocumentos.Seccion));
builder.Services.AddSingleton<MantIA.DAL.Documentos.IAlmacenDocumentos, MantIA.DAL.Documentos.AlmacenDocumentosLocal>();
builder.Services.AddScoped<MantIA.BLL.Documentos.IServicioDocumentos, MantIA.BLL.Documentos.ServicioDocumentos>();

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

try
{
    await app.Services.PrepararBitacoraAsync();
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "No se pudieron preparar los indices de la bitacora.");
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
