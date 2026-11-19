namespace PowerGym.Services;

public sealed record ResultadoDescuento(
    decimal Subtotal,
    string Tipo,
    decimal ValorSolicitado,
    decimal DescuentoAplicado,
    decimal Total);

// Regla exclusivamente del servidor. Trabajamos con decimal para evitar errores de flotantes.
public static class DescuentoService
{
    public const decimal LimitePorcentaje = 20m;

    public static ResultadoDescuento Calcular(decimal subtotal, string? tipo, decimal valor)
    {
        if (subtotal <= 0m || subtotal > 99999999.99m || subtotal != decimal.Round(subtotal, 2))
            throw new ArgumentException("El subtotal de la venta debe ser válido y mayor a cero.");
        if (valor < 0m)
            throw new ArgumentException("El descuento no puede ser negativo.");
        if (valor != decimal.Round(valor, 2))
            throw new ArgumentException("El descuento admite como máximo dos decimales.");

        tipo = (tipo ?? "NINGUNO").Trim().ToUpperInvariant();
        if (tipo is not ("NINGUNO" or "PORCENTAJE" or "MONTO"))
            throw new ArgumentException("Selecciona un tipo de descuento válido.");
        if (tipo == "NINGUNO" && valor != 0m)
            throw new ArgumentException("El descuento sin tipo debe ser igual a cero.");
        if (tipo == "PORCENTAJE" && valor > LimitePorcentaje)
            throw new ArgumentException("El descuento excede el límite máximo corporativo del 20%.");

        var maximo = decimal.Round(subtotal * 0.20m, 2, MidpointRounding.AwayFromZero);
        var importe = tipo switch
        {
            "PORCENTAJE" => decimal.Round(subtotal * valor / 100m, 2, MidpointRounding.AwayFromZero),
            "MONTO" => valor,
            _ => 0m
        };
        if (importe > maximo)
            throw new ArgumentException("El descuento excede el límite máximo corporativo del 20%.");

        return new ResultadoDescuento(subtotal, tipo, valor, importe, subtotal - importe);
    }
}
