using Microsoft.AspNetCore.Mvc;
using Npgsql;
using PowerGym.Data;

namespace PowerGym.Controllers;

public class DbTestController : Controller
{
    private readonly DbConnectionFactory _factory;

    public DbTestController(DbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            await using var connection = _factory.CreateConnection();
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(
                "SELECT current_database(), current_user;",
                connection
            );

            await using var reader = await command.ExecuteReaderAsync();
            await reader.ReadAsync();

            return Content(
                $"CONEXION EXITOSA\nBase de datos: {reader.GetString(0)}\nUsuario: {reader.GetString(1)}"
            );
        }
        catch (Exception ex)
        {
            return StatusCode(500, "ERROR DE CONEXION: " + ex.Message);
        }
    }
}
