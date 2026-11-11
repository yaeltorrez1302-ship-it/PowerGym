using Npgsql;
using PowerGym.Models;

namespace PowerGym.Data;

public class MembresiaDAO
{
    private readonly DbConnectionFactory factory;

    public MembresiaDAO(DbConnectionFactory f) => factory = f;

    public async Task<List<Socio>> ObtenerSociosAsync()
    {
        var lista = new List<Socio>();
        await using var cn = factory.CreateConnection();
        await cn.OpenAsync();

        await using var cmd = new NpgsqlCommand(
            "SELECT id_socio,nombres,apellidos,documento FROM socios WHERE activo=TRUE ORDER BY apellidos", cn);

        await using var r = await cmd.ExecuteReaderAsync();

        while (await r.ReadAsync())
            lista.Add(new Socio {
                IdSocio = r.GetInt32(0),
                Nombres = r.GetString(1),
                Apellidos = r.GetString(2),
                Documento = r.GetString(3)
            });

        return lista;
    }

    public async Task<List<Plan>> ObtenerPlanesAsync()
    {
        var lista = new List<Plan>();
        await using var cn = factory.CreateConnection();
        await cn.OpenAsync();

        await using var cmd = new NpgsqlCommand(
            "SELECT id_plan,nombre,duracion_dias,precio,activo FROM planes WHERE activo=TRUE ORDER BY duracion_dias", cn);

        await using var r = await cmd.ExecuteReaderAsync();

        while (await r.ReadAsync())
            lista.Add(new Plan {
                IdPlan = r.GetInt32(0),
                Nombre = r.GetString(1),
                DuracionDias = r.GetInt32(2),
                Precio = r.GetDecimal(3),
                Activo = r.GetBoolean(4)
            });

        return lista;
    }

    public async Task<List<Membresia>> ListarAsync()
    {
        var lista = new List<Membresia>();

        const string sql = """
        SELECT m.id_membresia,
               s.nombres || ' ' || s.apellidos,
               s.documento,
               p.nombre,
               m.fecha_inicio,
               m.fecha_fin,
               m.precio_aplicado,
               m.estado
        FROM membresias m
        INNER JOIN socios s ON s.id_socio=m.id_socio
        INNER JOIN planes p ON p.id_plan=m.id_plan
        ORDER BY m.id_membresia DESC;
        """;

        await using var cn = factory.CreateConnection();
        await cn.OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, cn);
        await using var r = await cmd.ExecuteReaderAsync();

        while (await r.ReadAsync())
            lista.Add(new Membresia {
                IdMembresia = r.GetInt32(0),
                NombreSocio = r.GetString(1),
                Documento = r.GetString(2),
                NombrePlan = r.GetString(3),
                FechaInicio = r.GetDateTime(4),
                FechaFin = r.GetDateTime(5),
                PrecioAplicado = r.GetDecimal(6),
                Estado = r.GetString(7)
            });

        return lista;
    }

    public async Task<bool> CrearAsync(Membresia m)
    {
        const string sql = """
        INSERT INTO membresias
        (id_socio,id_plan,fecha_inicio,fecha_fin,precio_aplicado,estado)
        SELECT @socio,p.id_plan,@inicio::date,
               (@inicio::date + p.duracion_dias),
               p.precio,'ACTIVA'
        FROM planes p
        INNER JOIN socios s
            ON s.id_socio=@socio AND s.activo=TRUE
        WHERE p.id_plan=@plan AND p.activo=TRUE;
        """;

        await using var cn = factory.CreateConnection();
        await cn.OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, cn);

        cmd.Parameters.AddWithValue("socio", m.IdSocio);
        cmd.Parameters.AddWithValue("plan", m.IdPlan);
        cmd.Parameters.AddWithValue("inicio", m.FechaInicio.Date);

        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task CancelarAsync(int id)
    {
        await using var cn = factory.CreateConnection();
        await cn.OpenAsync();

        await using var cmd = new NpgsqlCommand(
            "UPDATE membresias SET estado='CANCELADA' WHERE id_membresia=@id AND estado='ACTIVA'", cn);

        cmd.Parameters.AddWithValue("id", id);
        await cmd.ExecuteNonQueryAsync();
    }
}
