namespace PowerGym.Models;

public sealed class ProductoVenta
{
    public int IdProducto { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public decimal Precio { get; set; }
}
