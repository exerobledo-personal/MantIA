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

        // Lookup por sub ANTES de conocer el tenant: excepcion controlada al filtro.
        var usuario = await _db.Usuarios
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Auth0UserId == sub);

        if (usuario is null)
            return new ResultadoAcceso
            {
                Estado = EstadoAcceso.UsuarioNoAprovisionado,
                Mensaje = "El usuario no esta dado de alta en ninguna empresa."
            };

        // Empresa no lleva filtro de tenant (es BaseEntity), se consulta directo.
        var empresa = await _db.Empresas
            .FirstOrDefaultAsync(e => e.Id == usuario.EmpresaId);

        if (empresa is null)
            return new ResultadoAcceso
            {
                Estado = EstadoAcceso.UsuarioNoAprovisionado,
                Mensaje = "La empresa del usuario no existe."
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

        // Acceso OK: recien aca se setea el tenant real.
        _tenant.EmpresaId = empresa.Id;
        return new ResultadoAcceso { Estado = EstadoAcceso.Autorizado, EmpresaId = empresa.Id };
    }

    private static string? ExtraerDominio(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;
        var idx = email.LastIndexOf('@');
        return idx >= 0 && idx < email.Length - 1 ? email[(idx + 1)..] : null;
    }
}