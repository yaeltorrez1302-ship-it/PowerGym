using Npgsql;
using PowerGym.Models;
namespace PowerGym.Data;
public class SocioDAO
{
    private readonly DbConnectionFactory factory;
    public SocioDAO(DbConnectionFactory f) => factory = f;

    public async Task<List<Socio>> Listar(string? buscar)
    {
        var lista = new List<Socio>();
        const string sql = """
        SELECT id_socio,nombres,apellidos,documento,telefono,correo,fecha_registro
        FROM socios WHERE activo=TRUE AND
        (@b='' OR nombres ILIKE '%'||@b||'%' OR apellidos ILIKE '%'||@b||'%' OR documento ILIKE '%'||@b||'%')
        ORDER BY apellidos,nombres
        """;
        await using var cn = factory.CreateConnection();
        await cn.OpenAsync();
        await using var cmd = new NpgsqlCommand(sql,cn);
        cmd.Parameters.AddWithValue("b",buscar?.Trim() ?? "");
        await using var r = await cmd.ExecuteReaderAsync();
        while(await r.ReadAsync())
            lista.Add(new Socio {
                IdSocio=r.GetInt32(0), Nombres=r.GetString(1),
                Apellidos=r.GetString(2), Documento=r.GetString(3),
                Telefono=r.IsDBNull(4)?null:r.GetString(4),
                Correo=r.IsDBNull(5)?null:r.GetString(5),
                FechaRegistro=r.GetDateTime(6)
            });
        return lista;
    }

    public async Task<Socio?> Obtener(int id)
    {
        await using var cn=factory.CreateConnection();
        await cn.OpenAsync();
        await using var cmd=new NpgsqlCommand(
            "SELECT id_socio,nombres,apellidos,documento,telefono,correo,fecha_registro FROM socios WHERE id_socio=@id AND activo=TRUE",cn);
        cmd.Parameters.AddWithValue("id",id);
        await using var r=await cmd.ExecuteReaderAsync();
        if(!await r.ReadAsync()) return null;
        return new Socio {
            IdSocio=r.GetInt32(0),Nombres=r.GetString(1),
            Apellidos=r.GetString(2),Documento=r.GetString(3),
            Telefono=r.IsDBNull(4)?null:r.GetString(4),
            Correo=r.IsDBNull(5)?null:r.GetString(5),
            FechaRegistro=r.GetDateTime(6)
        };
    }

    public async Task Crear(Socio s)
    {
        const string sql="INSERT INTO socios(nombres,apellidos,documento,telefono,correo) VALUES(@n,@a,@d,@t,@c)";
        await using var cn=factory.CreateConnection();
        await cn.OpenAsync();
        await using var cmd=new NpgsqlCommand(sql,cn);
        Parametros(cmd,s);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<bool> Editar(Socio s)
    {
        const string sql="UPDATE socios SET nombres=@n,apellidos=@a,documento=@d,telefono=@t,correo=@c WHERE id_socio=@id AND activo=TRUE";
        await using var cn=factory.CreateConnection();
        await cn.OpenAsync();
        await using var cmd=new NpgsqlCommand(sql,cn);
        Parametros(cmd,s);
        cmd.Parameters.AddWithValue("id",s.IdSocio);
        return await cmd.ExecuteNonQueryAsync()>0;
    }

    public async Task Desactivar(int id)
    {
        await using var cn=factory.CreateConnection();
        await cn.OpenAsync();
        await using var cmd=new NpgsqlCommand("UPDATE socios SET activo=FALSE WHERE id_socio=@id",cn);
        cmd.Parameters.AddWithValue("id",id);
        await cmd.ExecuteNonQueryAsync();
    }

    private static void Parametros(NpgsqlCommand c,Socio s)
    {
        c.Parameters.AddWithValue("n",s.Nombres.Trim());
        c.Parameters.AddWithValue("a",s.Apellidos.Trim());
        c.Parameters.AddWithValue("d",s.Documento.Trim());
        c.Parameters.AddWithValue("t",(object?)s.Telefono??DBNull.Value);
        c.Parameters.AddWithValue("c",(object?)s.Correo??DBNull.Value);
    }
}
