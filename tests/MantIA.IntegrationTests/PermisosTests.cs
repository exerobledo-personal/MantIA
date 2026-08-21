using MantIA.BE.Common;
using MantIA.BE.Seguridad;
using Xunit;

namespace MantIA.IntegrationTests;

/// <summary>
/// Pruebas del modelo de seguridad que **no necesitan base de datos**.
///
/// <para>La versión anterior de este archivo levantaba PostgreSQL, buscaba una "Empresa Demo"
/// sembrada a mano y comparaba contra nombres de recurso que ya no existen. Dos problemas: fallaba
/// por motivos ajenos a lo que decía probar —la base no está, los datos cambiaron— y no verificaba
/// gran cosa que el compilador no verificara ya.</para>
///
/// <para>Lo que sigue prueba las reglas estructurales, que son las que importan y las que nadie
/// puede romper sin darse cuenta: los ámbitos, el piso irrevocable y la separación de funciones.
/// Corren en milisegundos y no dependen de nada externo. Las pruebas contra base real van cuando
/// exista la siembra.</para>
/// </summary>
public class PermisosTests
{
    [Fact]
    public void El_piso_de_permisos_es_coherente_con_el_catalogo()
    {
        // Si un minimo apuntara a una combinacion invalida, el evaluador concederia un permiso que
        // la frontera estructural deniega. Seria un error de codigo, no de configuracion.
        Assert.Empty(PermisosMinimos.Incoherentes());
    }

    [Theory]
    [InlineData("Ordenes", Acciones.Cerrar)]
    [InlineData("Ordenes", Acciones.Modificacion)]
    [InlineData("Stock", Acciones.Alta)]
    public void El_administrador_de_empresa_no_puede_operar(string recurso, string accion)
    {
        // Separacion de funciones: quien administra la empresa no cierra ordenes ni mueve stock.
        // No es configuracion, es frontera: ninguna edicion de la matriz puede concederlo.
        Assert.False(CatalogoPermisos.EsCombinacionValida(RolSistema.AdminEmpresa, recurso, accion));
    }

    [Fact]
    public void El_administrador_de_empresa_si_puede_mirar_la_operacion()
    {
        Assert.True(CatalogoPermisos.EsCombinacionValida(
            RolSistema.AdminEmpresa, "Ordenes", Acciones.Consultar));

        // Pero solo mirar: ni siquiera exportar, que es sacar datos y no supervisar.
        Assert.False(CatalogoPermisos.EsCombinacionValida(
            RolSistema.AdminEmpresa, "Reportes", Acciones.Exportar));
    }

    [Fact]
    public void La_administracion_de_permisos_esta_repartida_por_ambito()
    {
        // El gerente reparte permisos de operacion; el administrador, los de empresa. Ninguno
        // alcanza el terreno del otro, y por eso "nadie otorga lo que no tiene" puede ser estricta.
        Assert.True(CatalogoPermisos.EsCombinacionValida(
            RolSistema.Gerente, "PermisosOperacion", Acciones.Configurar));
        Assert.False(CatalogoPermisos.EsCombinacionValida(
            RolSistema.Gerente, "Permisos", Acciones.Configurar));

        Assert.True(CatalogoPermisos.EsCombinacionValida(
            RolSistema.AdminEmpresa, "Permisos", Acciones.Configurar));
        Assert.False(CatalogoPermisos.EsCombinacionValida(
            RolSistema.AdminEmpresa, "PermisosOperacion", Acciones.Configurar));
    }

    [Fact]
    public void Nadie_puede_dejarse_sin_salida()
    {
        // Bloqueo puro: si el administrador se quita el acceso a la matriz, nadie dentro de la
        // empresa puede devolverselo.
        Assert.False(PermisosMinimos.EsRevocable(
            RolSistema.AdminEmpresa, "Permisos", Acciones.Configurar));

        Assert.False(PermisosMinimos.EsRevocable(
            RolSistema.Gerente, "PermisosOperacion", Acciones.Configurar));
    }

    [Fact]
    public void Un_operario_conserva_lo_minimo_para_trabajar()
    {
        Assert.False(PermisosMinimos.EsRevocable(RolSistema.Empleado, "Ordenes", Acciones.Consultar));
        Assert.False(PermisosMinimos.EsRevocable(RolSistema.Empleado, "Maquinas", Acciones.Consultar));

        // Y nada mas: el piso es estrecho a proposito. Que un empleado pueda abrir una orden es
        // decision de cada empresa, no nuestra.
        Assert.True(PermisosMinimos.EsRevocable(RolSistema.Empleado, "Ordenes", Acciones.Alta));
    }

    [Fact]
    public void Cerrar_una_orden_es_una_accion_aparte_de_modificarla()
    {
        // Cerrar mueve stock y congela costos. Por eso no es un caso de Modificacion: se concede y
        // se audita por separado.
        var ordenes = CatalogoPermisos.BuscarRecurso("Ordenes");
        Assert.NotNull(ordenes);
        Assert.Contains(Acciones.Cerrar, ordenes!.AccionesValidas);
        Assert.Contains(Acciones.Modificacion, ordenes.AccionesValidas);
    }
}
