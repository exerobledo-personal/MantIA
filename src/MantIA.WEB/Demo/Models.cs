namespace MantIA.WEB.Demo;

public enum Criticidad { Baja, Media, Alta, Critica }

public enum EstadoMaquina { Operativa, EnMantenimiento, Detenida, Inactiva }

public enum EstadoEnriquecimiento { Pendiente, EnProceso, Completado, Fallido }

public enum EstadoOrden { Abierta, EnCurso, Cerrada, Cancelada }

public enum TipoMantenimiento { Correctivo, Preventivo, Predictivo }

public enum Prioridad { Baja, Media, Alta, Urgente }

public enum EstadoAlerta { Activa, Resuelta }

public enum EstadoRecomendacion { Activa, Aceptada, Rechazada }

public enum OrigenRecomendacion { Regla, Modelo }

public enum EstadoGenerico { Activo, Inactivo }

public enum NivelLog { Debug, Info, Warning, Error }

public enum EstadoServicio { Operativo, Degradado, Caido }

/// <summary>
/// Generador de identificadores para la maqueta.
///
/// Durante la siembra de <see cref="DatosDemo"/> entrega una secuencia deterministica,
/// para que todas las sesiones compartan los mismos Id y los enlaces directos
/// (/maquinas/{id}) y el F5 sigan funcionando aunque cada circuito tenga su propia
/// copia de los datos. Fuera de la siembra vuelve a Guid.NewGuid(), asi todo lo que
/// el usuario da de alta en pantalla recibe un Id unico de verdad.
/// </summary>
public static class IdDemo
{
    private sealed class Contador { public int Ultimo; }

    private static readonly System.Threading.AsyncLocal<Contador?> EnSiembra = new();

    public static Guid Nuevo() =>
        EnSiembra.Value is { } contador ? Determinista(++contador.Ultimo) : Guid.NewGuid();

    /// <summary>Abre un tramo de siembra deterministica. Se cierra al liberar el objeto.</summary>
    public static IDisposable Sembrando()
    {
        var previo = EnSiembra.Value;
        EnSiembra.Value = new Contador();
        return new Tramo(previo);
    }

    private static Guid Determinista(int numero)
    {
        Span<byte> bytes = stackalloc byte[16];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(bytes, numero);
        // Marca fija para que un Id sembrado se reconozca de un vistazo en la URL.
        bytes[8] = 0xDE;
        bytes[9] = 0x30;
        return new Guid(bytes);
    }

    private sealed class Tramo(Contador? previo) : IDisposable
    {
        public void Dispose() => EnSiembra.Value = previo;
    }
}

public static class Roles
{
    public const string Empleado = "Empleado";
    public const string Supervisor = "Supervisor";
    public const string Gerente = "Gerente";
    public const string AdminEmpresa = "AdminEmpresa";
    public const string SuperAdmin = "SuperAdminMantIA";

    public static string Etiqueta(string rol) => rol switch
    {
        Empleado => "Empleado de mantenimiento",
        Supervisor => "Supervisor de mantenimiento",
        Gerente => "Gerente de ingeniería",
        AdminEmpresa => "Administrador de empresa",
        SuperAdmin => "Superadministrador MantIA",
        _ => rol
    };
}

public class PlanVm
{
    public Guid Id { get; set; } = IdDemo.Nuevo();
    public string Nombre { get; set; } = "";
    public int MaxMaquinas { get; set; }
    public decimal Precio { get; set; }
    public string Descripcion { get; set; } = "";
    public int EmpresasActivas { get; set; }
}

public class EmpresaVm
{
    public Guid Id { get; set; } = IdDemo.Nuevo();
    public string RazonSocial { get; set; } = "";
    public string Dominio { get; set; } = "";
    public string TenantId { get; set; } = "";
    public string Plan { get; set; } = "";
    public int MaxMaquinasHabilitadas { get; set; }
    public int MaquinasRegistradas { get; set; }
    public int UsuariosActivos { get; set; }
    public string Rubro { get; set; } = "";
    public EstadoGenerico Estado { get; set; } = EstadoGenerico.Activo;
    public DateTime FechaAlta { get; set; }
    public string AdminInicial { get; set; } = "";
    public int OrdenesUltimoMes { get; set; }
    public int RecomendacionesProcesadas { get; set; }
}

public class PlantaVm
{
    public Guid Id { get; set; } = IdDemo.Nuevo();
    public string Nombre { get; set; } = "";
    public string Direccion { get; set; } = "";
    public string Localidad { get; set; } = "";
    public decimal Latitud { get; set; }
    public decimal Longitud { get; set; }
    public EstadoGenerico Estado { get; set; } = EstadoGenerico.Activo;
    public DateTime FechaAlta { get; set; }
    public int Maquinas { get; set; }
    public int AlertasActivas { get; set; }
    public int OrdenesAbiertas { get; set; }
}

public class CatalogoMaquinaVm
{
    public Guid Id { get; set; } = IdDemo.Nuevo();
    public string Marca { get; set; } = "";
    public string Modelo { get; set; } = "";
    public string Categoria { get; set; } = "";
    public List<string> FallasComunes { get; set; } = [];
    public List<string> RepuestosSugeridos { get; set; } = [];
    public string IntervalosMantenimiento { get; set; } = "";
    public EstadoEnriquecimiento Estado { get; set; } = EstadoEnriquecimiento.Pendiente;
    public DateTime? FechaUltimoEnriquecimiento { get; set; }
    public int TenantsQueLoUsan { get; set; }

    public string Descripcion => $"{Marca} {Modelo}";
}

public class MaquinaVm
{
    public Guid Id { get; set; } = IdDemo.Nuevo();
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string NumeroSerie { get; set; } = "";
    public Guid PlantaId { get; set; }
    public string Planta { get; set; } = "";
    public Guid CatalogoMaquinaId { get; set; }
    public string Marca { get; set; } = "";
    public string Modelo { get; set; } = "";
    public string Linea { get; set; } = "";
    public EstadoMaquina Estado { get; set; } = EstadoMaquina.Operativa;
    public Criticidad CriticidadOperativa { get; set; } = Criticidad.Media;
    public DateTime FechaAlta { get; set; }
    public DateTime? UltimaIntervencion { get; set; }
    public int HorasOperacion { get; set; }
    public EstadoEnriquecimiento Enriquecimiento { get; set; } = EstadoEnriquecimiento.Completado;

    public string Descripcion => $"{Marca} {Modelo}";
}

public class RepuestoVm
{
    public Guid Id { get; set; } = IdDemo.Nuevo();
    public string Nombre { get; set; } = "";
    public string NumeroParte { get; set; } = "";
    public string ProveedorReferencia { get; set; } = "";
    public string UnidadMedida { get; set; } = "Unidad";
    public decimal StockActual { get; set; }
    public decimal StockMinimo { get; set; }
    public Criticidad Criticidad { get; set; } = Criticidad.Media;
    public EstadoGenerico Estado { get; set; } = EstadoGenerico.Activo;
    public int PlazoReposicionDias { get; set; }
    public decimal CostoUnitario { get; set; }
    public List<Guid> MaquinaIds { get; set; } = [];
    public List<string> Maquinas { get; set; } = [];
    public DateTime FechaAlta { get; set; }

    public bool BajoMinimo => StockActual <= StockMinimo;
    public decimal Cobertura => StockMinimo <= 0 ? 100 : Math.Round(StockActual / StockMinimo * 100, 0);
}

public class OrdenTrabajoVm
{
    public Guid Id { get; set; } = IdDemo.Nuevo();
    public string Numero { get; set; } = "";
    public Guid MaquinaId { get; set; }
    public string Maquina { get; set; } = "";
    public string Planta { get; set; } = "";
    public string UsuarioAsignado { get; set; } = "";
    public TipoMantenimiento TipoMantenimiento { get; set; }
    public string TipoFalla { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public Prioridad Prioridad { get; set; } = Prioridad.Media;
    public EstadoOrden Estado { get; set; } = EstadoOrden.Abierta;
    public DateTime FechaApertura { get; set; }
    public DateTime? FechaCierre { get; set; }
    public string ResolucionAplicada { get; set; } = "";
    public List<ConsumoRepuestoVm> Repuestos { get; set; } = [];

    public double? HorasResolucion => FechaCierre is null
        ? null
        : Math.Round((FechaCierre.Value - FechaApertura).TotalHours, 1);
}

public class ConsumoRepuestoVm
{
    public Guid RepuestoId { get; set; }
    public string Repuesto { get; set; } = "";
    public string NumeroParte { get; set; } = "";
    public decimal Cantidad { get; set; }
    public decimal StockAnterior { get; set; }
}

public class AlertaStockVm
{
    public Guid Id { get; set; } = IdDemo.Nuevo();
    public Guid RepuestoId { get; set; }
    public string Repuesto { get; set; } = "";
    public string NumeroParte { get; set; } = "";
    public string Maquina { get; set; } = "";
    public string Planta { get; set; } = "";
    public Criticidad Criticidad { get; set; }
    public decimal StockActual { get; set; }
    public decimal StockMinimo { get; set; }
    public int DiasCobertura { get; set; }
    public EstadoAlerta Estado { get; set; } = EstadoAlerta.Activa;
    public DateTime FechaGeneracion { get; set; }
    public DateTime? FechaResolucion { get; set; }
}

public class RecomendacionVm
{
    public Guid Id { get; set; } = IdDemo.Nuevo();
    public Guid RepuestoId { get; set; }
    public string Repuesto { get; set; } = "";
    public string NumeroParte { get; set; } = "";
    public string Maquina { get; set; } = "";
    public string Planta { get; set; } = "";
    public OrigenRecomendacion Origen { get; set; }
    public string ReglaAplicada { get; set; } = "";
    public decimal CantidadSugerida { get; set; }
    public decimal StockActual { get; set; }
    public string Justificacion { get; set; } = "";
    public List<string> Evidencia { get; set; } = [];
    public int Confianza { get; set; }
    public Prioridad Prioridad { get; set; }
    public EstadoRecomendacion Estado { get; set; } = EstadoRecomendacion.Activa;
    public string ComentarioRechazo { get; set; } = "";
    public string UsuarioValidacion { get; set; } = "";
    public DateTime FechaGeneracion { get; set; }
    public DateTime? FechaValidacion { get; set; }
    public decimal ImpactoEstimado { get; set; }
}

public class NivelPermisoVm
{
    public Guid Id { get; set; } = IdDemo.Nuevo();
    public string Nombre { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public int Usuarios { get; set; }
}

public class UsuarioVm
{
    public Guid Id { get; set; } = IdDemo.Nuevo();
    public string Nombre { get; set; } = "";
    public string Apellido { get; set; } = "";
    public string Email { get; set; } = "";
    public string Rol { get; set; } = Roles.Empleado;
    public string Nivel { get; set; } = "Jr";
    public EstadoGenerico Estado { get; set; } = EstadoGenerico.Activo;
    public List<string> Plantas { get; set; } = [];
    public DateTime FechaAlta { get; set; }
    public DateTime? UltimoAcceso { get; set; }
    public int OrdenesAsignadas { get; set; }

    public string NombreCompleto => $"{Nombre} {Apellido}";
    public string Iniciales => $"{(Nombre.Length > 0 ? Nombre[0] : ' ')}{(Apellido.Length > 0 ? Apellido[0] : ' ')}".Trim();
}

public class PermisoVm
{
    public string Rol { get; set; } = "";
    public string Nivel { get; set; } = "";
    public string Recurso { get; set; } = "";
    public string Accion { get; set; } = "";
    public bool Habilitado { get; set; }
}

public class ReporteVm
{
    public Guid Id { get; set; } = IdDemo.Nuevo();
    public string Nombre { get; set; } = "";
    public string TipoReporte { get; set; } = "";
    public string Parametros { get; set; } = "";
    public string Periodo { get; set; } = "";
    public string Planta { get; set; } = "";
    public string UsuarioCreador { get; set; } = "";
    public EstadoGenerico Estado { get; set; } = EstadoGenerico.Activo;
    public DateTime FechaGeneracion { get; set; }
    public int Filas { get; set; }
    public List<ReporteHistorialVm> Historial { get; set; } = [];
}

public class ReporteHistorialVm
{
    public string Usuario { get; set; } = "";
    public string Accion { get; set; } = "";
    public DateTime Fecha { get; set; }
    public string Detalle { get; set; } = "";
}

public class EventoBitacoraVm
{
    public Guid Id { get; set; } = IdDemo.Nuevo();
    public DateTime Fecha { get; set; }
    public string Usuario { get; set; } = "";
    public string Empresa { get; set; } = "";
    public string Accion { get; set; } = "";
    public string Recurso { get; set; } = "";
    public string Detalle { get; set; } = "";
    public NivelLog Nivel { get; set; } = NivelLog.Info;
    public string Origen { get; set; } = "";
}

public class ServicioVm
{
    public string Nombre { get; set; } = "";
    public string Tecnologia { get; set; } = "";
    public EstadoServicio Estado { get; set; } = EstadoServicio.Operativo;
    public double LatenciaMs { get; set; }
    public double Disponibilidad { get; set; }
    public string UltimoIncidente { get; set; } = "";
}

public class HistorialFallaVm
{
    public DateTime Fecha { get; set; }
    public string Orden { get; set; } = "";
    public string TipoFalla { get; set; } = "";
    public TipoMantenimiento Tipo { get; set; }
    public string RepuestoUtilizado { get; set; } = "";
    public double HorasResolucion { get; set; }
    public string Tecnico { get; set; } = "";
}
