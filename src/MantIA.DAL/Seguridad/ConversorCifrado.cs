using MantIA.BE.Seguridad;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MantIA.DAL.Seguridad;

/// <summary>
/// Cifra y descifra un campo al ir y volver de la base, sin que la entidad ni la capa de negocio se
/// enteren. Una <c>OrdenTrabajo</c> siempre tiene su descripción en claro en memoria; lo que cambia
/// es lo que hay en la columna.
///
/// <para><b>Tolera datos preexistentes en claro.</b> Al leer, si el valor no tiene el prefijo de
/// cifrado se devuelve tal cual. Sin eso, activar el cifrado sobre una tabla con datos rompería
/// todas las filas viejas de golpe; así conviven mientras se migran, y cada fila que se guarda
/// queda cifrada.</para>
/// </summary>
public class ConversorCifrado : ValueConverter<string?, string?>
{
    public ConversorCifrado(IProtectorDatos protector, NivelCifrado nivel, string entidad, string campo)
        : this(protector, nivel, Contexto(entidad, campo))
    {
    }

    private ConversorCifrado(IProtectorDatos protector, NivelCifrado nivel, string contexto)
        : base(
            claro => Proteger(protector, nivel, contexto, claro),
            guardado => Revelar(protector, contexto, guardado))
    {
    }

    /// <summary>
    /// Ata el valor cifrado a la columna donde vive. Copiar el texto cifrado del correo de un
    /// usuario a la columna de otra tabla deja de funcionar: la etiqueta ya no verifica.
    /// </summary>
    public static string Contexto(string entidad, string campo) => $"{entidad}.{campo}";

    private static string? Proteger(IProtectorDatos protector, NivelCifrado nivel, string contexto, string? claro)
    {
        if (string.IsNullOrEmpty(claro)) return claro;

        return nivel == NivelCifrado.Determinista
            ? protector.CifrarDeterminista(claro, contexto)
            : protector.Cifrar(claro, contexto);
    }

    private static string? Revelar(IProtectorDatos protector, string contexto, string? guardado)
    {
        if (string.IsNullOrEmpty(guardado) || !protector.EstaCifrado(guardado))
            return guardado;

        // La version con la que se cifro no viaja en la columna: el protector prueba con la vigente
        // y, si no abre, con las anteriores. Es lo que permite rotar la llave sin reescribir la
        // tabla entera el mismo dia.
        return protector.Descifrar(guardado, contexto);
    }
}
