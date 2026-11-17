using Npgsql;
using NpgsqlTypes;
using PowerGym.Models;

namespace PowerGym.Data;

public sealed class VentaDAO
{
    private readonly DbConnectionFactory _factory;

    public VentaDAO(DbConnectionFactory factory) => _factory = factory;

    public async Task<List<ProductoVenta>> ListarProductosAsync()
    {
        var resultado = new List<ProductoVenta>();
        await using var cn = _factory.CreateConnection();
        await cn.OpenAsync();

        await using var cmd = new NpgsqlCommand(
            "SELECT id_producto, nombre, precio FROM productos WHERE activo = TRUE ORDER BY nombre", cn);
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
        {
            resultado.Add(new ProductoVenta
            {
                IdProducto = r.GetInt32(0),
                Nombre = r.GetString(1),
                Precio = r.GetDecimal(2)
            });
        }
        return resultado;
    }

    public async Task<Venta> RegistrarVentaAsync(string? cliente, IReadOnlyList<LineaVentaInput> lineas)
    {
        cliente = string.IsNullOrWhiteSpace(cliente) ? "Consumidor Final" : cliente.Trim();
        if (cliente.Length > 150)
            throw new ArgumentException("El nombre del cliente no debe superar los 150 caracteres.");
        if (lineas.Count == 0)
            throw new ArgumentException("Agrega al menos un artículo antes de confirmar la venta.");
        if (lineas.Count > 100 || lineas.Any(x => x.IdProducto <= 0 || x.Cantidad <= 0 || x.Cantidad > 1000))
            throw new ArgumentException("Revisa los artículos: utiliza cantidades entre 1 y 1000.");

        // Agrupar protege contra productos repetidos y totales manipulados desde el navegador.
        var grupos = lineas.GroupBy(x => x.IdProducto)
            .Select(g => new { IdProducto = g.Key, Cantidad = g.Sum(x => (long)x.Cantidad) })
            .ToList();
        if (grupos.Any(x => x.Cantidad > 1000))
            throw new ArgumentException("La cantidad acumulada por artículo no puede superar 1000.");

        await using var cn = _factory.CreateConnection();
        await cn.OpenAsync();
        await using var tx = await cn.BeginTransactionAsync(); // NpgsqlTransaction

        try
        {
            var venta = new Venta { Cliente = cliente, Fecha = DateTime.Now };

            foreach (var grupo in grupos)
            {
                // Se toma el precio vigente de PostgreSQL, no el proporcionado por la vista.
                await using var buscar = new NpgsqlCommand(
                    "SELECT nombre, precio FROM productos WHERE id_producto = @id AND activo = TRUE FOR SHARE",
                    cn, tx);
                buscar.Parameters.AddWithValue("id", NpgsqlDbType.Integer, grupo.IdProducto);
                await using var r = await buscar.ExecuteReaderAsync();
                if (!await r.ReadAsync())
                    throw new ArgumentException("Uno de los artículos ya no está disponible. Revisa el carrito.");

                var nombre = r.GetString(0);
                var precio = r.GetDecimal(1);
                await r.CloseAsync();

                venta.Detalles.Add(new DetalleVenta
                {
                    IdProducto = grupo.IdProducto,
                    NombreProducto = nombre,
                    Cantidad = (int)grupo.Cantidad,
                    PrecioUnitario = precio
                });
            }

            if (venta.Total > 99999999.99m)
                throw new ArgumentException("El total supera el límite permitido para una venta.");

            // Maestro: INSERT y obtención del ID dentro de la misma transacción.
            await using (var cabecera = new NpgsqlCommand(
                "INSERT INTO ventas (fecha, cliente, total) VALUES (@fecha, @cliente, @total) RETURNING id",
                cn, tx))
            {
                cabecera.Parameters.AddWithValue("fecha", NpgsqlDbType.Timestamp, venta.Fecha);
                cabecera.Parameters.AddWithValue("cliente", NpgsqlDbType.Varchar, venta.Cliente);
                cabecera.Parameters.AddWithValue("total", NpgsqlDbType.Numeric, venta.Total);
                venta.Id = Convert.ToInt32(await cabecera.ExecuteScalarAsync());
            }

            // Detalle: cada línea guarda id_venta apuntando al maestro.
            const string sql = """
                INSERT INTO detalle_ventas
                    (id_venta, id_producto, nombre_producto, cantidad, precio_unitario, subtotal)
                VALUES (@id_venta, @id_producto, @nombre, @cantidad, @precio, @subtotal)
                """;

            foreach (var linea in venta.Detalles)
            {
                await using var detalle = new NpgsqlCommand(sql, cn, tx);
                detalle.Parameters.AddWithValue("id_venta", NpgsqlDbType.Integer, venta.Id);
                detalle.Parameters.AddWithValue("id_producto", NpgsqlDbType.Integer, linea.IdProducto);
                detalle.Parameters.AddWithValue("nombre", NpgsqlDbType.Varchar, linea.NombreProducto);
                detalle.Parameters.AddWithValue("cantidad", NpgsqlDbType.Integer, linea.Cantidad);
                detalle.Parameters.AddWithValue("precio", NpgsqlDbType.Numeric, linea.PrecioUnitario);
                detalle.Parameters.AddWithValue("subtotal", NpgsqlDbType.Numeric, linea.Subtotal);
                await detalle.ExecuteNonQueryAsync();
            }

            await tx.CommitAsync(); // si todo salió bien: COMMIT
            return venta;
        }
        catch
        {
            await tx.RollbackAsync(); // si algo falla: ROLLBACK del maestro y del detalle
            throw;
        }
    }

    public async Task<Venta?> ObtenerVentaAsync(int id)
    {
        if (id <= 0) return null;
        await using var cn = _factory.CreateConnection();
        await cn.OpenAsync();
        Venta? venta;
        await using (var cabecera = new NpgsqlCommand(
            "SELECT id, fecha, cliente FROM ventas WHERE id = @id", cn))
        {
            cabecera.Parameters.AddWithValue("id", NpgsqlDbType.Integer, id);
            await using var r = await cabecera.ExecuteReaderAsync();
            if (!await r.ReadAsync()) return null;
            venta = new Venta
            {
                Id = r.GetInt32(0), Fecha = r.GetDateTime(1), Cliente = r.GetString(2)
            };
        }

        await using (var detalle = new NpgsqlCommand(
            """
            SELECT id_producto, nombre_producto, cantidad, precio_unitario
            FROM detalle_ventas WHERE id_venta = @id ORDER BY id
            """, cn))
        {
            detalle.Parameters.AddWithValue("id", NpgsqlDbType.Integer, id);
            await using var r = await detalle.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                venta.Detalles.Add(new DetalleVenta
                {
                    IdProducto = r.GetInt32(0), NombreProducto = r.GetString(1),
                    Cantidad = r.GetInt32(2), PrecioUnitario = r.GetDecimal(3)
                });
            }
        }
        return venta;
    }
}
