using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MantIA.BE.Auditoria;
using MantIA.BE.Common;
using MantIA.BE.Entities;
using MantIA.BE.Seguridad;
using MantIA.DAL.Seguridad;
using MantIA.DAL.Tenancy;

namespace MantIA.DAL.Context;

/// <summary>
/// Contexto de persistencia de MantIA sobre PostgreSQL.
/// <para>
/// Tres reglas se aplican <b>por convencion y no entidad por entidad</b>, deliberadamente:
/// el aislamiento entre empresas, la baja logica y el control de concurrencia. Si dependieran
/// de que alguien se acuerde de configurarlas en cada entidad nueva, la primera que se olvide
/// filtra datos de un cliente a otro. Aca una entidad nueva las hereda por el solo hecho de
/// derivar de <see cref="TenantEntity"/> o de implementar la interfaz correspondiente.
/// </para>
/// <para>
/// La bitacora (<see cref="EventoBitacora"/>) NO vive aca: va a MongoDB. Si en el futuro
/// aparece un <c>DbSet</c> de eventos, es un error.
/// </para>
/// </summary>
public class MantIADbContext : DbContext
{
    /// <summary>Nombre del filtro de baja logica, para poder ignorarlo sin perder el de tenant.</summary>
    public const string FiltroBaja = "baja_logica";

    /// <summary>Nombre del filtro de aislamiento entre empresas.</summary>
    public const string FiltroTenant = "tenant";

    private readonly ICurrentTenant _tenant;
    private readonly IProtectorDatos? _protector;

    /// <param name="protector">
    /// Cifra los campos marcados en <see cref="CamposCifrados"/>. Es opcional a proposito: en tiempo
    /// de diseno —generar migraciones— no hay llaves configuradas y tampoco hacen falta, porque el
    /// cifrado no cambia el tipo de la columna. El esquema que genera EF es identico con o sin el.
    /// </param>
    public MantIADbContext(
        DbContextOptions<MantIADbContext> options,
        ICurrentTenant tenant,
        IProtectorDatos? protector = null)
        : base(options)
    {
        _tenant = tenant;
        _protector = protector;
    }

    /// <summary>
    /// Empresa del contexto actual. Es publica porque los filtros globales la leen: EF Core
    /// reescribe el acceso a un miembro del contexto como parametro de consulta, de modo que
    /// el valor se evalua en cada consulta y no queda congelado en el modelo cacheado.
    /// </summary>
    public Guid? EmpresaActual => _tenant.EmpresaId;

    #region DbSets

    // --- Plataforma (compartido entre todas las empresas) ---
    public DbSet<Empresa> Empresas => Set<Empresa>();
    public DbSet<Plan> Planes => Set<Plan>();
    public DbSet<CatalogoMaquina> CatalogosMaquina => Set<CatalogoMaquina>();
    public DbSet<CatalogoFallaComun> CatalogoFallasComunes => Set<CatalogoFallaComun>();
    public DbSet<CatalogoRepuestoSugerido> CatalogoRepuestosSugeridos => Set<CatalogoRepuestoSugerido>();
    public DbSet<EvidenciaModelo> EvidenciasModelo => Set<EvidenciaModelo>();

    // --- Empresa ---
    public DbSet<Planta> Plantas => Set<Planta>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<UsuarioAlcance> UsuariosAlcance => Set<UsuarioAlcance>();
    public DbSet<NivelPermiso> NivelesPermiso => Set<NivelPermiso>();
    public DbSet<PermisoPorRolYNivel> PermisosPorRolYNivel => Set<PermisoPorRolYNivel>();
    public DbSet<PermisoPorUsuario> PermisosPorUsuario => Set<PermisoPorUsuario>();
    public DbSet<SolicitudRollback> SolicitudesRollback => Set<SolicitudRollback>();
    public DbSet<ContadorDocumento> ContadoresDocumento => Set<ContadorDocumento>();

    /// <summary>
    /// Respaldo de bitacora: eventos que no pudieron escribirse en MongoDB y esperan a reflejarse.
    /// Es la unica tabla de PostgreSQL que existe para sostener a otro motor.
    /// </summary>
    public DbSet<EventoPendiente> EventosPendientes => Set<EventoPendiente>();

    // --- Operacion ---
    public DbSet<Maquina> Maquinas => Set<Maquina>();
    public DbSet<Repuesto> Repuestos => Set<Repuesto>();
    public DbSet<MaquinaRepuesto> MaquinasRepuesto => Set<MaquinaRepuesto>();
    public DbSet<MovimientoStock> MovimientosStock => Set<MovimientoStock>();
    public DbSet<OrdenTrabajo> OrdenesTrabajo => Set<OrdenTrabajo>();
    public DbSet<OrdenTrabajoRepuesto> OrdenesTrabajoRepuesto => Set<OrdenTrabajoRepuesto>();
    public DbSet<HistorialOrdenTrabajo> HistorialOrdenesTrabajo => Set<HistorialOrdenTrabajo>();
    public DbSet<AlertaStock> AlertasStock => Set<AlertaStock>();
    public DbSet<Recomendacion> Recomendaciones => Set<Recomendacion>();
    public DbSet<Reporte> Reportes => Set<Reporte>();
    public DbSet<ReporteHistorial> ReportesHistorial => Set<ReporteHistorial>();

    #endregion

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // La extension se declara en el modelo para que la migracion emita el CREATE EXTENSION.
        // En Azure Database for PostgreSQL el nombre sigue siendo "vector"; hay que habilitarla
        // antes en el servidor (azure.extensions) o el CREATE EXTENSION falla por permisos.
        modelBuilder.HasPostgresExtension("vector");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MantIADbContext).Assembly);

        AplicarConvenciones(modelBuilder);
    }

    /// <summary>
    /// Entidades de tenant que NO llevan clave foranea a <c>empresas</c>, con su motivo.
    /// La lista es corta a proposito: cada excepcion es un lugar donde la base deja de verificar
    /// algo, y tiene que poder justificarse.
    /// </summary>
    private static readonly HashSet<Type> SinClaveForaneaAEmpresa =
    [
        // El respaldo de bitacora guarda tambien eventos de plataforma, que no pertenecen a ninguna
        // empresa. Una clave foranea los rechazaria justo cuando el sistema esta degradado, que es
        // el unico momento en que esta tabla se usa.
        typeof(EventoPendiente),
    ];

    /// <summary>
    /// Recorre el modelo ya construido y aplica lo que no debe depender de la memoria de
    /// quien agregue la proxima entidad.
    /// </summary>
    private void AplicarConvenciones(ModelBuilder modelBuilder)
    {
        // Se materializa la lista: adentro del bucle se agregan relaciones al modelo.
        foreach (var entidad in modelBuilder.Model.GetEntityTypes().ToList())
        {
            var tipo = entidad.ClrType;

            // 1. Enums como texto. Un entero en la base es ilegible, y reordenar el enum en C#
            //    cambiaria el significado de los datos ya guardados sin que nada avise.
            foreach (var propiedad in entidad.GetProperties())
            {
                var tipoPropiedad = Nullable.GetUnderlyingType(propiedad.ClrType) ?? propiedad.ClrType;
                if (!tipoPropiedad.IsEnum) continue;

                var conversor = (ValueConverter)Activator.CreateInstance(
                    typeof(EnumToStringConverter<>).MakeGenericType(tipoPropiedad),
                    [null])!;

                propiedad.SetValueConverter(conversor);
                propiedad.SetMaxLength(40);
            }

            // 2. Cifrado por campo. Solo los que figuran en el catalogo: una tabla enteramente
            //    cifrada deja de ser una base de datos, porque no se puede filtrar, ordenar ni sumar.
            //
            //    Va en dos partes deliberadamente separadas:
            //
            //    - La FORMA de la columna la decide el catalogo, SIEMPRE, haya llave o no. El texto
            //      cifrado ocupa bastante mas que el original —nonce, etiqueta y base64—, asi que la
            //      columna no puede llevar el largo declarado en la entidad. Si esto dependiera de
            //      que haya protector, la migracion se generaria con varchar(120) —en tiempo de
            //      diseno no hay llaves— y la aplicacion fallaria al insertar el primer valor
            //      cifrado. Es exactamente el modo de falla que el esquema no puede tener.
            //
            //    - El COMPORTAMIENTO, cifrar y descifrar, solo cuando hay protector.
            foreach (var propiedad in entidad.GetProperties())
            {
                if (propiedad.ClrType != typeof(string)) continue;

                var nivel = CamposCifrados.De(tipo.Name, propiedad.Name);
                if (nivel == NivelCifrado.Ninguno) continue;

                propiedad.SetMaxLength(null);

                if (_protector is not null)
                    propiedad.SetValueConverter(new ConversorCifrado(_protector, nivel, tipo.Name, propiedad.Name));
            }

            // 3. Concurrencia optimista sobre xmin, la columna de sistema que PostgreSQL ya
            //    mantiene por fila. No agrega ninguna columna propia.
            if (typeof(IConcurrencia).IsAssignableFrom(tipo))
            {
                modelBuilder.Entity(tipo)
                    .Property(nameof(IConcurrencia.Version))
                    .IsRowVersion();
            }

            // 4. Filtros globales. Se aplican con lambdas tipadas invocadas por reflexion en
            //    lugar de armar arboles de expresion a mano: la expresion queda con la forma
            //    exacta que EF Core sabe reescribir para leer el tenant en cada consulta.
            var tenant = typeof(TenantEntity).IsAssignableFrom(tipo);
            var baja = typeof(IBajaLogica).IsAssignableFrom(tipo);

            if (tenant)
            {
                Invocar(nameof(FiltrarPorTenant), tipo, modelBuilder);
                AsegurarClaveForaneaAEmpresa(modelBuilder, entidad, tipo);
            }

            if (baja)
                Invocar(nameof(FiltrarBajaLogica), tipo, modelBuilder);
        }
    }

    /// <summary>
    /// Toda entidad de tenant apunta a su empresa con una clave foranea real, y nunca en cascada.
    ///
    /// <para><b>Por que la clave foranea.</b> Sin ella, <c>EmpresaId</c> es un uuid suelto: una fila
    /// con una empresa inexistente es posible y nada la detecta. Con ella, la base rechaza el dato
    /// mal formado en el momento, que es cuando todavia se puede arreglar. EF solo la habia
    /// descubierto en dos de veinte entidades —las unicas con navegacion declarada desde
    /// <c>Empresa</c>—, con lo cual la integridad dependia de un detalle de como se escribio la
    /// entidad.</para>
    ///
    /// <para><b>Por que <see cref="DeleteBehavior.Restrict"/> y no cascada.</b> La cascada que EF
    /// pone por convencion significa que borrar una fila de <c>empresas</c> borra todos sus usuarios
    /// y sus plantas sin preguntar. Va en contra de todo lo demas: las bajas son logicas, la purga de
    /// un tenant es manual y deliberada, y el historial tiene que sobrevivir. Con Restrict, ese
    /// borrado falla, que es exactamente lo que tiene que pasar.</para>
    /// </summary>
    private static void AsegurarClaveForaneaAEmpresa(
        ModelBuilder modelBuilder, IMutableEntityType entidad, Type tipo)
    {
        var existente = entidad.GetForeignKeys()
            .FirstOrDefault(f => f.PrincipalEntityType.ClrType == typeof(Empresa));

        if (existente is not null)
        {
            existente.DeleteBehavior = DeleteBehavior.Restrict;
            return;
        }

        if (SinClaveForaneaAEmpresa.Contains(tipo)) return;

        modelBuilder.Entity(tipo)
            .HasOne(typeof(Empresa))
            .WithMany()
            .HasForeignKey(nameof(TenantEntity.EmpresaId))
            .OnDelete(DeleteBehavior.Restrict);
    }

    private void Invocar(string metodo, Type tipoEntidad, ModelBuilder modelBuilder) =>
        typeof(MantIADbContext)
            .GetMethod(metodo, BindingFlags.NonPublic | BindingFlags.Instance)!
            .MakeGenericMethod(tipoEntidad)
            .Invoke(this, [modelBuilder]);

    /// <summary>
    /// Aislamiento entre empresas. Una consulta mal escrita en la capa de negocio no puede
    /// devolver filas de otro cliente porque el filtro se aplica en el modelo, no en la consulta.
    /// </summary>
    private void FiltrarPorTenant<TEntidad>(ModelBuilder modelBuilder)
        where TEntidad : TenantEntity =>
        modelBuilder.Entity<TEntidad>()
            .HasQueryFilter(FiltroTenant, e => e.EmpresaId == EmpresaActual);

    /// <summary>
    /// Oculta las filas dadas de baja.
    /// <para>
    /// Va como filtro <b>con nombre</b> por una razon concreta: los reportes y el historial
    /// necesitan ver lo dado de baja, y <c>IgnoreQueryFilters()</c> sin argumentos apaga
    /// <i>todos</i> los filtros, incluido el de empresa. Escribir
    /// <c>IgnoreQueryFilters([MantIADbContext.FiltroBaja])</c> devuelve las bajas sin abrir
    /// nunca la puerta a los datos de otro tenant.
    /// </para>
    /// </summary>
    private void FiltrarBajaLogica<TEntidad>(ModelBuilder modelBuilder)
        where TEntidad : class, IBajaLogica =>
        modelBuilder.Entity<TEntidad>()
            .HasQueryFilter(FiltroBaja, e => e.FechaBaja == null);

    public override int SaveChanges()
    {
        Sellar();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        Sellar();
        return base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Completa empresa y campos de auditoria antes de escribir.
    /// <para>
    /// El alta sin contexto de tenant lanza en lugar de guardar con empresa vacia: ante la duda,
    /// el sistema se cierra. Un registro huerfano no dispara ninguna alarma y se descubre meses
    /// despues, cuando ya nadie sabe de donde salio.
    /// </para>
    /// </summary>
    private void Sellar()
    {
        var ahora = DateTimeOffset.UtcNow;
        var usuario = _tenant.UsuarioId;

        foreach (var entrada in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entrada.State)
            {
                case EntityState.Added:
                    if (entrada.Entity is TenantEntity nueva)
                    {
                        if (_tenant.EmpresaId is null)
                            throw new InvalidOperationException(
                                "Intento de escritura sin contexto de tenant (fail-closed).");
                        nueva.EmpresaId = _tenant.EmpresaId.Value;
                    }
                    entrada.Entity.FechaCreacion = ahora;
                    entrada.Entity.CreadoPorUsuarioId ??= usuario;
                    break;

                case EntityState.Modified:
                    // La fecha de creacion no se reescribe aunque la entidad venga modificada
                    // desde afuera con otro valor.
                    entrada.Property(e => e.FechaCreacion).IsModified = false;
                    entrada.Property(e => e.CreadoPorUsuarioId).IsModified = false;
                    entrada.Entity.FechaModificacion = ahora;
                    entrada.Entity.ModificadoPorUsuarioId = usuario;
                    break;
            }
        }
    }
}
