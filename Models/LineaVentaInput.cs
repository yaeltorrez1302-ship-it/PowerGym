namespace PowerGym.Models;

// El navegador solo envia identificador y cantidad.
// El servidor obtiene nombre y precio desde PostgreSQL, nunca del navegador.
public sealed class LineaVentaInput
{
    public int IdProducto { get; set; }
    public int Cantidad { get; set; }
}
