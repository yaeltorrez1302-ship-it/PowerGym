using System.ComponentModel.DataAnnotations;
namespace PowerGym.Models;
public class Socio
{
    public int IdSocio { get; set; }
    [Required, StringLength(80)]
    public string Nombres { get; set; } = "";
    [Required, StringLength(80)]
    public string Apellidos { get; set; } = "";
    [Required, StringLength(20)]
    public string Documento { get; set; } = "";
    [StringLength(25)]
    public string? Telefono { get; set; }
    [EmailAddress]
    public string? Correo { get; set; }
    public DateTime FechaRegistro { get; set; }
}
