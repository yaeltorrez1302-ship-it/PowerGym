using Npgsql;
using NpgsqlTypes;
using PowerGym.Models;
using PowerGym.Services;

namespace PowerGym.Data;

public sealed record AuditoriaDescuentoFila(
    int IdVenta, DateTime Fecha, string Cliente, string Vendedor, int VendedorId,
    decimal Subtotal, decimal Descuento, decimal Total, string Tipo);

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
            resultado.Add(new ProductoVenta { IdProducto = r.GetInt32(0), Nombre = r.GetString(1), Precio = r.GetDecimal(2) });
        return resultado;
    }

    public async Task<Venta> RegistrarVentaAsync(
        string? cliente, IReadOnlyList<LineaVentaInput> lineas,
        string? tipoDescuento, decimal valorDescuento, int vendedorAutenticadoId)
    {
        cliente = string.IsNullOrWhiteSpace(cliente) ? "Consumidor Final" : cliente.Trim();
        if (cliente.Length > 150)
            throw new ArgumentException("El nombre del cliente no debe superar los 150 caracteres.");
        if (vendedorAutenticadoId <= 0)
            throw new UnauthorizedAccessException("No hay un vendedor autenticado.");
        if (lineas.Count == 0)
            throw new ArgumentException("Agrega al menos un artículo antes de confirmar la venta.");
        if (lineas.Count > 100 || lineas.Any(x => x.IdProducto <= 0 || x.Cantidad <= 0 || x.Cantidad > 1000))
            throw new ArgumentException("Revisa los artículos: utiliza cantidades entre 1 y 1000.");

        var grupos = lineas.GroupBy(x => x.IdProducto)
            .Select(g => new { IdProducto = g.Key, Cantidad = g.Sum(x => (long)x.Cantidad) }).ToList();
        if (grupos.Any(x => x.Cantidad > 1000))
            throw new ArgumentException("La cantidad acumulada por artículo no puede superar 1000.");

        await using var cn = _factory.CreateConnection();
        await cn.OpenAsync();
        await using var tx = await cn.BeginTransactionAsync();
        try
        {
            // Valida otra vez contra la BD por si el vendedor fue desactivado
            // después de iniciar sesión. FOR SHARE protege contra cambios concurrentes.
            await using (var vendedor = new NpgsqlCommand(
                "SELECT id FROM vendedores WHERE id = @id AND activo = TRUE AND rol = 'VENDEDOR' FOR SHARE", cn, tx))
            {
                vendedor.Parameters.AddWithValue("id", NpgsqlDbType.Integer, vendedorAutenticadoId);
                if (await vendedor.ExecuteScalarAsync() is null)
                    throw new UnauthorizedAccessException("El vendedor ya no tiene autorización para registrar ventas.");
            }

            var venta = new Venta { Cliente = cliente, Fecha = DateTime.Now, VendedorId = vendedorAutenticadoId };
            foreach (var grupo in grupos)
            {
                await using var buscar = new NpgsqlCommand(
                    "SELECT nombre, precio FROM productos WHERE id_producto = @id AND activo = TRUE FOR SHARE", cn, tx);
                buscar.Parameters.AddWithValue("id", NpgsqlDbType.Integer, grupo.IdProducto);
                await using var r = await buscar.ExecuteReaderAsync();
                if (!await r.ReadAsync())
                    throw new ArgumentException("Uno de los artículos ya no está disponible. Revisa el carrito.");
                var nombre = r.GetString(0);
                var precio = r.GetDecimal(1);
                await r.CloseAsync();
                venta.Detalles.Add(new DetalleVenta
                {
                    IdProducto = grupo.IdProducto, NombreProducto = nombre,
                    Cantidad = (int)grupo.Cantidad, PrecioUnitario = precio
                });
            }

            if (venta.Subtotal > 99999999.99m)
                throw new ArgumentException("El subtotal supera el límite permitido para una venta.");
            var descuento = DescuentoService.Calcular(venta.Subtotal, tipoDescuento, valorDescuento);
            venta.TipoDescuento = descuento.Tipo;
            venta.ValorDescuento = descuento.ValorSolicitado;
            venta.DescuentoAplicado = descuento.DescuentoAplicado;

            // La cabecera, las líneas y la auditoría se confirman o revierten juntas.
            const string sqlCabecera = """
                INSERT INTO ventas
                    (fecha, cliente, subtotal, descuento_aplicado, tipo_descuento,
                     valor_descuento, total, vendedor_id)
                VALUES (@fecha, @cliente, @subtotal, @descuento, @tipo, @valor, @total, @vendedor)
                RETURNING id
                """;
            await using (var cabecera = new NpgsqlCommand(sqlCabecera, cn, tx))
            {
                cabecera.Parameters.AddWithValue("fecha", NpgsqlDbType.Timestamp, venta.Fecha);
                cabecera.Parameters.AddWithValue("cliente", NpgsqlDbType.Varchar, venta.Cliente);
                cabecera.Parameters.AddWithValue("subtotal", NpgsqlDbType.Numeric, venta.Subtotal);
                cabecera.Parameters.AddWithValue("descuento", NpgsqlDbType.Numeric, venta.DescuentoAplicado);
                cabecera.Parameters.AddWithValue("tipo", NpgsqlDbType.Varchar, venta.TipoDescuento);
                cabecera.Parameters.AddWithValue("valor", NpgsqlDbType.Numeric, venta.ValorDescuento);
                cabecera.Parameters.AddWithValue("total", NpgsqlDbType.Numeric, venta.Total);
                cabecera.Parameters.AddWithValue("vendedor", NpgsqlDbType.Integer, vendedorAutenticadoId);
                venta.Id = Convert.ToInt32(await cabecera.ExecuteScalarAsync());
            }

            const string sqlDetalle = """
                INSERT INTO detalle_ventas
                    (id_venta, id_producto, nombre_producto, cantidad, precio_unitario, subtotal)
                VALUES (@id_venta, @id_producto, @nombre, @cantidad, @precio, @subtotal)
                """;
            foreach (var linea in venta.Detalles)
            {
                await using var detalle = new NpgsqlCommand(sqlDetalle, cn, tx);
                detalle.Parameters.AddWithValue("id_venta", NpgsqlDbType.Integer, venta.Id);
                detalle.Parameters.AddWithValue("id_producto", NpgsqlDbType.Integer, linea.IdProducto);
                detalle.Parameters.AddWithValue("nombre", NpgsqlDbType.Varchar, linea.NombreProducto);
                detalle.Parameters.AddWithValue("cantidad", NpgsqlDbType.Integer, linea.Cantidad);
                detalle.Parameters.AddWithValue("precio", NpgsqlDbType.Numeric, linea.PrecioUnitario);
                detalle.Parameters.AddWithValue("subtotal", NpgsqlDbType.Numeric, linea.Subtotal);
                await detalle.ExecuteNonQueryAsync();
            }

            if (venta.DescuentoAplicado > 0m)
            {
                const string sqlAuditoria = """
                    INSERT INTO auditoria_descuentos
                        (id_venta, vendedor_id, subtotal, tipo_descuento,
                         valor_solicitado, descuento_aplicado, total_final)
                    VALUES (@venta, @vendedor, @subtotal, @tipo, @valor, @descuento, @total)
                    """;
                await using var auditoria = new NpgsqlCommand(sqlAuditoria, cn, tx);
                auditoria.Parameters.AddWithValue("venta", NpgsqlDbType.Integer, venta.Id);
                auditoria.Parameters.AddWithValue("vendedor", NpgsqlDbType.Integer, vendedorAutenticadoId);
                auditoria.Parameters.AddWithValue("subtotal", NpgsqlDbType.Numeric, venta.Subtotal);
                auditoria.Parameters.AddWithValue("tipo", NpgsqlDbType.Varchar, venta.TipoDescuento);
                auditoria.Parameters.AddWithValue("valor", NpgsqlDbType.Numeric, venta.ValorDescuento);
                auditoria.Parameters.AddWithValue("descuento", NpgsqlDbType.Numeric, venta.DescuentoAplicado);
                auditoria.Parameters.AddWithValue("total", NpgsqlDbType.Numeric, venta.Total);
                await auditoria.ExecuteNonQueryAsync();
            }
            await tx.CommitAsync();
            return venta;
        }
        catch
        {
            await tx.RollbackAsync();
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
            """
            SELECT v.id, v.fecha, v.cliente, v.descuento_aplicado,
                   v.tipo_descuento, v.valor_descuento, v.vendedor_id,
                   COALESCE(u.nombre, 'Registro anterior')
              FROM ventas v LEFT JOIN vendedores u ON u.id = v.vendedor_id
             WHERE v.id = @id
            """, cn))
        {
            cabecera.Parameters.AddWithValue("id", NpgsqlDbType.Integer, id);
            await using var r = await cabecera.ExecuteReaderAsync();
            if (!await r.ReadAsync()) return null;
            venta = new Venta
            {
                Id = r.GetInt32(0), Fecha = r.GetDateTime(1), Cliente = r.GetString(2),
                DescuentoAplicado = r.GetDecimal(3), TipoDescuento = r.GetString(4),
                ValorDescuento = r.GetDecimal(5),
                VendedorId = r.IsDBNull(6) ? null : r.GetInt32(6),
                VendedorNombre = r.GetString(7)
            };
        }
        await using (var detalle = new NpgsqlCommand(
            "SELECT id_producto, nombre_producto, cantidad, precio_unitario FROM detalle_ventas WHERE id_venta = @id ORDER BY id", cn))
        {
            detalle.Parameters.AddWithValue("id", NpgsqlDbType.Integer, id);
            await using var r = await detalle.ExecuteReaderAsync();
            while (await r.ReadAsync())
                venta.Detalles.Add(new DetalleVenta
                {
                    IdProducto = r.GetInt32(0), NombreProducto = r.GetString(1),
                    Cantidad = r.GetInt32(2), PrecioUnitario = r.GetDecimal(3)
                });
        }
        return venta;
    }

    // Solo se invoca desde el controlador protegido por la política AUDITOR.
    public async Task<List<AuditoriaDescuentoFila>> ListarAuditoriaAsync()
    {
        var lista = new List<AuditoriaDescuentoFila>();
        await using var cn = _factory.CreateConnection();
        await cn.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            """
            SELECT a.id_venta, a.fecha AT TIME ZONE 'America/La_Paz', v.cliente, u.nombre, a.vendedor_id, a.subtotal,
                   a.descuento_aplicado, a.total_final, a.tipo_descuento
              FROM auditoria_descuentos a
              JOIN ventas v ON v.id = a.id_venta
              JOIN vendedores u ON u.id = a.vendedor_id
             ORDER BY a.fecha DESC LIMIT 200
            """, cn);
        await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync())
            lista.Add(new AuditoriaDescuentoFila(r.GetInt32(0), r.GetDateTime(1),
                r.GetString(2), r.GetString(3), r.GetInt32(4), r.GetDecimal(5), r.GetDecimal(6),
                r.GetDecimal(7), r.GetString(8)));
        return lista;
    }
}
