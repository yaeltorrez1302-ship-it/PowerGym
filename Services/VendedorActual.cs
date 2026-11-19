using System.Security.Claims;

namespace PowerGym.Services;

public static class VendedorActual
{
    // Solo obtiene el ID de la identidad validada por ASP.NET Core (cookie firmada).
    // No utiliza nunca campos editables del cliente.
    public static int ObtenerId(ClaimsPrincipal usuario)
    {
        if (usuario.Identity?.IsAuthenticated != true)
            throw new UnauthorizedAccessException("Debes iniciar sesión como vendedor.");
        var identificadores = usuario.FindAll(ClaimTypes.NameIdentifier).ToList();
        if (identificadores.Count != 1 || !int.TryParse(identificadores[0].Value, out int id) || id <= 0)
            throw new UnauthorizedAccessException("La identidad del vendedor no es válida.");
        return id;
    }
}
