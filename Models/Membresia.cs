using System.ComponentModel.DataAnnotations;
namespace PowerGym.Models;
public class Membresia
{
    public int IdMembresia { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un socio.")]
    public int IdSocio { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un plan.")]
    public int IdPlan { get; set; }

    [Required, DataType(DataType.Date)]
    public DateTime FechaInicio { get; set; } = DateTime.Today;

    public DateTime FechaFin { get; set; }
    public decimal PrecioAplicado { get; set; }
    public string Estado { get; set; } = "ACTIVA";
    public string NombreSocio { get; set; } = "";
    public string Documento { get; set; } = "";
    public string NombrePlan { get; set; } = "";
}
