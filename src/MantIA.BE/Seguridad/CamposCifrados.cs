namespace MantIA.BE.Seguridad;

/// <summary>Cómo se guarda un campo en la base.</summary>
public enum NivelCifrado
{
    /// <summary>En claro. Es el default y la mayoría de los campos quedan acá.</summary>
    Ninguno,

    /// <summary>
    /// Cifrado, pero el mismo valor produce siempre el mismo texto cifrado.
    /// <para>
    /// Se puede indexar y buscar por igualdad —<c>WHERE email = @x</c> sigue funcionando— al precio
    /// de que quien vea la tabla puede deducir que dos filas tienen el mismo valor, aunque no sepa
    /// cuál es. Para un correo o un identificador externo es un intercambio razonable: son campos
    /// por los que hay que poder buscar.
    /// </para>
    /// </summary>
    Determinista,

    /// <summary>
    /// Cifrado con nonce nuevo en cada escritura. Dos filas con el mismo valor se ven distintas.
    /// <para>
    /// <b>No se puede indexar, buscar, ordenar ni comparar en SQL.</b> Solo sirve para campos que se
    /// leen enteros y se muestran: una descripción, un motivo, una observación.
    /// </para>
    /// </summary>
    Aleatorio
}

/// <summary>
/// Qué campos se guardan cifrados y con qué nivel.
///
/// <para><b>El criterio no es "cifrar todo".</b> Una tabla enteramente cifrada deja de ser una base
/// de datos: no se puede filtrar, ordenar, agrupar ni sumar. Se cifra lo que, leído por alguien que
/// tenga acceso al motor pero no a la aplicación, causaría daño concreto — y se deja en claro lo que
/// el sistema necesita para funcionar.</para>
///
/// <para>Por eso hay dos niveles y no uno. El determinista existe justamente para los campos por los
/// que hay que buscar: sin él, cifrar el correo del usuario rompería el login.</para>
///
/// <para><b>Esto es cifrado a nivel de campo, que es distinto del cifrado en reposo.</b> El motor de
/// base de datos cifra el disco entero y protege del robo del archivo; esto protege de quien tenga
/// una sesión legítima contra la base —un administrador de infraestructura, un volcado de respaldo,
/// una consulta de soporte— y no debería ver ciertos valores. Los dos conviven y resuelven amenazas
/// distintas.</para>
/// </summary>
public static class CamposCifrados
{
    private static readonly IReadOnlyDictionary<string, NivelCifrado> Politica =
        new Dictionary<string, NivelCifrado>(StringComparer.OrdinalIgnoreCase)
        {
            // --- Identidad ---
            // Determinista y no aleatorio porque el login busca por estos dos campos. Con cifrado
            // aleatorio habria que traer todos los usuarios y descifrarlos uno por uno para
            // encontrar uno: inviable, y ademas se caen los indices unicos.
            ["Usuario.Auth0UserId"] = NivelCifrado.Determinista,
            ["Usuario.Email"]       = NivelCifrado.Determinista,

            // El correo invitado se busca por igualdad en cada primer ingreso, asi que tiene que
            // ser determinista por el mismo motivo que el del usuario. Con cifrado aleatorio habria
            // que traer todas las invitaciones y descifrarlas una por una para resolver un login.
            ["InvitacionUsuario.Email"] = NivelCifrado.Determinista,

            // Texto libre escrito por quien revoca.
            ["InvitacionUsuario.MotivoRevocacion"] = NivelCifrado.Aleatorio,

            // --- Texto libre de operacion ---
            // Es donde la gente escribe de verdad, y donde termina apareciendo lo que no deberia:
            // nombres de terceros, precios negociados, comentarios sobre companeros. Se lee entero
            // y se muestra; no se filtra por su contenido en SQL.
            ["OrdenTrabajo.DescripcionProblema"]   = NivelCifrado.Aleatorio,
            ["OrdenTrabajo.DescripcionResolucion"] = NivelCifrado.Aleatorio,
            ["OrdenTrabajo.MotivoRechazo"]         = NivelCifrado.Aleatorio,
            ["MovimientoStock.Motivo"]             = NivelCifrado.Aleatorio,
            ["SolicitudRollback.Motivo"]           = NivelCifrado.Aleatorio,
            ["SolicitudRollback.MotivoRechazo"]    = NivelCifrado.Aleatorio,
            ["PermisoPorUsuario.Motivo"]           = NivelCifrado.Aleatorio,
            ["Recomendacion.MotivoRechazo"]        = NivelCifrado.Aleatorio,

            // --- Historial de cambios ---
            // Estos tres son OBLIGATORIOS y por un motivo que no es obvio: el historial guarda el
            // valor anterior y el nuevo de cada campo modificado. Si "DescripcionProblema" se cifra
            // en su tabla pero su valor viejo queda en claro aca, el cifrado no sirve de nada — se
            // lee lo mismo, una tabla mas alla. Una tabla de auditoria hereda la sensibilidad de
            // todo lo que audita, y ese es el criterio general: siempre que un campo copie valores
            // de otro, tiene que estar al menos tan protegido como el original.
            ["HistorialOrdenTrabajo.ValorAnterior"] = NivelCifrado.Aleatorio,
            ["HistorialOrdenTrabajo.ValorNuevo"]    = NivelCifrado.Aleatorio,
            ["HistorialOrdenTrabajo.Motivo"]        = NivelCifrado.Aleatorio,

            // --- Comercial ---
            // El proveedor y su precio son la relacion comercial del cliente. No se filtra ni se
            // agrupa por proveedor en ninguna pantalla, asi que puede ir aleatorio.
            ["Repuesto.Proveedor"] = NivelCifrado.Aleatorio,

            // --- Documentos adjuntos ---
            // Mismo criterio que arriba, aplicado al papel: el emisor de un certificado ES el
            // proveedor de mantenimiento del cliente, y su numero identifica una relacion comercial
            // concreta. La descripcion es texto libre y por lo tanto termina teniendo de todo.
            // El titulo queda en claro porque es por lo que se busca en la lista de la maquina.
            ["DocumentoMaquina.Emisor"]          = NivelCifrado.Aleatorio,
            ["DocumentoMaquina.NumeroDocumento"] = NivelCifrado.Aleatorio,
            ["DocumentoMaquina.Descripcion"]     = NivelCifrado.Aleatorio,
        };

    public static NivelCifrado De(string entidad, string campo) =>
        Politica.TryGetValue($"{entidad}.{campo}", out var nivel) ? nivel : NivelCifrado.Ninguno;

    public static IEnumerable<(string Entidad, string Campo, NivelCifrado Nivel)> Todos() =>
        Politica.Select(p =>
        {
            var partes = p.Key.Split('.');
            return (partes[0], partes[1], p.Value);
        });

    // ---------------------------------------------------------------------------------------
    // NO se cifran, y cada uno tiene su motivo:
    //
    //   Repuesto.CostoUnitario           Se suma. "Cuanto vale el stock inmovilizado" es una de las
    //                                    cifras que justifica el producto, y sobre un campo cifrado
    //                                    no hay SUM ni comparacion posible.
    //
    //   Criticidad, Severidad, Estado    Son los ejes por los que se filtra en todas las pantallas.
    //                                    Cifrarlos obliga a traer la tabla entera a memoria para
    //                                    mostrar "las alertas criticas de este mes".
    //
    //   Usuario.Nombre / Apellido        Se ordenan alfabeticamente en cada selector de usuario.
    //                                    Cifrados, ese orden hay que hacerlo en memoria.
    //
    // Todos son reversibles: si alguno resulta mas sensible que util, se agrega al diccionario y
    // se migran los datos existentes. Lo que no conviene es cifrarlos "por las dudas" y descubrir
    // en la demo que un listado tarda ocho segundos.
    // ---------------------------------------------------------------------------------------
}
