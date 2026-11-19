using System.Security.Cryptography;
using Npgsql;
using NpgsqlTypes;

namespace PowerGym.Data;

public sealed record VendedorSesion(int Id, string Usuario, string Nombre, string Rol);

public sealed class VendedorDAO
{
    private const int Iteraciones = 310000;
    private readonly DbConnectionFactory _factory;
    public VendedorDAO(DbConnectionFactory factory) => _factory = factory;

    public async Task<VendedorSesion?> AutenticarAsync(string? usuario, string? clave)
    {
        if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrEmpty(clave) || usuario.Length > 60)
            return null;
        await using var cn = _factory.CreateConnection();
        await cn.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            "SELECT id, usuario, nombre, rol, hash_password FROM vendedores WHERE usuario = @u AND activo = TRUE", cn);
        cmd.Parameters.AddWithValue("u", NpgsqlDbType.Varchar, usuario.Trim());
        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;
        var id = reader.GetInt32(0);
        var usuarioBd = reader.GetString(1);
        var nombre = reader.GetString(2);
        var rol = reader.GetString(3);
        var hash = reader.GetString(4);
        if (!VerificarClave(clave, hash)) return null;
        return new VendedorSesion(id, usuarioBd, nombre, rol);
    }

    public async Task CrearUsuarioLocalAsync(string usuario, string nombre, string rol, string clave)
    {
        if (string.IsNullOrWhiteSpace(usuario) || usuario.Length > 60 ||
            !System.Text.RegularExpressions.Regex.IsMatch(usuario, "^[a-zA-Z0-9._-]{3,60}$"))
            throw new ArgumentException("Usuario: de 3 a 60 caracteres, letras, números, punto, guion o guion bajo.");
        if (string.IsNullOrWhiteSpace(nombre) || nombre.Length > 120)
            throw new ArgumentException("Introduce un nombre de 1 a 120 caracteres.");
        if (rol is not ("VENDEDOR" or "AUDITOR"))
            throw new ArgumentException("Rol no permitido.");
        if (clave.Length < 12 || clave.Length > 128)
            throw new ArgumentException("La contraseña debe tener entre 12 y 128 caracteres.");
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(clave, salt, Iteraciones, HashAlgorithmName.SHA256, 32);
        var almacenado = $"pbkdf2-sha256${Iteraciones}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
        await using var cn = _factory.CreateConnection();
        await cn.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            "INSERT INTO vendedores (usuario, nombre, rol, hash_password) VALUES (@u, @n, @r, @h)", cn);
        cmd.Parameters.AddWithValue("u", NpgsqlDbType.Varchar, usuario.Trim());
        cmd.Parameters.AddWithValue("n", NpgsqlDbType.Varchar, nombre.Trim());
        cmd.Parameters.AddWithValue("r", NpgsqlDbType.Varchar, rol);
        cmd.Parameters.AddWithValue("h", NpgsqlDbType.Text, almacenado);
        await cmd.ExecuteNonQueryAsync();
    }

    private static bool VerificarClave(string clave, string almacenado)
    {
        var partes = almacenado.Split('$');
        if (partes.Length != 4 || partes[0] != "pbkdf2-sha256" ||
            !int.TryParse(partes[1], out var vueltas) || vueltas < Iteraciones || vueltas > 1000000)
            return false;
        try
        {
            var salt = Convert.FromBase64String(partes[2]);
            var esperado = Convert.FromBase64String(partes[3]);
            if (salt.Length != 16 || esperado.Length != 32) return false;
            var real = Rfc2898DeriveBytes.Pbkdf2(clave, salt, vueltas, HashAlgorithmName.SHA256, 32);
            return CryptographicOperations.FixedTimeEquals(real, esperado);
        }
        catch (FormatException) { return false; }
    }
}
