namespace PowerGym.Models;

public sealed class Venta
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; } = DateTime.Now;
    public string Cliente { get; set; } = "Consumidor Final";
    public List<DetalleVenta> Detalles { get; set; } = new();
    public string TipoDescuento { get; set; } = "NINGUNO";
    public decimal ValorDescuento { get; set; }
    public decimal DescuentoAplicado { get; set; }
    public int? VendedorId { get; set; } // Las ventas anteriores al cambio pueden ser históricas.
    public string VendedorNombre { get; set; } = "Registro anterior";
    public decimal Subtotal => Detalles.Sum(x => x.Subtotal);
    public decimal Total => Subtotal - DescuentoAplicado;
}
