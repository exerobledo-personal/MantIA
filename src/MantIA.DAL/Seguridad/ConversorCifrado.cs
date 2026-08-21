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
    public ConversorCifrado(IProtectorDatos protector, NivelCifrado nivel)
        : base(
            claro => Proteger(protector, nivel, claro),
            guardado => Revelar(protector, guardado))
    {
    }

    private static string? Proteger(IProtectorDatos protector, NivelCifrado nivel, string? claro)
    {
        if (string.IsNullOrEmpty(claro)) return claro;

        return nivel == NivelCifrado.Determinista
            ? protector.CifrarDeterminista(claro)
            : protector.Cifrar(claro);
    }

    private static string? Revelar(IProtectorDatos protector, string? guardado)
    {
        if (string.IsNullOrEmpty(guardado) || !protector.EstaCifrado(guardado))
            return guardado;

        // La version con la que se cifro no viaja en la columna: el protector prueba con la vigente
        // y, si no abre, con las anteriores. Es lo que permite rotar la llave sin reescribir la
        // tabla entera el mismo dia.
        return protector.Descifrar(guardado);
    }
}
