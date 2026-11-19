using System.Security.Claims;
using PowerGym.Services;

var casos = new (string Nombre, Action Ejecutar)[]
{
    ("15 % de Bs. 1000 = descuento Bs. 150 y total Bs. 850", () =>
    {
        var r = DescuentoService.Calcular(1000m, "PORCENTAJE", 15m);
        Igual(150m, r.DescuentoAplicado); Igual(850m, r.Total);
    }),
    ("20 % exacto aceptado: Bs. 500 - Bs. 100 = Bs. 400", () =>
    {
        var r = DescuentoService.Calcular(500m, "PORCENTAJE", 20m);
        Igual(100m, r.DescuentoAplicado); Igual(400m, r.Total);
    }),
    ("20.01 % rechazado", () => Rechaza(() => DescuentoService.Calcular(1000m, "PORCENTAJE", 20.01m))),
    ("Importe monetario que supera el 20 % rechazado", () => Rechaza(() => DescuentoService.Calcular(1000m, "MONTO", 200.01m))),
    ("Descuento negativo rechazado", () => Rechaza(() => DescuentoService.Calcular(500m, "MONTO", -50m))),
    ("Descuento sin tipo pero con valor rechazado", () => Rechaza(() => DescuentoService.Calcular(500m, "NINGUNO", 50m))),
    ("Descuento de 0 % aceptado", () =>
    {
        var r = DescuentoService.Calcular(214m, "NINGUNO", 0m);
        Igual(0m, r.DescuentoAplicado); Igual(214m, r.Total);
    }),
    ("Monto monetario máximo aceptado: Bs. 42.80 de Bs. 214", () =>
    {
        var r = DescuentoService.Calcular(214m, "MONTO", 42.80m);
        Igual(42.80m, r.DescuentoAplicado); Igual(171.20m, r.Total);
    }),
    ("0.01 por encima del máximo rechazado", () => Rechaza(() => DescuentoService.Calcular(214m, "MONTO", 42.81m))),
    ("Subtotal cero rechazado", () => Rechaza(() => DescuentoService.Calcular(0m, "MONTO", 0m))),
    ("Valores con exceso de precisión rechazados", () => Rechaza(() => DescuentoService.Calcular(1000m, "PORCENTAJE", 5.555m))),
    ("Identidad del vendedor obtenida de ClaimTypes.NameIdentifier", () =>
    {
        var p = Principal(new Claim(ClaimTypes.NameIdentifier, "42"), new Claim(ClaimTypes.Role, "VENDEDOR"));
        Igual(42, VendedorActual.ObtenerId(p));
    }),
    ("Identidad ausente rechazada", () => RechazaIdentidad(() => VendedorActual.ObtenerId(new ClaimsPrincipal()))),
    ("Identidad manipulada o repetida rechazada", () => RechazaIdentidad(() =>
        VendedorActual.ObtenerId(Principal(new Claim(ClaimTypes.NameIdentifier,"42"), new Claim(ClaimTypes.NameIdentifier,"99"))))),
};

var aprobadas = 0;
foreach (var caso in casos)
{
    try { caso.Ejecutar(); Console.WriteLine("PASS: " + caso.Nombre); aprobadas++; }
    catch (Exception ex) { Console.WriteLine("FAIL: " + caso.Nombre + " -> " + ex.Message); }
}
Console.WriteLine($"RESULTADO: {aprobadas}/{casos.Length} pruebas aprobadas");
return aprobadas == casos.Length ? 0 : 1;

static ClaimsPrincipal Principal(params Claim[] claims) =>
    new(new ClaimsIdentity(claims, "Cookies"));
static void Igual<T>(T esperado, T real) where T : notnull
{
    if (!EqualityComparer<T>.Default.Equals(esperado, real))
        throw new Exception($"Esperado {esperado}; recibido {real}");
}
static void Rechaza(Action accion)
{
    try { accion(); } catch (ArgumentException) { return; }
    throw new Exception("Debió rechazar el descuento y no lo hizo.");
}
static void RechazaIdentidad(Action accion)
{
    try { accion(); } catch (UnauthorizedAccessException) { return; }
    throw new Exception("Debió rechazar la identidad y no lo hizo.");
}
