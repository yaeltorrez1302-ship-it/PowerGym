namespace PowerGym.Services;

/// <summary>
/// Separa los detalles tecnicos, que solo se registran en los logs,
/// de los avisos comprensibles que ve el usuario final.
/// </summary>
public static class MensajesErrorUx
{
    public const string Conexion =
        "No podemos conectarnos con la base de datos. Comprueba que PostgreSQL esté disponible e inténtalo nuevamente.";

    public const string Inesperado =
        "Ocurrió un inconveniente al procesar la solicitud. Inténtalo nuevamente o contacta al administrador.";

    public const string AccesoTemporal =
        "No podemos verificar tus credenciales en este momento. Comprueba la conexión e inténtalo nuevamente.";

    public static string Restriccion(string? sqlState) => sqlState switch
    {
        "23505" => "Ya existe un registro con esos datos. Comprueba la información antes de continuar.",
        "23503" => "El registro está vinculado a otra operación. Revisa los datos relacionados antes de continuar.",
        "23514" => "Alguno de los datos no cumple las condiciones permitidas. Revisa los importes y vuelve a intentar.",
        "23502" => "Falta un dato obligatorio. Revisa los campos y vuelve a intentar.",
        _ => "No se pudo completar la operación por una restricción de datos. Comprueba la información e inténtalo otra vez."
    };
}
