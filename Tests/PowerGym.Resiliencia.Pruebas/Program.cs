using System.Security.Claims;
using PowerGym.Services;

int exitos = 0, fallos = 0;
void Probar(string nombre, Action prueba)
{
    try { prueba(); Console.WriteLine($"PASS: {nombre}"); exitos++; }
    catch (Exception ex) { Console.WriteLine($"FAIL: {nombre} ({ex.GetType().Name}: {ex.Message})"); fallos++; }
}
void Igual<T>(T real, T esperado)
{
    if (!EqualityComparer<T>.Default.Equals(real, esperado))
        throw new Exception($"Se esperaba {esperado}, pero se obtuvo {real}");
}
void Rechaza<T>(Action accion) where T : Exception
{
    try { accion(); }
    catch (T) { return; }
    throw new Exception($"La operacion debia lanzar {typeof(T).Name}");
}
ClaimsPrincipal UsuarioConClaims(params Claim[] claims) =>
    new(new ClaimsIdentity(claims, "cookies"));

Probar("Descuento valido de 15 %", () =>
    Igual(DescuentoService.Calcular(1000m, "PORCENTAJE", 15m).Total, 850m));
Probar("Descuento del 20 % exacto", () =>
    Igual(DescuentoService.Calcular(500m, "PORCENTAJE", 20m).Total, 400m));
Probar("Descuento del 21 % rechazado", () =>
    Rechaza<ArgumentException>(() => DescuentoService.Calcular(100m, "PORCENTAJE", 21m)));
Probar("Descuento monetario superior al limite rechazado", () =>
    Rechaza<ArgumentException>(() => DescuentoService.Calcular(100m, "MONTO", 20.01m)));
Probar("Descuento negativo rechazado", () =>
    Rechaza<ArgumentException>(() => DescuentoService.Calcular(100m, "MONTO", -1m)));
Probar("Venta con subtotal cero rechazada", () =>
    Rechaza<ArgumentException>(() => DescuentoService.Calcular(0m, "NINGUNO", 0m)));
Probar("Identidad autenticada correcta", () =>
    Igual(VendedorActual.ObtenerId(UsuarioConClaims(new Claim(ClaimTypes.NameIdentifier, "1"))), 1));
Probar("Identidad ausente rechazada", () =>
    Rechaza<UnauthorizedAccessException>(() => VendedorActual.ObtenerId(UsuarioConClaims())));
Probar("Identidad duplicada rechazada", () =>
    Rechaza<UnauthorizedAccessException>(() => VendedorActual.ObtenerId(UsuarioConClaims(
        new Claim(ClaimTypes.NameIdentifier, "1"), new Claim(ClaimTypes.NameIdentifier, "2")))));
Probar("Mensaje UX para duplicado (23505)", () =>
{
    var mensaje = MensajesErrorUx.Restriccion("23505");
    if (!mensaje.Contains("Ya existe") || mensaje.Contains("23505")) throw new Exception("Mensaje no orientador");
});
Probar("Mensaje UX para referencia vinculada (23503)", () =>
{
    var mensaje = MensajesErrorUx.Restriccion("23503");
    if (!mensaje.Contains("vinculado") || mensaje.Contains("23503")) throw new Exception("Mensaje no orientador");
});
Probar("Mensaje UX para validacion (23514)", () =>
{
    var mensaje = MensajesErrorUx.Restriccion("23514");
    if (!mensaje.Contains("Revisa") || mensaje.Contains("23514")) throw new Exception("Mensaje no orientador");
});
Probar("Mensaje de conexion sin jerga tecnica", () =>
{
    var mensaje = MensajesErrorUx.Conexion;
    if (!mensaje.Contains("inténtalo", StringComparison.OrdinalIgnoreCase) || mensaje.Contains("Npgsql") || mensaje.Contains("StackTrace"))
        throw new Exception("Mensaje no orientador");
});
Probar("Mensaje inesperado sin mostrar stack trace", () =>
{
    var mensaje = MensajesErrorUx.Inesperado;
    if (!mensaje.Contains("Inténtalo") || mensaje.Contains("Exception"))
        throw new Exception("Mensaje no orientador");
});

Console.WriteLine($"RESULTADO: {exitos}/{exitos + fallos} pruebas aprobadas");
return fallos == 0 ? 0 : 1;
