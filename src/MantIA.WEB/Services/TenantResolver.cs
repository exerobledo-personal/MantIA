using System.Security.Claims;
using MantIA.DAL.Context;
using MantIA.DAL.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace MantIA.WEB.Services;

public enum EstadoAcceso
{
    Autorizado,
    NoAutenticado,
    UsuarioNoAprovisionado,
    DominioNoCorporativo
}

public class ResultadoAcceso
{
    public EstadoAcceso Estado { get; init; }
    public Guid? EmpresaId { get; init; }
    public string? Mensaje { get; init; }
}

public class TenantResolver
{
    private readonly MantIADbContext _db;
    private readonly CurrentTenant _tenant;

    public TenantResolver(MantIADbContext db, ICurrentTenant tenant)
    {
        _db = db;
        _tenant = (CurrentTenant)tenant;
    }

    public async Task<ResultadoAcceso> ResolverAsync(ClaimsPrincipal user)
    {
        var sub = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = user.FindFirst(ClaimTypes.Email)?.Value;

        if (string.IsNullOrEmpty(sub))
            return new ResultadoAcceso { Estado = EstadoAcceso.NoAutenticado };

        // Busqueda por sub ANTES de conocer el tenant: es la unica excepcion legitima al filtro
        // de empresa, porque justamente estamos averiguando cual es. Se ignora SOLO ese filtro,
        // por nombre: con IgnoreQueryFilters() sin argumentos tambien se apagaria el de baja
        // logica y un usuario dado de baja podria volver a entrar.
        var usuario = await _db.Usuarios
            .IgnoreQueryFilters([MantIADbContext.FiltroTenant])
            .FirstOrDefaultAsync(u => u.Auth0UserId == sub);

        if (usuario is null)
            return new ResultadoAcceso
            {
                Estado = EstadoAcceso.UsuarioNoAprovisionado,
                Mensaje = "El usuario no esta dado de alta en ninguna empresa."
            };

        // Empresa no lleva filtro de tenant (es BaseEntity). Si lleva el de baja logica, y aca
        // se ignora a proposito para poder distinguir "no existe" de "esta dada de baja": son
        // dos problemas distintos y el usuario merece saber cual de los dos le toco.
        var empresa = await _db.Empresas
            .IgnoreQueryFilters([MantIADbContext.FiltroBaja])
            .FirstOrDefaultAsync(e => e.Id == usuario.EmpresaId);

        if (empresa is null)
            return new ResultadoAcceso
            {
                Estado = EstadoAcceso.UsuarioNoAprovisionado,
                Mensaje = "La empresa del usuario no existe."
            };

        if (empresa.FechaBaja is not null)
            return new ResultadoAcceso
            {
                Estado = EstadoAcceso.UsuarioNoAprovisionado,
                Mensaje = $"La cuenta de {empresa.RazonSocial} esta dada de baja. Contactate con MantIA."
            };

        // Validacion de dominio corporativo
        var dominioEmail = ExtraerDominio(email);
        if (dominioEmail is null ||
            !string.Equals(dominioEmail, empresa.Dominio, StringComparison.OrdinalIgnoreCase))
        {
            return new ResultadoAcceso
            {
                Estado = EstadoAcceso.DominioNoCorporativo,
                Mensaje = $"El correo {email} no pertenece al dominio corporativo de {empresa.RazonSocial} ({empresa.Dominio})."
            };
        }

        // Acceso OK: recien aca se setea el contexto real. El usuario se guarda ademas de la
        // empresa porque el contexto de datos lo usa para sellar quien creo o modifico cada fila.
        _tenant.EmpresaId = empresa.Id;
        _tenant.UsuarioId = usuario.Id;
        return new ResultadoAcceso { Estado = EstadoAcceso.Autorizado, EmpresaId = empresa.Id };
    }

    private static string? ExtraerDominio(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;
        var idx = email.LastIndexOf('@');
        return idx >= 0 && idx < email.Length - 1 ? email[(idx + 1)..] : null;
    }
}